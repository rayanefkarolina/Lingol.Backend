using Lingol.Pedagogico.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lingol.Pedagogico.API.Controllers;

[ApiController]
[Route("api/atividades")]
[Authorize]
public class AtividadesController : ControllerBase
{
    private readonly PedagogicoDbContext _db;
    private readonly MediatR.IMediator _mediator;
    private readonly Lingol.Pedagogico.Application.Abstractions.IIaAtividadeService _iaService;

    public AtividadesController(PedagogicoDbContext db, MediatR.IMediator mediator, Lingol.Pedagogico.Application.Abstractions.IIaAtividadeService iaService)
    {
        _db = db;
        _mediator = mediator;
        _iaService = iaService;
    }

    // GET /api/atividades/turmas/{turmaId}
    [HttpGet("turmas/{turmaId:guid}")]
    public async Task<IActionResult> ListarPorTurma(Guid turmaId, CancellationToken ct)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role == "Aluno")
        {
            var turmaClaim = User.FindFirstValue("turmaId");
            if (turmaClaim is null) return Unauthorized();
            if (!Guid.TryParse(turmaClaim, out var turmaClaimId) || turmaClaimId != turmaId)
                return Forbid();
        }

        var atividades = await _db.Atividades
            .Where(a => a.TurmaId == turmaId)
            .Select(a => new
            {
                a.Id,
                a.Livro,
                a.CapituloOuAssunto,
                QuestoesCount = a.Questoes.Count
            })
            .ToListAsync(ct);

        return Ok(atividades);
    }

    // GET /api/atividades/{atividadeId}
    [HttpGet("{atividadeId:guid}")]
    public async Task<IActionResult> ObterPorId(Guid atividadeId, CancellationToken ct)
    {
        var atividade = await _db.Atividades
            .Include(a => a.Questoes)
            .FirstOrDefaultAsync(a => a.Id == atividadeId, ct);

        if (atividade is null)
            return NotFound();

        // Se usuário for aluno, validar pertence à mesma turma
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role == "Aluno")
        {
            var turmaClaim = User.FindFirstValue("turmaId");
            if (turmaClaim is null) return Unauthorized();
            if (!Guid.TryParse(turmaClaim, out var turmaClaimId) || turmaClaimId != atividade.TurmaId)
                return Forbid();
        }

        var dto = new
        {
            atividade.Id,
            atividade.Livro,
            atividade.CapituloOuAssunto,
            Questoes = atividade.Questoes.Select(q => new
            {
                q.Id,
                q.Enunciado,
                q.TipoQuestao,
                q.AlternativasJson
            })
        };

        return Ok(dto);
    }

    // POST /api/atividades/gerar
    [HttpPost("gerar")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> GerarAtividade([FromBody] GerarAtividadeRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // Monta o command e delega para MediatR
        var command = new Lingol.Pedagogico.Application.Commands.CriarAtividadeCommand(
            request.TurmaId,
            request.Livro,
            request.CapituloOuAssunto,
            request.Materia,
            request.PerfilAeeContexto);

        var result = await _mediator.Send(command, ct);

        return CreatedAtAction(nameof(ObterPorId), new { atividadeId = result.AtividadeId }, result);
    }

    public record GerarAtividadeRequest(Guid TurmaId, string Livro, string CapituloOuAssunto, string Materia, string? PerfilAeeContexto);

    // POST /api/atividades/{atividadeId}/respostas
    [HttpPost("{atividadeId:guid}/respostas")]
    [Authorize(Roles = "Aluno")]
    public async Task<IActionResult> EnviarResposta(Guid atividadeId, [FromBody] EnviarRespostaRequest request, CancellationToken ct)
    {
        var alunoIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (alunoIdClaim is null) return Unauthorized();

        var alunoId = Guid.Parse(alunoIdClaim);

        var atividade = await _db.Atividades
            .Include(a => a.Questoes)
            .FirstOrDefaultAsync(a => a.Id == atividadeId, ct);

        if (atividade is null) return NotFound();

        // Cria RespostaAluno simples (detalhes das respostas podem ser incorporados depois)
        var resposta = new Lingol.Pedagogico.Domain.Entities.RespostaAluno(atividadeId, alunoId);
        _db.RespostasAluno.Add(resposta);
        await _db.SaveChangesAsync(ct);

        // Chama serviço de IA para correção
        var correcao = await _iaService.CorrigirRespostaAsync(atividade, resposta, ct);

        // Atualiza resposta
        resposta.DefinirCorrecao(correcao.Nota, correcao.FeedbackGeral);
        resposta.MarcarCorrecaoProcessada();

        // Persiste dificuldades identificadas
        foreach (var d in correcao.Dificuldades)
        {
            var dif = new Lingol.Pedagogico.Domain.Entities.DificuldadeAluno(
                resposta.Id,
                atividade.Id,
                atividade.TurmaId,
                alunoId,
                d.QuestaoId,
                d.Tipo,
                d.Descricao);

            _db.DificuldadesAluno.Add(dif);
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new { respostaId = resposta.Id, nota = resposta.Nota, feedback = resposta.FeedbackGeral });
    }

    public record EnviarRespostaRequest(string? RespostasJson);
}
