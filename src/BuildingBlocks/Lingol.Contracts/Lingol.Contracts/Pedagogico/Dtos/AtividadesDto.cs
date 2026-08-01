using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Contracts.Pedagogico.Dtos
{
    public record AtividadeResumoDto(
    Guid Id,
    Guid TurmaId,
    string Livro,
    string CapituloOuAssunto,
    DateTime DataCriacao);

    public record AtividadeDetalheDto(
        Guid Id,
        Guid TurmaId,
        string Livro,
        string CapituloOuAssunto,
        List<QuestaoDetalheDto> Questoes);

    public record QuestaoDetalheDto(
        Guid Id,
        string Enunciado,
        string Tipo,
        List<string> Alternativas);
}
