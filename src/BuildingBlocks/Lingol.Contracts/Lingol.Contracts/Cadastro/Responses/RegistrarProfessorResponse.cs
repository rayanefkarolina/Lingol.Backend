using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Contracts.Cadastro.Responses
{
    public record RegistrarProfessorResponse(
    Guid Id,
    string Nome,
    string Email);
}
