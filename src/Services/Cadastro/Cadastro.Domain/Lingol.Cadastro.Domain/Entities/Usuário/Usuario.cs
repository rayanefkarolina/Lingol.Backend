using Lingol.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Domain.Entities.Usuário
{
    public abstract class Usuario : EntityBase
    {
        public string Nome { get; protected set; } = default!;
        public string Email { get; protected set; } = string.Empty;

        protected Usuario() { }

        protected Usuario(string nome, string email)
        {
            Nome = nome;
            Email = email;
        }
    }
}
