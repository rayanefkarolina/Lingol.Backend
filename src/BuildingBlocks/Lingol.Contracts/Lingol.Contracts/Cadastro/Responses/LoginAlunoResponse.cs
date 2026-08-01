using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Contracts.Cadastro.Responses
{
    public record LoginAlunoResponse(
        string AccessToken,
        DateTime ExpiraEm,
        Guid AlunoId,
        Guid TurmaId,
        string NomeAluno,
        string Matricula);
}
