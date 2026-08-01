using Lingol.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Domain.Entities
{
    public class PerfilAee: EntityBase
    {
        public Guid AlunoId { get; private set; }
        public string TipoNecessidade { get; private set; } = default!;
        public string Observacoes { get; private set; } = string.Empty;

        private PerfilAee() { }

        public PerfilAee(Guid alunoId, string tipoNecessidade, string observacoes)
        {
            AlunoId = alunoId;
            TipoNecessidade = tipoNecessidade;
            Observacoes = observacoes;
        }
    }
}
