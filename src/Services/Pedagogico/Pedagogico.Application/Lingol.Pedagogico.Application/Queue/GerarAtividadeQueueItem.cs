using System.Text.Json;
using System.Text.Json.Serialization;
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

        /// <summary>
        /// Preenchido apenas em revisao: as lacunas daquele aluno, que viram
        /// o foco do prompt.
        /// </summary>
        public string? FocoRevisao { get; set; }
        public DateTime EnfileiradoEm { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Serializa o pedido para ficar guardado junto da atividade. Se o
        /// processo reiniciar no meio da geração, a varredura de recuperação
        /// reenfileira exatamente este mesmo item.
        /// </summary>
        public static string Serializar(GerarAtividadeQueueItem item) =>
            JsonSerializer.Serialize(item, OpcoesJson);

        /// <summary>Devolve null quando o JSON está ausente ou corrompido.</summary>
        public static GerarAtividadeQueueItem? Desserializar(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                return JsonSerializer.Deserialize<GerarAtividadeQueueItem>(json, OpcoesJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static readonly JsonSerializerOptions OpcoesJson = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };
    }
}
