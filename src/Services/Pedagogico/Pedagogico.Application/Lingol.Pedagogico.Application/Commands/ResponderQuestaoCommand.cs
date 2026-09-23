using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lingol.Pedagogico.Application.Commands;

/// <summary>
/// Resposta de UMA questão, usada pelo ciclo de feedback imediato: o aluno
/// responde, descobre na hora se acertou e recebe a explicação da regra.
/// A entrega é criada na primeira questão respondida e cada questão só aceita
/// uma tentativa, o que impede ficar chutando até acertar.
/// </summary>
public record ResponderQuestaoCommand(
    Guid AtividadeId,
    Guid AlunoId,
    Guid QuestaoId,
    string RespostaEscolhida,
    int TempoSegundos
) : IRequest<ResponderQuestaoResult>;

public record ResponderQuestaoResult(
    bool EstaCorreta,
    string GabaritoOuCriterio,
    string? Explicacao,
    int Acertos,
    int Respondidas,
    int TotalQuestoes
);

public class ResponderQuestaoCommandHandler
    : IRequestHandler<ResponderQuestaoCommand, ResponderQuestaoResult>
{
    private readonly IPedagogicoDbContext _db;
    private readonly ICadastroClient _cadastroClient;

    public ResponderQuestaoCommandHandler(IPedagogicoDbContext db, ICadastroClient cadastroClient)
    {
        _db = db;
        _cadastroClient = cadastroClient;
    }

    public async Task<ResponderQuestaoResult> Handle(
        ResponderQuestaoCommand command,
        CancellationToken cancellationToken)
    {
        var atividade = await _db.Atividades
            .FirstOrDefaultAsync(a => a.Id == command.AtividadeId, cancellationToken)
            ?? throw new InvalidOperationException("Atividade não encontrada.");

        if (atividade.Status != StatusAtividade.Pronta)
            throw new InvalidOperationException("Esta atividade ainda não está disponível.");

        var aluno = await _cadastroClient.ObterAlunoAsync(command.AlunoId, cancellationToken)
            ?? throw new InvalidOperationException("Aluno não encontrado no serviço de Cadastro.");

        if (aluno.TurmaId != atividade.TurmaId)
            throw new UnauthorizedAccessException("Esta atividade não pertence à turma do aluno.");

        var questao = await _db.Questoes
            .FirstOrDefaultAsync(q => q.Id == command.QuestaoId && q.AtividadeId == atividade.Id, cancellationToken)
            ?? throw new InvalidOperationException("Questão não pertence a esta atividade.");

        var totalQuestoes = await _db.Questoes
            .CountAsync(q => q.AtividadeId == atividade.Id, cancellationToken);

        var entrega = await _db.RespostasAluno
            .Include(r => r.Itens)
            .FirstOrDefaultAsync(r => r.AtividadeId == atividade.Id && r.AlunoId == command.AlunoId, cancellationToken);

        if (entrega is null)
        {
            entrega = new RespostaAluno(atividade.Id, atividade.TurmaId, command.AlunoId);
            _db.Adicionar(entrega);
        }
        else if (entrega.TotalQuestoes > 0)
        {
            throw new InvalidOperationException("Esta atividade já foi finalizada por este aluno.");
        }
        else if (entrega.Itens.Any(i => i.QuestaoId == command.QuestaoId))
        {
            throw new InvalidOperationException("Esta questão já foi respondida.");
        }

        var acertou = !string.IsNullOrWhiteSpace(command.RespostaEscolhida)
            && string.Equals(
                command.RespostaEscolhida.Trim(),
                questao.GabaritoOuCriterio.Trim(),
                StringComparison.InvariantCultureIgnoreCase);

        var item = entrega.ResponderQuestao(
            questao.Id,
            command.RespostaEscolhida ?? string.Empty,
            acertou,
            command.TempoSegundos);

        _db.Adicionar(item);
        await _db.SaveChangesAsync(cancellationToken);

        return new ResponderQuestaoResult(
            EstaCorreta: acertou,
            GabaritoOuCriterio: questao.GabaritoOuCriterio,
            Explicacao: questao.Explicacao,
            Acertos: entrega.Itens.Count(i => i.EstaCorreta),
            Respondidas: entrega.Itens.Count,
            TotalQuestoes: totalQuestoes);
    }
}
