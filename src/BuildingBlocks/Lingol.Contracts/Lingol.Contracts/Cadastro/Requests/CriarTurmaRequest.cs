using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Contracts.Cadastro.Requests
{
    public class CriarTurmaRequest
    {
        [Required(ErrorMessage = "O nome da turma é obrigatório.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "A matéria é obrigatória.")]
        public string Materia { get; set; } = "Língua Portuguesa";
    }
}
