using System.Threading.Channels;
using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Application.Queue;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lingol.Pedagogico.Application.Commands;

/// <summary>
/// Fecha a entrega do aluno: consolida o placar ("você acertou 6/10") e
/// enfileira o diagnóstico pedagógico. Questões não respondidas contam como erro.
/// </summary>
public record FinalizarAtividadeCommand(
    Guid AtividadeId,
    Guid AlunoId
) : IRequest<FinalizarAtividadeResult>;

public record FinalizarAtividadeResult(
    Guid RespostaAlunoId,
    int Acertos,
    int Erros,
    int TotalQuestoes,
    decimal? Nota
);

public class FinalizarAtividadeCommandHandler
    : IRequestHandler<FinalizarAtividadeCommand, FinalizarAtividadeResult>
{
    private readonly IPedagogicoDbContext _db;
    private readonly ChannelWriter<CorrigirEntregaQueueItem> _correcaoWriter;

    public FinalizarAtividadeCommandHandler(
        IPedagogicoDbContext db,
        ChannelWriter<CorrigirEntregaQueueItem> correcaoWriter)
    {
        _db = db;
        _correcaoWriter = correcaoWriter;
    }

    public async Task<FinalizarAtividadeResult> Handle(
        FinalizarAtividadeCommand command,
        CancellationToken cancellationToken)
    {
        var entrega = await _db.RespostasAluno
            .Include(r => r.Itens)
            .FirstOrDefaultAsync(
                r => r.AtividadeId == command.AtividadeId && r.AlunoId == command.AlunoId,
                cancellationToken)
            ?? throw new InvalidOperationException("Nenhuma resposta registrada para esta atividade.");

        // Já finalizada: devolve o mesmo resultado em vez de duplicar a entrega.
        if (entrega.TotalQuestoes > 0)
        {
            return new FinalizarAtividadeResult(
                entrega.Id, entrega.Acertos, entrega.Erros, entrega.TotalQuestoes, entrega.Nota);
        }

        var totalQuestoes = await _db.Questoes
            .CountAsync(q => q.AtividadeId == command.AtividadeId, cancellationToken);

        var acertos = entrega.Itens.Count(i => i.EstaCorreta);

        // O que o aluno deixou em branco entra como erro no placar.
        var erros = totalQuestoes - acertos;

        entrega.ConsolidarPlacar(acertos, erros);
        await _db.SaveChangesAsync(cancellationToken);

        await _correcaoWriter.WriteAsync(new CorrigirEntregaQueueItem
        {
            RespostaAlunoId = entrega.Id,
            AtividadeId = entrega.AtividadeId,
            TurmaId = entrega.TurmaId,
            AlunoId = entrega.AlunoId
        }, cancellationToken);

        return new FinalizarAtividadeResult(
            entrega.Id, entrega.Acertos, entrega.Erros, entrega.TotalQuestoes, entrega.Nota);
    }
}
