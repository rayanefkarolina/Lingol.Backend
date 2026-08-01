using Lingol.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Pedagogico.Domain.Entities
{
    public class Questao : EntityBase
    {
        public Guid AtividadeId { get; private set; }
        public string Enunciado { get; private set; } = default!;
        public string TipoQuestao { get; private set; } = default!;
        public string GabaritoOuCriterio { get; private set; } = default!;
        public string? AlternativasJson { get; private set; }

        private Questao() { }

        public Questao(
            Guid atividadeId,
            string enunciado,
            string tipoQuestao,
            string gabaritoOuCriterio,
            IEnumerable<string>? alternativas = null)
        {
            AtividadeId = atividadeId;
            Enunciado = enunciado;
            TipoQuestao = tipoQuestao;
            GabaritoOuCriterio = gabaritoOuCriterio;
            AlternativasJson = alternativas is null
                ? null
                : System.Text.Json.JsonSerializer.Serialize(alternativas);
        }
    }
}
