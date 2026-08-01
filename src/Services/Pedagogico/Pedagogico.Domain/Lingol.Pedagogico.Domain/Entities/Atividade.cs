using Lingol.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Pedagogico.Domain.Entities
{
    public class Atividade : EntityBase
    {
        private readonly List<Questao> _questoes = new();

        public Guid TurmaId { get; private set; }
        public string Livro { get; private set; } = default!;
        public string CapituloOuAssunto { get; private set; } = default!;
        public IReadOnlyCollection<Questao> Questoes => _questoes;

        private Atividade() { }

        public Atividade(Guid turmaId, string livro, string capituloOuAssunto)
        {
            TurmaId = turmaId;
            Livro = livro;
            CapituloOuAssunto = capituloOuAssunto;
        }

        public void AdicionarQuestao(Questao questao)
        {
            _questoes.Add(questao);
        }
    }
}
