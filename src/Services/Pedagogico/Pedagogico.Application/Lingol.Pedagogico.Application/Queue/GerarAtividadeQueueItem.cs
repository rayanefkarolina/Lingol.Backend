using Lingol.Pedagogico.Domain.Entities;

namespace Lingol.Pedagogico.Application.Queue
{
    /// <summary>Item da fila in-process (System.Threading.Channels) de geração de atividade.</summary>
    public class GerarAtividadeQueueItem
    {
        public Guid AtividadeId { get; set; }
        public Guid TurmaId { get; set; }
        public string Livro { get; set; } = default!;
        public string CapituloOuAssunto { get; set; } = default!;
        public string Materia { get; set; } = default!;
        public int Ano { get; set; } = 6;
        public int NumQuestoes { get; set; } = 10;
        public ModoGamificacao Modo { get; set; } = ModoGamificacao.Simples;
        public string Tema { get; set; } = TemasAtividade.Fantasia;
        public string? PerfilAeeContexto { get; set; }
        public DateTime EnfileiradoEm { get; set; } = DateTime.UtcNow;
    }
}
