using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Pedagogico.Application.Dtos
{
    public class CadastroDtos
    {
        public record TurmaCadastroDto(
            Guid Id,
            string Nome,
            string Materia,
            Guid ProfessorId);

        public record AlunoCadastroDto(
            Guid Id,
            string Nome,
            string Matricula,
            Guid TurmaId,
            string? TipoNecessidade);
    }
}
