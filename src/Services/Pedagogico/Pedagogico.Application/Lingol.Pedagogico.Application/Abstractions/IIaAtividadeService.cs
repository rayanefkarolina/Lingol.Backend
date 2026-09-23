using Lingol.Pedagogico.Application.Dtos;
using Lingol.Pedagogico.Domain.Entities;

namespace Lingol.Pedagogico.Application.Abstractions
{
    public interface IIaAtividadeService
    {
        Task<List<QuestaoGeradaDto>> GerarAtividadeAsync(
            string livro,
            string capituloOuAssunto,
            string materia,
            int ano,
            int numQuestoes,
            ModoGamificacao modo,
            string tema,
            string? perfilAeeContexto,
            CancellationToken cancellationToken);

        Task<CorrecaoResultado> CorrigirRespostaAsync(
            Atividade atividade,
            RespostaAluno respostaAluno,
            CancellationToken cancellationToken);
    }
}
