using Lingol.Core;

namespace Lingol.Pedagogico.Domain.Entities
{
    /// <summary>
    /// Entrega de uma atividade por um aluno (o "envelope" com todas as respostas).
    /// As respostas questão a questão ficam em <see cref="Itens"/>.
    /// </summary>
    public class RespostaAluno : EntityBase
    {
        private readonly List<RespostaQuestao> _itens = new();

        public Guid AtividadeId { get; private set; }
        public Guid TurmaId { get; private set; }
        public Guid AlunoId { get; private set; }
        public DateTime DataEnvio { get; private set; } = DateTime.UtcNow;

        public int Acertos { get; private set; }
        public int Erros { get; private set; }
        public int TotalQuestoes { get; private set; }

        public bool CorrecaoProcessada { get; private set; }
        public decimal? Nota { get; private set; }
        public string? FeedbackGeral { get; private set; }

        public IReadOnlyCollection<RespostaQuestao> Itens => _itens;

        private RespostaAluno() { }

        public RespostaAluno(Guid atividadeId, Guid turmaId, Guid alunoId)
        {
            AtividadeId = atividadeId;
            TurmaId = turmaId;
            AlunoId = alunoId;
            DataEnvio = DateTime.UtcNow;
            CorrecaoProcessada = false;
        }

        public RespostaQuestao ResponderQuestao(
            Guid questaoId,
            string respostaEscolhida,
            bool estaCorreta,
            int tempoSegundos)
        {
            var item = new RespostaQuestao(Id, questaoId, respostaEscolhida, estaCorreta, tempoSegundos);
            _itens.Add(item);
            return item;
        }

        /// <summary>Consolida o placar objetivo ("você acertou 6/10").</summary>
        public void ConsolidarPlacar(int acertos, int erros)
        {
            Acertos = acertos;
            Erros = erros;
            TotalQuestoes = acertos + erros;
            Nota = TotalQuestoes == 0
                ? 0
                : Math.Round((decimal)acertos / TotalQuestoes * 10m, 2);
        }

        public void DefinirCorrecao(decimal nota, string feedbackGeral)
        {
            Nota = nota;
            FeedbackGeral = feedbackGeral;
        }

        public void MarcarCorrecaoProcessada()
        {
            CorrecaoProcessada = true;
        }
    }
}
