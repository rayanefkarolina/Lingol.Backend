using Lingol.Pedagogico.Application.Abstractions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Pedagogico.Application.Commands
{
    public record CriarAtividadeCommand(

    Guid TurmaId,
    string Livro,
    string CapituloOuAssunto,
    string Materia,
    string? PerfilAeeContexto
    ) : IRequest<CriarAtividadeResult>;

    public record CriarAtividadeResult(
        Guid AtividadeId,
        List<QuestaoGeradaDto> Questoes);
}
