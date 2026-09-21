using System.Security.Claims;
using Lingol.Pedagogico.Application.Abstractions;

namespace Lingol.Pedagogico.API.Services;

/// <summary>
/// Regras de acesso a uma turma, consultando o microsserviço de Cadastro.
/// O token do usuário é repassado pelo AuthHeaderPropagationHandler, então o
/// Cadastro já devolve apenas o que aquele usuário pode ver.
/// </summary>
public class AcessoTurma
{
    private readonly ICadastroClient _cadastroClient;

    public AcessoTurma(ICadastroClient cadastroClient)
    {
        _cadastroClient = cadastroClient;
    }

    public static Guid? ObterUsuarioId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    public static Guid? ObterTurmaIdDoAluno(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue("turmaId");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>
    /// True quando o professor autenticado é dono da turma. O endpoint
    /// GET /api/turmas/{id} do Cadastro já filtra por dono, então uma turma de
    /// outro professor volta como nula.
    /// </summary>
    public async Task<bool> ProfessorEhDonoAsync(
        ClaimsPrincipal user,
        Guid turmaId,
        CancellationToken ct)
    {
        var professorId = ObterUsuarioId(user);
        if (professorId is null)
            return false;

        var turma = await _cadastroClient.ObterTurmaAsync(turmaId, ct);
        return turma is not null && turma.ProfessorId == professorId.Value;
    }

    /// <summary>Professor dono da turma, ou o próprio aluno matriculado nela.</summary>
    public async Task<bool> PodeVerTurmaAsync(
        ClaimsPrincipal user,
        Guid turmaId,
        CancellationToken ct)
    {
        if (user.IsInRole("Aluno"))
            return ObterTurmaIdDoAluno(user) == turmaId;

        return await ProfessorEhDonoAsync(user, turmaId, ct);
    }
}
