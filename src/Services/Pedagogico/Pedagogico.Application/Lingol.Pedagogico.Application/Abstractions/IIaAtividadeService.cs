using Lingol.Pedagogico.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Pedagogico.Application.Abstractions
{
    public interface IIaAtividadeService
    {
        Task<List<QuestaoGeradaDto>> GerarAtividadeAsync(
        string livro,
        string capituloOuAssunto,
        string materia,
        string? perfilAeeContexto,
        CancellationToken cancellationToken);

        Task<CorrecaoResultado> CorrigirRespostaAsync(
            Atividade atividade,
            RespostaAluno respostaAluno,
            CancellationToken cancellationToken);
    }

    // DTOs mínimos para compilar – depois você pode mover para um arquivo separado
    public record QuestaoGeradaDto(
        string Enunciado,
        string Tipo,
        List<string> Alternativas,
        string GabaritoOuCriterio);

    public record CorrecaoResultado(
        decimal Nota,
        string FeedbackGeral,
        List<DificuldadeIdentificadaDto> Dificuldades);

    public record DificuldadeIdentificadaDto(
        TipoDificuldade Tipo,
        Guid QuestaoId,
        string Descricao);
}

