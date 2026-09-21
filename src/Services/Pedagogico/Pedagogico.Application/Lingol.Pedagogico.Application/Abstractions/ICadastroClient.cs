using Lingol.Contracts.Cadastro.Dtos;

namespace Lingol.Pedagogico.Application.Abstractions
{
    public interface ICadastroClient
    {
        Task<TurmaCadastroDto?> ObterTurmaAsync(Guid turmaId, CancellationToken ct);
        Task<AlunoCadastroDto?> ObterAlunoAsync(Guid alunoId, CancellationToken ct);
        Task<List<AlunoCadastroDto>> ObterAlunosDaTurmaAsync(Guid turmaId, CancellationToken ct);
    }
}
