using System.ComponentModel.DataAnnotations;

namespace Lingol.Contracts.Cadastro.Requests
{
    public class CriarTurmaRequest
    {
        [Required(ErrorMessage = "O nome da turma é obrigatório.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "A matéria é obrigatória.")]
        public string Materia { get; set; } = "Língua Portuguesa";

        [Range(1, 9, ErrorMessage = "O ano da turma deve estar entre 1 e 9.")]
        public int Ano { get; set; } = 6;
    }
}
