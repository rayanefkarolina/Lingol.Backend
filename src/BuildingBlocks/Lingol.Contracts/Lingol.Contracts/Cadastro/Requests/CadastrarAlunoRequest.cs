using System.ComponentModel.DataAnnotations;

namespace Lingol.Contracts.Cadastro.Requests
{
    public class CadastrarAlunoRequest
    {
        [Required(ErrorMessage = "O nome do aluno é obrigatório.")]
        public string Nome { get; set; } = string.Empty;

        // A matricula e gerada automaticamente pelo backend.

        // Perfil de Atendimento Educacional Especializado (opcional).
        public string? TipoNecessidadeAee { get; set; }

        public string? ObservacoesAee { get; set; }
    }
}
