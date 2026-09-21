using System.ComponentModel.DataAnnotations;

namespace Lingol.Contracts.Cadastro.Requests
{
    public class CadastrarAlunoRequest
    {
        [Required(ErrorMessage = "O nome do aluno é obrigatório.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "A matrícula é obrigatória.")]
        public string Matricula { get; set; } = string.Empty;

        // Perfil de Atendimento Educacional Especializado (opcional).
        public string? TipoNecessidadeAee { get; set; }

        public string? ObservacoesAee { get; set; }
    }
}
