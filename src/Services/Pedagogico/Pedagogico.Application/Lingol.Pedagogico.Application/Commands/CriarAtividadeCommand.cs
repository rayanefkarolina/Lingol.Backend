using Lingol.Pedagogico.Domain.Entities;
using MediatR;

namespace Lingol.Pedagogico.Application.Commands
{
    public record CriarAtividadeCommand(
        Guid TurmaId,
        Guid ProfessorId,
        string Livro,
        string CapituloOuAssunto,
        string Materia,
        int NumQuestoes = 10,
        ModoGamificacao Modo = ModoGamificacao.Simples,
        string? PerfilAeeContexto = null
    ) : IRequest<CriarAtividadeResult>;

    public record CriarAtividadeResult(
        Guid AtividadeId,
        string Status,
        DateTime CriadoEm
    );
}
