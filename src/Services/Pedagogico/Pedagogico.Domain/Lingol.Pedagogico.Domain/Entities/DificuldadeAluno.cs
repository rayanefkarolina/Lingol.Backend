using Lingol.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Pedagogico.Domain.Entities
{
    public class DificuldadeAluno : EntityBase
    {
        public Guid RespostaAlunoId { get; private set; }
        public Guid AtividadeId { get; private set; }
        public Guid TurmaId { get; private set; }
        public Guid AlunoId { get; private set; }
        public Guid QuestaoId { get; private set; }
        public TipoDificuldade Tipo { get; private set; }
        public string Descricao { get; private set; } = default!;

        private DificuldadeAluno() { }

        public DificuldadeAluno(
            Guid respostaAlunoId,
            Guid atividadeId,
            Guid turmaId,
            Guid alunoId,
            Guid questaoId,
            TipoDificuldade tipo,
            string descricao)
        {
            RespostaAlunoId = respostaAlunoId;
            AtividadeId = atividadeId;
            TurmaId = turmaId;
            AlunoId = alunoId;
            QuestaoId = questaoId;
            Tipo = tipo;
            Descricao = descricao;
        }
    }
}
