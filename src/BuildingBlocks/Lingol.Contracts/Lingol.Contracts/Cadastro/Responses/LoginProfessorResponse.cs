using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Contracts.Cadastro.Responses
{
    public record LoginProfessorResponse(
        string AccessToken,
        DateTime ExpiraEm,
        Guid ProfessorId,
        string NomeProfessor,
        string Email);
}
