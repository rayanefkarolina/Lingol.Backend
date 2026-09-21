using Lingol.Core;

namespace Lingol.Pedagogico.Domain.Entities
{
    /// <summary>Resposta de um aluno a uma questão específica de uma atividade.</summary>
    public class RespostaQuestao : EntityBase
    {
        public Guid RespostaAlunoId { get; private set; }
        public Guid QuestaoId { get; private set; }
        public string RespostaEscolhida { get; private set; } = default!;
        public bool EstaCorreta { get; private set; }
        public int TempoSegundos { get; private set; }

        private RespostaQuestao() { }

        public RespostaQuestao(
            Guid respostaAlunoId,
            Guid questaoId,
            string respostaEscolhida,
            bool estaCorreta,
            int tempoSegundos)
        {
            RespostaAlunoId = respostaAlunoId;
            QuestaoId = questaoId;
            RespostaEscolhida = respostaEscolhida;
            EstaCorreta = estaCorreta;
            TempoSegundos = tempoSegundos;
        }
    }
}
