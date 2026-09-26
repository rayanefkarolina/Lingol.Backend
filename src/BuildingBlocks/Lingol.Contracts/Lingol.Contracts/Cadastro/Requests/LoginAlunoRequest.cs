using System.ComponentModel.DataAnnotations;

namespace Lingol.Contracts.Cadastro.Requests
{
    /// <summary>
    /// Acesso sem friccao: o aluno informa apenas o numero de matricula,
    /// que e unico em toda a base.
    /// </summary>
    public record LoginAlunoRequest(
        [Required(ErrorMessage = "Informe o numero de matricula.")]
        string Matricula);
}
