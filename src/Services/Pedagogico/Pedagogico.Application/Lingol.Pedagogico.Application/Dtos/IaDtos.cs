using Lingol.Pedagogico.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Pedagogico.Application.Dtos
{
    // Questão gerada pela IA quando o professor cria uma atividade
    public record QuestaoGeradaDto(
        string Enunciado,
        string Tipo,
        List<string> Alternativas,
        string GabaritoOuCriterio);

    // Dificuldade identificada pela IA ao corrigir uma atividade
    public record DificuldadeIdentificadaDto(
        TipoDificuldade Tipo,
        Guid QuestaoId,
        string Descricao);

    // Resultado da correção de uma atividade pelo aluno
    public record CorrecaoResultado(
        decimal Nota,
        string FeedbackGeral,
        List<DificuldadeIdentificadaDto> Dificuldades);
}
