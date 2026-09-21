using Lingol.Pedagogico.Domain.Entities;

namespace Lingol.Pedagogico.Application.Dtos
{
    /// <summary>Questão devolvida pela IA no momento em que o professor gera a atividade.</summary>
    public record QuestaoGeradaDto(
        string Enunciado,
        string Tipo,
        List<string> Alternativas,
        string GabaritoOuCriterio,
        string? Explicacao = null);

    /// <summary>Dificuldade identificada pela IA ao corrigir uma entrega.</summary>
    public record DificuldadeIdentificadaDto(
        TipoDificuldade Tipo,
        Guid QuestaoId,
        string Descricao);

    /// <summary>Resultado da correção de uma entrega do aluno.</summary>
    public record CorrecaoResultado(
        decimal Nota,
        string FeedbackGeral,
        List<DificuldadeIdentificadaDto> Dificuldades);
}
