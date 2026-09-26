using Microsoft.EntityFrameworkCore;

namespace Lingol.Cadastro.Infrastructure.Persistence;

/// <summary>
/// Gera a matrícula do aluno no formato {ano}{sequencial}, ex.: 2026001.
/// O professor não digita mais esse número, e ele é único em toda a base
/// porque o aluno entra na plataforma usando apenas a matrícula.
/// </summary>
public class GeradorMatricula
{
    private const int TamanhoSequencial = 3;

    private readonly CadastroDbContext _db;

    public GeradorMatricula(CadastroDbContext db)
    {
        _db = db;
    }

    public async Task<string> GerarAsync(CancellationToken ct = default)
    {
        var prefixo = DateTime.UtcNow.Year.ToString();

        var ultima = await _db.Alunos
            .Where(a => a.Matricula.StartsWith(prefixo))
            .OrderByDescending(a => a.Matricula)
            .Select(a => a.Matricula)
            .FirstOrDefaultAsync(ct);

        var proximo = 1;

        if (ultima is not null &&
            ultima.Length > prefixo.Length &&
            int.TryParse(ultima[prefixo.Length..], out var sequencialAtual))
        {
            proximo = sequencialAtual + 1;
        }

        return prefixo + proximo.ToString($"D{TamanhoSequencial}");
    }
}
