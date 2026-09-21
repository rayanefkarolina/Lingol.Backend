namespace Lingol.Pedagogico.Application.Abstractions
{
    /// <summary>
    /// Notificação em tempo real para o painel do professor.
    /// Implementado na camada de API (SignalR), para que a Infrastructure
    /// não precise conhecer o Hub.
    /// </summary>
    public interface IAtividadeNotifier
    {
        Task NotificarAtividadeProntaAsync(
            Guid turmaId,
            Guid atividadeId,
            int numQuestoes,
            CancellationToken cancellationToken = default);

        Task NotificarErroAsync(
            Guid turmaId,
            Guid atividadeId,
            string erro,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Avisa o painel de que o diagnóstico de dificuldades de uma entrega ficou
        /// pronto — é o gatilho para o dashboard recarregar o mapa de lacunas.
        /// </summary>
        Task NotificarCorrecaoProntaAsync(
            Guid turmaId,
            Guid atividadeId,
            Guid alunoId,
            int dificuldadesIdentificadas,
            CancellationToken cancellationToken = default);
    }
}
