using Lingol.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Domain.Entities
{
    public class Aluno : EntityBase
    {
        public string Nome { get; private set; } = default!;
        public string Matricula { get; private set; } = default!;
        public Guid TurmaId { get; private set; }
        public PerfilAee? PerfilAee { get; private set; }

        private Aluno() { }

        public Aluno(string nome, string matricula, Guid turmaId)
        {
            Nome = nome;
            Matricula = matricula;
            TurmaId = turmaId;
        }

        public void DefinirPerfilAee(PerfilAee perfil) => PerfilAee = perfil;
    }
}
