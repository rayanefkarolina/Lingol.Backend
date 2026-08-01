using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Contracts.Cadastro.Requests
{
    public class CadastrarAlunoRequest
    {
        [Required(ErrorMessage = "O nome do aluno é obrigatório.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "A matrícula é obrigatória.")]
        public string Matricula { get; set; } = string.Empty;
    }
}
