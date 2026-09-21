namespace Lingol.Pedagogico.Application.Queue
{
    /// <summary>
    /// Item da fila de diagnóstico pedagógico. O placar objetivo (acertos/erros) já
    /// foi devolvido ao aluno na hora; aqui a IA analisa os erros e classifica as
    /// dificuldades que alimentam o dashboard do professor.
    /// </summary>
    public class CorrigirEntregaQueueItem
    {
        public Guid RespostaAlunoId { get; set; }
        public Guid AtividadeId { get; set; }
        public Guid TurmaId { get; set; }
        public Guid AlunoId { get; set; }
        public DateTime EnfileiradoEm { get; set; } = DateTime.UtcNow;
    }
}
