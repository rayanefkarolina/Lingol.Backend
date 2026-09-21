using System.Text.Json;
using Lingol.Core;

namespace Lingol.Pedagogico.Domain.Entities
{
    /// <summary>
    /// Modo de renderização da atividade no frontend.
    /// Simples: Questionário padrão
    /// Rpg: Renderização com modelo interativo de RPG (Lingolgard)
    /// </summary>
    public enum ModoGamificacao
    {
        Simples = 0,
        Rpg = 1
    }

    public class Questao : EntityBase
    {
        public Guid AtividadeId { get; private set; }

        /// <summary>Posição da questão dentro da atividade (1..N).</summary>
        public int Ordem { get; private set; }

        public string Enunciado { get; private set; } = default!;
        public string TipoQuestao { get; private set; } = default!;
        public string GabaritoOuCriterio { get; private set; } = default!;

        /// <summary>
        /// Explicação da regra gramatical devolvida pela IA.
        /// Exibida ao aluno no feedback imediato de erro.
        /// </summary>
        public string? Explicacao { get; private set; }

        public string? AlternativasJson { get; private set; }

        private Questao() { }

        public Questao(
            Guid atividadeId,
            int ordem,
            string enunciado,
            string tipoQuestao,
            string gabaritoOuCriterio,
            IEnumerable<string>? alternativas = null,
            string? explicacao = null)
        {
            AtividadeId = atividadeId;
            Ordem = ordem;
            Enunciado = enunciado;
            TipoQuestao = tipoQuestao;
            GabaritoOuCriterio = gabaritoOuCriterio;
            Explicacao = explicacao;
            AlternativasJson = alternativas is null
                ? null
                : JsonSerializer.Serialize(alternativas);
        }

        /// <summary>Alternativas desserializadas a partir de <see cref="AlternativasJson"/>.</summary>
        public IReadOnlyList<string> ObterAlternativas()
        {
            if (string.IsNullOrWhiteSpace(AlternativasJson))
                return Array.Empty<string>();

            return JsonSerializer.Deserialize<List<string>>(AlternativasJson) ?? new List<string>();
        }
    }
}
