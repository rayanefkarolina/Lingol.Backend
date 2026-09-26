using System.Text;
using System.Threading.Channels;
using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Application.Queue;
using Lingol.Pedagogico.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lingol.Pedagogico.Application.Commands;

/// <summary>
/// Gera uma atividade de revisão individual a partir do que a IA diagnosticou
/// para UM aluno em uma atividade. Cada aluno erra coisas diferentes, então a
/// revisão é direcionada e não aparece para o resto da turma.
/// </summary>
public record CriarAtividadeRevisaoCommand(
    Guid AtividadeOrigemId,
    Guid AlunoId,
    Guid ProfessorId,
    int? NumQuestoes = null
) : IRequest<CriarAtividadeResult>;

public class CriarAtividadeRevisaoCommandHandler
    : IRequestHandler<CriarAtividadeRevisaoCommand, CriarAtividadeResult>
{
    /// <summary>Revisão é mais curta que a atividade original, de propósito.</summary>
    private const int QuestoesPadraoRevisao = 5;

    private readonly IPedagogicoDbContext _db;
    private readonly ICadastroClient _cadastroClient;
    private readonly ChannelWriter<GerarAtividadeQueueItem> _queueWriter;

    public CriarAtividadeRevisaoCommandHandler(
        IPedagogicoDbContext db,
        ICadastroClient cadastroClient,
        ChannelWriter<GerarAtividadeQueueItem> queueWriter)
    {
        _db = db;
        _cadastroClient = cadastroClient;
        _queueWriter = queueWriter;
    }

    public async Task<CriarAtividadeResult> Handle(
        CriarAtividadeRevisaoCommand command,
        CancellationToken cancellationToken)
    {
        var origem = await _db.Atividades
            .Include(a => a.Questoes)
            .FirstOrDefaultAsync(a => a.Id == command.AtividadeOrigemId, cancellationToken)
            ?? throw new InvalidOperationException("Atividade de origem não encontrada.");

        var turma = await _cadastroClient.ObterTurmaAsync(origem.TurmaId, cancellationToken)
            ?? throw new InvalidOperationException("Turma não encontrada no serviço de Cadastro.");

        if (turma.ProfessorId != command.ProfessorId)
            throw new UnauthorizedAccessException("A turma desta atividade não pertence a este professor.");

        var aluno = await _cadastroClient.ObterAlunoAsync(command.AlunoId, cancellationToken)
            ?? throw new InvalidOperationException("Aluno não encontrado no serviço de Cadastro.");

        if (aluno.TurmaId != origem.TurmaId)
            throw new InvalidOperationException("Este aluno não pertence à turma da atividade.");

        var foco = await MontarFocoAsync(origem, command.AlunoId, cancellationToken);

        if (string.IsNullOrWhiteSpace(foco))
        {
            throw new InvalidOperationException(
                "Este aluno não tem dificuldades registradas nesta atividade — não há o que revisar.");
        }

        var numQuestoes = command.NumQuestoes is > 0 ? command.NumQuestoes.Value : QuestoesPadraoRevisao;

        if (origem.Modo == ModoGamificacao.Rpg && numQuestoes > Atividade.MaxQuestoesGamificada)
            numQuestoes = Atividade.MaxQuestoesGamificada;

        var atividade = new Atividade(
            origem.TurmaId,
            command.ProfessorId,
            origem.Livro,
            $"Revisão: {origem.CapituloOuAssunto}",
            origem.Materia,
            numQuestoes,
            origem.Modo,
            origem.Tema,
            alunoId: command.AlunoId,
            atividadeOrigemId: origem.Id);

        var queueItem = new GerarAtividadeQueueItem
        {
            AtividadeId = atividade.Id,
            TurmaId = atividade.TurmaId,
            Livro = atividade.Livro,
            CapituloOuAssunto = origem.CapituloOuAssunto,
            Materia = atividade.Materia,
            Ano = turma.Ano,
            NumQuestoes = numQuestoes,
            Modo = atividade.Modo,
            Tema = atividade.Tema,
            PerfilAeeContexto = aluno.TipoNecessidade is null
                ? null
                : $"o aluno tem {aluno.TipoNecessidade}",
            FocoRevisao = foco
        };

        // Guardado junto da atividade: o foco da revisão é calculado uma vez e
        // não caberia em colunas soltas. Sem isso, um reinício perderia a revisão.
        atividade.RegistrarPayloadGeracao(GerarAtividadeQueueItem.Serializar(queueItem));

        _db.Adicionar(atividade);
        await _db.SaveChangesAsync(cancellationToken);

        await _queueWriter.WriteAsync(queueItem, cancellationToken);

        return new CriarAtividadeResult(
            AtividadeId: atividade.Id,
            Status: atividade.Status.ToString(),
            CriadoEm: atividade.DataCriacao);
    }

    /// <summary>
    /// Junta o que a IA diagnosticou com as questões que o aluno realmente errou.
    /// É esse texto que vira o foco do prompt de revisão.
    /// </summary>
    private async Task<string> MontarFocoAsync(
        Atividade origem,
        Guid alunoId,
        CancellationToken ct)
    {
        var dificuldades = await _db.DificuldadesAluno
            .Where(d => d.AtividadeId == origem.Id && d.AlunoId == alunoId)
            .ToListAsync(ct);

        var questoesErradas = await (
            from item in _db.RespostasQuestao
            join entrega in _db.RespostasAluno on item.RespostaAlunoId equals entrega.Id
            where entrega.AtividadeId == origem.Id
                  && entrega.AlunoId == alunoId
                  && !item.EstaCorreta
            select item.QuestaoId)
            .ToListAsync(ct);

        var sb = new StringBuilder();

        foreach (var grupo in dificuldades.GroupBy(d => d.Tipo))
        {
            sb.AppendLine($"- {grupo.Key}: {string.Join(" ", grupo.Select(d => d.Descricao).Distinct())}");
        }

        var habilidades = origem.Questoes
            .Where(q => questoesErradas.Contains(q.Id))
            .Select(q => q.Habilidade)
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Distinct()
            .ToList();

        if (habilidades.Count > 0)
            sb.AppendLine($"- Habilidades em que errou: {string.Join("; ", habilidades)}");

        return sb.ToString().Trim();
    }
}
