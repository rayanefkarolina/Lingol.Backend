using System.Threading.Channels;
using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Application.Queue;
using Lingol.Pedagogico.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lingol.Pedagogico.Application.Commands;

public class EnviarRespostasCommandHandler
    : IRequestHandler<EnviarRespostasCommand, EnviarRespostasResult>
{
    private readonly IPedagogicoDbContext _db;
    private readonly ICadastroClient _cadastroClient;
    private readonly ChannelWriter<CorrigirEntregaQueueItem> _correcaoWriter;

    public EnviarRespostasCommandHandler(
        IPedagogicoDbContext db,
        ICadastroClient cadastroClient,
        ChannelWriter<CorrigirEntregaQueueItem> correcaoWriter)
    {
        _db = db;
        _cadastroClient = cadastroClient;
        _correcaoWriter = correcaoWriter;
    }

    public async Task<EnviarRespostasResult> Handle(
        EnviarRespostasCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Atividade precisa existir e estar pronta.
        var atividade = await _db.Atividades
            .FirstOrDefaultAsync(a => a.Id == command.AtividadeId, cancellationToken)
            ?? throw new InvalidOperationException("Atividade não encontrada.");

        if (atividade.Status != StatusAtividade.Pronta)
            throw new InvalidOperationException("Esta atividade ainda não está disponível para respostas.");

        // 2. O aluno precisa pertencer à turma da atividade.
        var aluno = await _cadastroClient.ObterAlunoAsync(command.AlunoId, cancellationToken)
            ?? throw new InvalidOperationException("Aluno não encontrado no serviço de Cadastro.");

        if (aluno.TurmaId != atividade.TurmaId)
            throw new UnauthorizedAccessException("Esta atividade não pertence à turma do aluno.");

        // 3. Uma entrega por aluno por atividade.
        var jaEntregou = await _db.RespostasAluno
            .AnyAsync(r => r.AtividadeId == atividade.Id && r.AlunoId == command.AlunoId, cancellationToken);

        if (jaEntregou)
            throw new InvalidOperationException("Este aluno já entregou esta atividade.");

        var questoes = await _db.Questoes
            .Where(q => q.AtividadeId == atividade.Id)
            .ToListAsync(cancellationToken);

        // 4. Corrigir objetivamente (o diagnóstico por IA roda depois, em background).
        var entrega = new RespostaAluno(atividade.Id, atividade.TurmaId, command.AlunoId);
        _db.Adicionar(entrega);

        var correcoes = new List<CorrecaoQuestaoDto>();
        var acertos = 0;
        var erros = 0;

        foreach (var questao in questoes.OrderBy(q => q.Ordem))
        {
            var resposta = command.Respostas.FirstOrDefault(r => r.QuestaoId == questao.Id);
            var escolhida = resposta?.RespostaEscolhida ?? string.Empty;

            var acertou = !string.IsNullOrWhiteSpace(escolhida)
                && string.Equals(
                    escolhida.Trim(),
                    questao.GabaritoOuCriterio.Trim(),
                    StringComparison.InvariantCultureIgnoreCase);

            if (acertou) acertos++;
            else erros++;

            var item = entrega.ResponderQuestao(
                questao.Id,
                escolhida,
                acertou,
                resposta?.TempoSegundos ?? 0);

            _db.Adicionar(item);

            correcoes.Add(new CorrecaoQuestaoDto(
                questao.Id,
                acertou,
                questao.GabaritoOuCriterio,
                questao.Explicacao));
        }

        entrega.ConsolidarPlacar(acertos, erros);

        await _db.SaveChangesAsync(cancellationToken);

        // 5. Enfileirar o diagnóstico pedagógico (IA) — o aluno não espera por ele.
        await _correcaoWriter.WriteAsync(new CorrigirEntregaQueueItem
        {
            RespostaAlunoId = entrega.Id,
            AtividadeId = atividade.Id,
            TurmaId = atividade.TurmaId,
            AlunoId = command.AlunoId
        }, cancellationToken);

        return new EnviarRespostasResult(
            RespostaAlunoId: entrega.Id,
            Acertos: acertos,
            Erros: erros,
            TotalQuestoes: acertos + erros,
            Nota: entrega.Nota,
            Correcoes: correcoes);
    }
}
