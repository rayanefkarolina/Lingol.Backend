using Lingol.Cadastro.Infrastructure.Persistence;
using Lingol.Cadastro.Infrastructure.Security;
using Lingol.Contracts.Cadastro.Requests;
using Lingol.Contracts.Cadastro.Responses;
using Lingol.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Lingol.Cadastro.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly CadastroDbContext _db;
    private readonly IConfiguration _config;
 

    public AuthController(CadastroDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("professor/login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginProfessor(
        [FromBody] LoginProfessorRequest request,
        CancellationToken ct)
    {
        var emailNormalizado = request.Email.Trim().ToLowerInvariant();

        var professor = await _db.Professores
            .FirstOrDefaultAsync(p => p.Email.ToLower() == emailNormalizado, ct);

        if (professor is null)
            return Unauthorized();

        var senhaHash = SenhaHasher.Hash(request.Senha);
        if (professor.SenhaHash != senhaHash)
            return Unauthorized();

        // HS256 exige chave de no minimo 256 bits (32 bytes).
        var jwtKey = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key nao configurada.");
        var jwtIssuer = _config["Jwt:Issuer"] ?? "Lingol.Auth";
        var jwtAudience = _config["Jwt:Audience"] ?? "Lingol.Client";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            // identificador único do usuário
            new(ClaimTypes.NameIdentifier, professor.Id.ToString()),
            new(ClaimTypes.Name, professor.Nome),
            new(ClaimTypes.Email, professor.Email),

            // role para autorização
            new(ClaimTypes.Role, "Professor"),
            new("role", "Professor") // opcional, caso queira ler role como string simples
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var response = new LoginProfessorResponse(
            AccessToken: tokenString,
            ExpiraEm: token.ValidTo,
            ProfessorId: professor.Id,
            NomeProfessor: professor.Nome,
            Email: professor.Email);

        return Ok(response);
    }

    [HttpPost("aluno/login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAluno(
        [FromBody] LoginAlunoRequest request,
        CancellationToken ct)
    {
        var nomeNormalizado = request.Nome.Trim().ToLowerInvariant();

        var aluno = await _db.Alunos
            .Include(a => a.PerfilAee)
            .FirstOrDefaultAsync(a =>
                a.Nome.ToLower() == nomeNormalizado &&
                a.Matricula == request.Matricula, ct);

        if (aluno is null)
            return Unauthorized();

        var turma = await _db.Turmas.FirstOrDefaultAsync(t => t.Id == aluno.TurmaId, ct);
        if (turma is null)
            return Unauthorized();

        // HS256 exige chave de no minimo 256 bits (32 bytes).
        var jwtKey = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key nao configurada.");
        var jwtIssuer = _config["Jwt:Issuer"] ?? "Lingol.Auth";
        var jwtAudience = _config["Jwt:Audience"] ?? "Lingol.Client";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            // identificador único do aluno
            new(ClaimTypes.NameIdentifier, aluno.Id.ToString()),
            new(ClaimTypes.Name, aluno.Nome),

            // informações úteis para outras APIs
            new("matricula", aluno.Matricula),
            new("turmaId", aluno.TurmaId.ToString()),
            new("perfilAee", aluno.PerfilAee?.TipoNecessidade ?? string.Empty),

            // role para autorização
            new(ClaimTypes.Role, "Aluno"),
            new("role", "Aluno")
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var response = new LoginAlunoResponse(
            AccessToken: tokenString,
            ExpiraEm: token.ValidTo,
            AlunoId: aluno.Id,
            TurmaId: aluno.TurmaId,
            NomeAluno: aluno.Nome,
            Matricula: aluno.Matricula);

        return Ok(response);
    }

    [HttpPost("professor/register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegistrarProfessor(
        [FromBody] RegistrarProfessorRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var emailNormalizado = request.Email.Trim().ToLowerInvariant();

        var professorExistente = await _db.Professores
            .AnyAsync(p => p.Email.ToLower() == emailNormalizado, ct);

        if (professorExistente)
            return BadRequest(new { mensagem = "Já existe um professor cadastrado com este e-mail." });

        var professor = new Professor(
             request.Nome.Trim(),
             emailNormalizado,
             SenhaHasher.Hash(request.Senha));

        _db.Professores.Add(professor);
        await _db.SaveChangesAsync(ct);

        var response = new RegistrarProfessorResponse(
            professor.Id,
            professor.Nome,
            professor.Email);

        return CreatedAtAction(nameof(RegistrarProfessor), new { id = professor.Id }, response);
    }
}