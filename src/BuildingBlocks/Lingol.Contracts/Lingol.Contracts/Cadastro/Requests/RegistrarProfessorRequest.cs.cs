using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Contracts.Cadastro.Requests
{
    public record RegistrarProfessorRequest(
        [Required(ErrorMessage = "O nome é obrigatório.")]
    string Nome,

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "O e-mail deve estar em um formato válido. Exemplo: teste@gmail.com.")]
    string Email,

        [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(8, ErrorMessage = "A senha deve ter no mínimo 8 caracteres.")]
    string Senha);
}
