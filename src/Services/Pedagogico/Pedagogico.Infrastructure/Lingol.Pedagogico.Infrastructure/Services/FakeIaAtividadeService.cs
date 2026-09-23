using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Application.Dtos;
using Lingol.Pedagogico.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Lingol.Pedagogico.Infrastructure.Services
{
    /// <summary>
    /// Implementação de desenvolvimento: gera questões determinísticas sem chamar
    /// nenhum provedor externo. Permite testar o fluxo completo (202 -> fila ->
    /// SignalR -> aluno responde -> relatório) antes da integração com o Gemini.
    /// Ativada por IaProvider:UseFake = true.
    /// </summary>
    public class FakeIaAtividadeService : IIaAtividadeService
    {
        private static readonly string[] Letras = { "A", "B", "C", "D" };

        private readonly ILogger<FakeIaAtividadeService> _logger;

        public FakeIaAtividadeService(ILogger<FakeIaAtividadeService> logger)
        {
            _logger = logger;
        }

        public async Task<List<QuestaoGeradaDto>> GerarAtividadeAsync(
            string livro,
            string capituloOuAssunto,
            string materia,
            int ano,
            int numQuestoes,
            ModoGamificacao modo,
            string tema,
            string? perfilAeeContexto,
            CancellationToken cancellationToken)
        {
            _logger.LogWarning(
                "FakeIaAtividadeService em uso: gerando {NumQuestoes} questões simuladas de {Assunto}",
                numQuestoes, capituloOuAssunto);

            // Simula a latência de uma chamada real à IA, para exercitar o fluxo assíncrono.
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            var questoes = new List<QuestaoGeradaDto>();

            for (var i = 1; i <= numQuestoes; i++)
            {
                var correta = Letras[i % Letras.Length];

                // O modo gamificado usa frase com lacuna e alternativas sem rótulo.
                var alternativas = modo == ModoGamificacao.Rpg
                    ? Letras.Select(l => $"palavra{l}").ToList()
                    : Letras.Select(l => $"{l}) Alternativa {l} da questão {i}").ToList();

                var enunciado = modo == ModoGamificacao.Rpg
                    ? $"O guerreiro enfrentou o dragão ________ na fase {i}."
                    : $"({materia} - {ano}º ano) Questão {i} sobre {capituloOuAssunto}, do livro \"{livro}\".";

                questoes.Add(new QuestaoGeradaDto(
                    Enunciado: enunciado,
                    Tipo: "MultiplaEscolha",
                    Alternativas: alternativas,
                    GabaritoOuCriterio: correta,
                    Explicacao: $"A alternativa {correta} está correta porque aplica a regra de {capituloOuAssunto}.",
                    Habilidade: capituloOuAssunto));
            }

            return questoes;
        }

        public Task<CorrecaoResultado> CorrigirRespostaAsync(
            Atividade atividade,
            RespostaAluno respostaAluno,
            CancellationToken cancellationToken)
        {
            var nota = respostaAluno.Nota ?? 0m;

            var resultado = new CorrecaoResultado(
                Nota: nota,
                FeedbackGeral: $"Você acertou {respostaAluno.Acertos} de {respostaAluno.TotalQuestoes} questões. Continue treinando, guerreiro!",
                Dificuldades: new List<DificuldadeIdentificadaDto>());

            return Task.FromResult(resultado);
        }
    }
}
