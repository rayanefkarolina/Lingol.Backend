using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Lingol.Pedagogico.Application.Dtos.CadastroDtos;

namespace Lingol.Pedagogico.Application.Abstractions
{

    public interface ICadastroClient
    {
        Task<TurmaCadastroDto?> ObterTurmaAsync(Guid turmaId, CancellationToken ct);
        Task<AlunoCadastroDto?> ObterAlunoAsync(Guid alunoId, CancellationToken ct);
        Task<List<AlunoCadastroDto>> ObterAlunosDaTurmaAsync(Guid turmaId, CancellationToken ct);
    }
}
