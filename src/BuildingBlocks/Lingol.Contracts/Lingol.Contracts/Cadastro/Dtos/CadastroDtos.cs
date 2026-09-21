namespace Lingol.Contracts.Cadastro.Dtos;

public record TurmaCadastroDto(
    Guid Id,
    string Nome,
    string Materia,
    int Ano,
    Guid ProfessorId);

public record AlunoCadastroDto(
    Guid Id,
    string Nome,
    string Matricula,
    Guid TurmaId,
    string? TipoNecessidade);
