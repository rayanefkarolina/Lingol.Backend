using System.Security.Claims;
using Lingol.Pedagogico.API.Services;
using Lingol.Pedagogico.Application.Commands;
using Lingol.Pedagogico.Domain.Entities;
using Lingol.Pedagogico.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lingol.Pedagogico.API.Controllers;

[ApiController]
[Route("api/atividades")]
[Authorize]
public class AtividadesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly PedagogicoDbContext _db;
    private readonly AcessoTurma _acesso;
    private readonly ILogger<AtividadesController> _logger;

    public AtividadesController(
        IMediator mediator,
        PedagogicoDbContext db,
        AcessoTurma acesso,
        ILogger<AtividadesController> logger)
    {
        _mediator = mediator;
        _db = db;
        _acesso = acesso;
        _logger = logger;
    }

    // ================================================================
    // POST /api/atividades/gerar  -> 202 Accepted (geração em background)
    // ================================================================
    [HttpPost("gerar")]
    [Authorize(Policy = "ProfessorPolicy")]
    public async Task<IActionResult> GerarAtividade(
        [FromBody] GerarAtividadeRequest request,
        CancellationToken cancellationToken)
    {
        var professorId = ObterUsuarioId();
        if (professorId is null)
            return Unauthorized();

        if (!Enum.TryParse<ModoGamificacao>(request.Modo, ignoreCase: true, out var modo))
            modo = ModoGamificacao.Simples;

        _logger.LogInformation("Professor {ProfessorId} solicitou atividade para a turma {TurmaId}",
            professorId, request.TurmaId);

        var command = new CriarAtividadeCommand(
            TurmaId: request.TurmaId,
            ProfessorId: professorId.Value,
            Livro: request.Livro,
            CapituloOuAssunto: request.Assunto,
            Materia: request.Materia ?? "Língua Portuguesa",
            NumQuestoes: request.NumQuestoes ?? 10,
            Modo: modo,
            PerfilAeeContexto: request.PerfilAeeContexto);

        try
        {
            var result = await _mediator.Send(command, cancellationToken);

            return Accepted(
                $"/api/atividades/{result.AtividadeId}",
                new
                {
                    atividadeId = result.AtividadeId,
                    status = result.Status,
                    criadoEm = result.CriadoEm,
                    mensagem = "Atividade enfileirada para geração. Você será notificado quando estiver pronta."
                });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { erro = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    // ================================================================
    // GET /api/atividades/turmas/{turmaId}
    // ================================================================
    [HttpGet("turmas/{turmaId:guid}")]
    public async Task<IActionResult> ListarPorTurma(Guid turmaId, CancellationToken cancellationToken)
    {
        // Aluno: só a própria turma. Professor: só as turmas que ele criou.
        if (!await _acesso.PodeVerTurmaAsync(User, turmaId, cancellationToken))
            return StatusCode(StatusCodes.Status403Forbidden, new { erro = "Você não tem acesso a esta turma." });

        var atividades = await _db.Atividades
            .Where(a => a.TurmaId == turmaId)
            .OrderByDescending(a => a.DataCriacao)
            .Select(a => new
            {
                id = a.Id,
                livro = a.Livro,
                assunto = a.CapituloOuAssunto,
                materia = a.Materia,
                modo = a.Modo.ToString(),
                status = a.Status.ToString(),
                dataCriacao = a.DataCriacao,
                numQuestoes = a.Questoes.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(atividades);
    }

    // ================================================================
    // GET /api/atividades/{id}
    // ================================================================
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterAtividade(Guid id, CancellationToken cancellationToken)
    {
        var atividade = await _db.Atividades
            .Include(a => a.Questoes)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (atividade is null)
            return NotFound();

        if (!await _acesso.PodeVerTurmaAsync(User, atividade.TurmaId, cancellationToken))
            return StatusCode(StatusCodes.Status403Forbidden, new { erro = "Você não tem acesso a esta atividade." });

        return Ok(new
        {
            id = atividade.Id,
            turmaId = atividade.TurmaId,
            livro = atividade.Livro,
            assunto = atividade.CapituloOuAssunto,
            materia = atividade.Materia,
            modo = atividade.Modo.ToString(),
            status = atividade.Status.ToString(),
            dataCriacao = atividade.DataCriacao,
            mensagemErro = atividade.MensagemErro,
            numQuestoes = atividade.Questoes.Count
        });
    }

    // ================================================================
    // GET /api/atividades/{id}/questoes  (sem gabarito)
    // ================================================================
    [HttpGet("{id:guid}/questoes")]
    public async Task<IActionResult> ObterQuestoes(Guid id, CancellationToken cancellationToken)
    {
        var atividade = await _db.Atividades
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (atividade is null)
            return NotFound();

        if (!await _acesso.PodeVerTurmaAsync(User, atividade.TurmaId, cancellationToken))
            return StatusCode(StatusCodes.Status403Forbidden, new { erro = "Você não tem acesso a esta atividade." });

        if (atividade.Status != StatusAtividade.Pronta)
            return Conflict(new { status = atividade.Status.ToString(), mensagem = "A atividade ainda não está pronta." });

        var questoes = await _db.Questoes
            .Where(q => q.AtividadeId == id)
            .OrderBy(q => q.Ordem)
            .ToListAsync(cancellationToken);

        var payload = questoes.Select(q => new
        {
            id = q.Id,
            ordem = q.Ordem,
            enunciado = q.Enunciado,
            tipo = q.TipoQuestao,
            alternativas = q.ObterAlternativas()
        });

        return Ok(payload);
    }

    // ================================================================
    // POST /api/atividades/{id}/respostas
    // ================================================================
    [HttpPost("{id:guid}/respostas")]
    [Authorize(Policy = "AlunoPolicy")]
    public async Task<IActionResult> EnviarRespostas(
        Guid id,
        [FromBody] EnviarRespostasRequest request,
        CancellationToken cancellationToken)
    {
        var alunoId = ObterUsuarioId();
        if (alunoId is null)
            return Unauthorized();

        _logger.LogInformation("Aluno {AlunoId} enviando respostas da atividade {AtividadeId}", alunoId, id);

        var command = new EnviarRespostasCommand(id, alunoId.Value, request.Respostas);

        try
        {
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new
            {
                atividadeId = id,
                respostaAlunoId = result.RespostaAlunoId,
                acertos = result.Acertos,
                erros = result.Erros,
                totalQuestoes = result.TotalQuestoes,
                nota = result.Nota,
                correcoes = result.Correcoes,
                mensagem = $"Você acertou {result.Acertos}/{result.TotalQuestoes} questões!"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { erro = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    // ----------------------------------------------------------------
    // Helpers de claims
    // ----------------------------------------------------------------

    private Guid? ObterUsuarioId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

// ================================================================
// DTOs de Request
// ================================================================

public class GerarAtividadeRequest
{
    public Guid TurmaId { get; set; }
    public string Livro { get; set; } = default!;
    public string Assunto { get; set; } = default!;
    public string? Materia { get; set; }
    public int? NumQuestoes { get; set; }

    /// <summary>"Simples" (questionário) ou "Rpg" (atividade gamificada).</summary>
    public string? Modo { get; set; }

    public string? PerfilAeeContexto { get; set; }
}

public class EnviarRespostasRequest
{
    public List<RespostaAlunoDto> Respostas { get; set; } = new();
}
