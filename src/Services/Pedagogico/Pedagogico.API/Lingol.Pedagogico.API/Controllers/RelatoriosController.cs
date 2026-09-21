using Lingol.Pedagogico.API.Services;
using Lingol.Pedagogico.Application.Queries;
using Lingol.Pedagogico.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lingol.Pedagogico.API.Controllers;

/// <summary>
/// Dashboard diagnóstico: onde o professor identifica em poucos minutos quais
/// conceitos a turma travou, sem corrigir nada no papel.
/// </summary>
[ApiController]
[Route("api/relatorios")]
[Authorize]
public class RelatoriosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly PedagogicoDbContext _db;
    private readonly AcessoTurma _acesso;

    public RelatoriosController(IMediator mediator, PedagogicoDbContext db, AcessoTurma acesso)
    {
        _mediator = mediator;
        _db = db;
        _acesso = acesso;
    }

    // ================================================================
    // GET /api/relatorios/turmas/{turmaId}
    // ================================================================
    [HttpGet("turmas/{turmaId:guid}")]
    [Authorize(Policy = "ProfessorPolicy")]
    public async Task<IActionResult> ObterRelatorioTurma(Guid turmaId, CancellationToken ct)
    {
        if (!await _acesso.ProfessorEhDonoAsync(User, turmaId, ct))
            return StatusCode(StatusCodes.Status403Forbidden, new { erro = "Esta turma não pertence a você." });

        try
        {
            var relatorio = await _mediator.Send(new ObterRelatorioDificuldadesTurmaQuery(turmaId), ct);
            return Ok(relatorio);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    // ================================================================
    // GET /api/relatorios/turmas/{turmaId}/alunos/{alunoId}
    // Professor dono da turma, ou o próprio aluno.
    // ================================================================
    [HttpGet("turmas/{turmaId:guid}/alunos/{alunoId:guid}")]
    public async Task<IActionResult> ObterRelatorioAluno(Guid turmaId, Guid alunoId, CancellationToken ct)
    {
        if (User.IsInRole("Aluno"))
        {
            var alunoLogado = AcessoTurma.ObterUsuarioId(User);
            if (alunoLogado != alunoId || AcessoTurma.ObterTurmaIdDoAluno(User) != turmaId)
                return StatusCode(StatusCodes.Status403Forbidden, new { erro = "Você só pode ver o seu próprio relatório." });
        }
        else if (!await _acesso.ProfessorEhDonoAsync(User, turmaId, ct))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { erro = "Esta turma não pertence a você." });
        }

        try
        {
            var relatorio = await _mediator.Send(new ObterRelatorioDificuldadesAlunoQuery(turmaId, alunoId), ct);
            return Ok(relatorio);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    // ================================================================
    // GET /api/relatorios/atividades/{atividadeId}
    // ================================================================
    [HttpGet("atividades/{atividadeId:guid}")]
    [Authorize(Policy = "ProfessorPolicy")]
    public async Task<IActionResult> ObterRelatorioAtividade(Guid atividadeId, CancellationToken ct)
    {
        var turmaId = await _db.Atividades
            .Where(a => a.Id == atividadeId)
            .Select(a => (Guid?)a.TurmaId)
            .FirstOrDefaultAsync(ct);

        if (turmaId is null)
            return NotFound();

        if (!await _acesso.ProfessorEhDonoAsync(User, turmaId.Value, ct))
            return StatusCode(StatusCodes.Status403Forbidden, new { erro = "Esta atividade não pertence a você." });

        try
        {
            var relatorio = await _mediator.Send(new ObterRelatorioAtividadeQuery(atividadeId), ct);
            return Ok(relatorio);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }
}
