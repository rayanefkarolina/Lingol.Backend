using Lingol.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Pedagogico.Domain.Entities
{
    public class RespostaAluno : EntityBase
    {
        public Guid AtividadeId { get; private set; }
        public Guid AlunoId { get; private set; }
        public bool CorrecaoProcessada { get; private set; }
        public decimal? Nota { get; private set; }
        public string? FeedbackGeral { get; private set; }

        private RespostaAluno() { }

        public RespostaAluno(Guid atividadeId, Guid alunoId)
        {
            AtividadeId = atividadeId;
            AlunoId = alunoId;
            CorrecaoProcessada = false;
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
