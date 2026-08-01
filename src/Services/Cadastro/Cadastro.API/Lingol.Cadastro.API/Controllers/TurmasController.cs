using Lingol.Cadastro.Infrastructure.Persistence;
using Lingol.Contracts.Cadastro.Requests;
using Lingol.Domain.Entities;
using Lingol.Pedagogico.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Lingol.Pedagogico.Application.Dtos.CadastroDtos;

namespace Lingol.Cadastro.API.Controllers;

[ApiController]
[Route("api/turmas")]
[Authorize]
public class TurmasController : ControllerBase
{
    private readonly CadastroDbContext _db;

    public TurmasController(CadastroDbContext db)
    {
        _db = db;
    }

    // 1) Criar turma (professor)
    [HttpPost]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> CriarTurma(
        [FromBody] CriarTurmaRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var professorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (professorIdClaim is null)
            return Unauthorized();

        var professorId = Guid.Parse(professorIdClaim);

        // Domínio: use o construtor da Turma, não object initializer
        var turma = new Turma(
            request.Nome.Trim(),
            professorId,
            request.Materia.Trim());

        _db.Turmas.Add(turma);
        await _db.SaveChangesAsync(ct);

        var dto = new TurmaCadastroDto(
            turma.Id,
            turma.Nome,
            turma.Materia,
            turma.ProfessorId);

        return CreatedAtAction(nameof(ObterPorId), new { turmaId = turma.Id }, dto);
    }

    // 2) Listar turmas do professor logado
    [HttpGet]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> ListarTurmas(CancellationToken ct)
    {
        var professorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (professorIdClaim is null)
            return Unauthorized();

        var professorId = Guid.Parse(professorIdClaim);

        var turmas = await _db.Turmas
            .Where(t => t.ProfessorId == professorId)
            .Select(t => new TurmaCadastroDto(
                t.Id,
                t.Nome,
                t.Materia,
                t.ProfessorId))
            .ToListAsync(ct);

        return Ok(turmas);
    }

    // 3) Obter turma por Id (já tinha algo parecido)
    [HttpGet("{turmaId:guid}")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> ObterPorId(Guid turmaId, CancellationToken ct)
    {
        var professorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (professorIdClaim is null)
            return Unauthorized();

        var professorId = Guid.Parse(professorIdClaim);

        var turma = await _db.Turmas
            .FirstOrDefaultAsync(t => t.Id == turmaId && t.ProfessorId == professorId, ct);

        if (turma is null)
            return NotFound();

        var dto = new TurmaCadastroDto(
            turma.Id,
            turma.Nome,
            turma.Materia,
            turma.ProfessorId);

        return Ok(dto);
    }

    // 4) Cadastrar aluno em uma turma
    [HttpPost("{turmaId:guid}/alunos")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> CadastrarAluno(
        Guid turmaId,
        [FromBody] CadastrarAlunoRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var professorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (professorIdClaim is null)
            return Unauthorized();

        var professorId = Guid.Parse(professorIdClaim);

        var turma = await _db.Turmas
            .FirstOrDefaultAsync(t => t.Id == turmaId && t.ProfessorId == professorId, ct);

        if (turma is null)
            return NotFound("Turma não encontrada ou não pertence ao professor atual.");

        var matriculaNormalizada = request.Matricula.Trim();

        var alunoExistente = await _db.Alunos
            .AnyAsync(a => a.Matricula == matriculaNormalizada && a.TurmaId == turmaId, ct);

        if (alunoExistente)
            return BadRequest("Já existe aluno com essa matrícula na turma.");

        // Use o construtor de Aluno
        var aluno = new Aluno(
            request.Nome.Trim(),
            matriculaNormalizada,
            turmaId);

        _db.Alunos.Add(aluno);
        await _db.SaveChangesAsync(ct);

        var dto = new AlunoCadastroDto(
            aluno.Id,
            aluno.Nome,
            aluno.Matricula,
            aluno.TurmaId,
            aluno.PerfilAee?.TipoNecessidade);

        return CreatedAtAction(nameof(AlunosController.ObterAlunoPorId), "Alunos", new { alunoId = aluno.Id }, dto);
    }

    // 5) Listar alunos de uma turma
    [HttpGet("{turmaId:guid}/alunos")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> ListarAlunosTurma(Guid turmaId, CancellationToken ct)
    {
        var professorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (professorIdClaim is null)
            return Unauthorized();

        var professorId = Guid.Parse(professorIdClaim);

        var turmaExiste = await _db.Turmas
            .AnyAsync(t => t.Id == turmaId && t.ProfessorId == professorId, ct);

        if (!turmaExiste)
            return NotFound("Turma não encontrada ou não pertence ao professor atual.");

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

    // 6) Listar turmas do aluno logado
    [HttpGet("minhas")]
    [Authorize(Roles = "Aluno")]
    public async Task<IActionResult> ListarMinhasTurmas(CancellationToken ct)
    {
        var alunoIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var turmaIdClaim = User.FindFirstValue("turmaId");

        if (alunoIdClaim is null || turmaIdClaim is null)
            return Unauthorized();

        var turmaId = Guid.Parse(turmaIdClaim);

        // Se quiser validar que o aluno realmente está nessa turma:
        var alunoExiste = await _db.Alunos
            .AnyAsync(a => a.Id == Guid.Parse(alunoIdClaim) && a.TurmaId == turmaId, ct);

        if (!alunoExiste)
            return Forbid();

        var turma = await _db.Turmas
            .FirstOrDefaultAsync(t => t.Id == turmaId, ct);

        if (turma is null)
            return NotFound("Turma não encontrada.");

        var dto = new TurmaCadastroDto(
            turma.Id,
            turma.Nome,
            turma.Materia,
            turma.ProfessorId);

        return Ok(new[] { dto });
    }
}