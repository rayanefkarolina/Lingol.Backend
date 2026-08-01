using Lingol.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Domain.Entities
{
    public class Turma : EntityBase
    {
        private readonly List<Aluno> _alunos = new();
        public string Nome { get; private set; } = default!;
        public string Materia { get; private set; } = "Língua Portuguesa";
        public Guid ProfessorId { get; private set; }
        public IReadOnlyCollection<Aluno> Alunos => _alunos;

        private Turma(string v) { }

        public Turma(string nome, Guid professorId, string materia = "Língua Portuguesa")
        {
            Nome = nome;
            ProfessorId = professorId;
            Materia = materia;
        }

        public Aluno MatricularAluno(string nome, string matricula)
        {
            if (_alunos.Any(a => a.Matricula == matricula))
                throw new DomainException("Matrícula já cadastrada nesta turma.");

            var aluno = new Aluno(nome, matricula, Id);
            _alunos.Add(aluno);
            return aluno;
        }
    }
}
