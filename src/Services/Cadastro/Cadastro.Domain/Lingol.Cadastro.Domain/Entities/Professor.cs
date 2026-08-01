using Lingol.Domain.Entities.Usuário;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Domain.Entities
{
    public class Professor : Usuario
    {
        private readonly List<Turma> _turmas = new();
        public string SenhaHash { get; private set; } = default!;
        public IReadOnlyCollection<Turma> Turmas => _turmas;

        private Professor() { }

        public Professor(string nome, string email, string senhaHash) : base(nome, email)
        {
            SenhaHash = senhaHash;
        }
    }
}
