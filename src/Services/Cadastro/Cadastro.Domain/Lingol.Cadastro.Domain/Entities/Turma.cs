using Lingol.Core;

namespace Lingol.Domain.Entities
{
    public class Turma : EntityBase
    {
        private readonly List<Aluno> _alunos = new();
        public string Nome { get; private set; } = default!;
        public string Materia { get; private set; } = "Língua Portuguesa";

        /// <summary>
        /// Ano/série da turma no Ensino Fundamental (1 a 9).
        /// Usado no prompt enviado à IA para adaptar o nível das questões.
        /// </summary>
        public int Ano { get; private set; } = 6;

        public Guid ProfessorId { get; private set; }
        public IReadOnlyCollection<Aluno> Alunos => _alunos;

        private Turma() { }

        public Turma(string nome, Guid professorId, int ano = 6, string materia = "Língua Portuguesa")
        {
            Nome = nome;
            ProfessorId = professorId;
            Ano = ano;
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
