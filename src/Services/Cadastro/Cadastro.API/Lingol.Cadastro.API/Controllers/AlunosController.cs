using Lingol.Cadastro.Infrastructure.Persistence;
using Lingol.Contracts.Cadastro.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lingol.Cadastro.API.Controllers;

[ApiController]
[Route("api/alunos")]
[Authorize] // exige usuário autenticado
public class AlunosController : ControllerBase
{
    private readonly CadastroDbContext _db;

    public AlunosController(CadastroDbContext db)
    {
        _db = db;
    }

    // 1) Obter aluno por Id
    // Professor pode ver qualquer aluno.
    // Aluno só pode ver a si mesmo.
    [HttpGet("{alunoId:guid}")]
    [Authorize(Roles = "Professor,Aluno")]
    public async Task<IActionResult> ObterAlunoPorId(Guid alunoId, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);

        if (userIdClaim is null || roleClaim is null)
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim);

        // Se for aluno e o alunoId da rota não for o mesmo da claim, bloqueia.
        if (roleClaim == "Aluno" && userId != alunoId)
            return Forbid(); // 403

        var aluno = await _db.Alunos
            .Include(a => a.PerfilAee)
            .FirstOrDefaultAsync(a => a.Id == alunoId, ct);

        if (aluno is null)
            return NotFound();

        var dto = new AlunoCadastroDto(
            aluno.Id,
            aluno.Nome,
            aluno.Matricula,
            aluno.TurmaId,
            aluno.PerfilAee?.TipoNecessidade);

        return Ok(dto);
    }

    // 2) (Opcional) Listar alunos de uma turma - apenas professor
    [HttpGet("turma/{turmaId:guid}")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> ListarAlunosPorTurma(Guid turmaId, CancellationToken ct)
    {
        var alunos = await _db.Alunos
            .Include(a => a.PerfilAee)
            .Where(a => a.TurmaId == turmaId)
            .Select(a => new AlunoCadastroDto(
                a.Id,
                a.Nome,
                a.Matricula,
                a.TurmaId,
                a.PerfilAee != null ? a.PerfilAee.TipoNecessidade : null))
            .ToListAsync(ct);

        return Ok(alunos);
    }
}