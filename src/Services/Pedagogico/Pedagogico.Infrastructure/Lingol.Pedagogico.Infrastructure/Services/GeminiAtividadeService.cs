using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Application.Dtos;
using Lingol.Pedagogico.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lingol.Pedagogico.Infrastructure.Services
{
    /// <summary>
    /// Integração com o Google AI Studio (Gemini). Usa JSON Mode
    /// (responseMimeType + responseSchema) para que a resposta seja desserializável
    /// direto em DTOs, sem parsing de texto livre.
    /// </summary>
    public class GeminiAtividadeService : IIaAtividadeService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiAtividadeService> _logger;

        public GeminiAtividadeService(
            HttpClient httpClient,
            IOptions<GeminiOptions> options,
            ILogger<GeminiAtividadeService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        // ================================================================
        // Geração da atividade adaptada
        // ================================================================

        public async Task<List<QuestaoGeradaDto>> GerarAtividadeAsync(
            string livro,
            string capituloOuAssunto,
            string materia,
            int ano,
            int numQuestoes,
            string? perfilAeeContexto,
            CancellationToken cancellationToken)
        {
            var prompt = GeminiPrompts.MontarPromptGeracao(
                livro, capituloOuAssunto, materia, ano, numQuestoes, perfilAeeContexto);

            using var json = await ChamarGeminiAsync(
                prompt,
                GeminiPrompts.InstrucaoSistemaGeracao,
                GeminiPrompts.SchemaGeracao(),
                cancellationToken);

            var questoes = new List<QuestaoGeradaDto>();

            if (!json.RootElement.TryGetProperty("questoes", out var arr) ||
                arr.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("A IA não devolveu o campo 'questoes'.");
            }

            foreach (var q in arr.EnumerateArray())
            {
                var alternativas = new List<string>();
                if (q.TryGetProperty("alternativas", out var alts) && alts.ValueKind == JsonValueKind.Array)
                    alternativas.AddRange(alts.EnumerateArray().Select(a => a.GetString() ?? string.Empty));

                var habilidade = q.TryGetProperty("habilidade", out var h) ? h.GetString() : null;

                questoes.Add(new QuestaoGeradaDto(
                    Enunciado: q.GetProperty("enunciado").GetString() ?? string.Empty,
                    Tipo: "MultiplaEscolha",
                    Alternativas: alternativas,
                    GabaritoOuCriterio: (q.GetProperty("gabarito").GetString() ?? string.Empty).Trim().ToUpperInvariant(),
                    Explicacao: q.TryGetProperty("explicacao", out var e) ? e.GetString() : null,
                    Habilidade: habilidade));
            }

            if (questoes.Count == 0)
                throw new InvalidOperationException("A IA devolveu uma lista de questões vazia.");

            if (questoes.Count != numQuestoes)
            {
                _logger.LogWarning(
                    "A IA devolveu {Recebidas} questões, mas foram pedidas {Pedidas}",
                    questoes.Count, numQuestoes);
            }

            return questoes;
        }

        // ================================================================
        // Diagnóstico pedagógico da entrega
        // ================================================================

        public async Task<CorrecaoResultado> CorrigirRespostaAsync(
            Atividade atividade,
            RespostaAluno respostaAluno,
            CancellationToken cancellationToken)
        {
            var questoesPorId = atividade.Questoes.ToDictionary(q => q.Id);

            var itens = respostaAluno.Itens
                .Where(i => questoesPorId.ContainsKey(i.QuestaoId))
                .Select(i => (Questao: questoesPorId[i.QuestaoId], Resposta: i))
                .OrderBy(x => x.Questao.Ordem)
                .ToList();

            if (itens.Count == 0)
                throw new InvalidOperationException("Entrega sem itens para diagnosticar.");

            var nota = respostaAluno.Nota ?? 0m;

            // Gabaritou: não há erro a diagnosticar, então poupamos uma chamada paga.
            if (respostaAluno.Erros == 0)
            {
                return new CorrecaoResultado(
                    nota,
                    $"Parabéns! Você acertou todas as {respostaAluno.TotalQuestoes} questões de {atividade.CapituloOuAssunto}.",
                    new List<DificuldadeIdentificadaDto>());
            }

            var prompt = GeminiPrompts.MontarPromptDiagnostico(atividade, respostaAluno, itens);

            using var json = await ChamarGeminiAsync(
                prompt,
                GeminiPrompts.InstrucaoSistemaDiagnostico,
                GeminiPrompts.SchemaDiagnostico(),
                cancellationToken);

            var raiz = json.RootElement;

            var feedback = raiz.TryGetProperty("feedbackGeral", out var f)
                ? f.GetString() ?? string.Empty
                : string.Empty;

            var dificuldades = new List<DificuldadeIdentificadaDto>();

            if (raiz.TryGetProperty("dificuldades", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var d in arr.EnumerateArray())
                {
                    var idTexto = d.GetProperty("questaoId").GetString();

                    // A IA pode alucinar um id: só aceitamos ids desta atividade.
                    if (!Guid.TryParse(idTexto, out var questaoId) || !questoesPorId.ContainsKey(questaoId))
                    {
                        _logger.LogWarning("Diagnóstico descartado: questaoId inválido '{Id}'", idTexto);
                        continue;
                    }

                    var tipoTexto = d.GetProperty("tipo").GetString() ?? "Interpretacao";
                    if (!Enum.TryParse<TipoDificuldade>(tipoTexto, ignoreCase: true, out var tipo))
                    {
                        _logger.LogWarning("Tipo de dificuldade desconhecido '{Tipo}', usando Interpretacao", tipoTexto);
                        tipo = TipoDificuldade.Interpretacao;
                    }

                    dificuldades.Add(new DificuldadeIdentificadaDto(
                        tipo,
                        questaoId,
                        d.GetProperty("descricao").GetString() ?? string.Empty));
                }
            }

            if (string.IsNullOrWhiteSpace(feedback))
            {
                feedback = $"Você acertou {respostaAluno.Acertos} de {respostaAluno.TotalQuestoes} questões. " +
                           "Não foi dessa vez em algumas, mas não desista, guerreiro!";
            }

            return new CorrecaoResultado(nota, feedback, dificuldades);
        }

        // ================================================================
        // Mecânica de chamada
        // ================================================================

        private async Task<JsonDocument> ChamarGeminiAsync(
            string prompt,
            string instrucaoSistema,
            object responseSchema,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException(
                    "Gemini:ApiKey não configurada. Use 'dotnet user-secrets set \"Gemini:ApiKey\" \"<token>\"'.");
            }

            var corpo = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = instrucaoSistema } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = _options.Temperatura,
                    // JSON Mode: obriga a resposta a seguir exatamente o schema.
                    responseMimeType = "application/json",
                    responseSchema
                }
            };

            var rota = $"models/{_options.Model}:generateContent";
            var maxTentativas = Math.Max(1, _options.MaxTentativas);

            for (var tentativa = 1; ; tentativa++)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, rota)
                    {
                        Content = JsonContent.Create(corpo)
                    };
                    request.Headers.Add("x-goog-api-key", _options.ApiKey);

                    using var response = await _httpClient.SendAsync(request, cancellationToken);
                    var conteudo = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        if (DevoTentarNovamente(response.StatusCode) && tentativa < maxTentativas)
                        {
                            await EsperarBackoffAsync(tentativa, cancellationToken);
                            continue;
                        }

                        throw new InvalidOperationException(
                            $"Gemini respondeu {(int)response.StatusCode}: {Resumir(conteudo)}");
                    }

                    return ExtrairJsonDaResposta(conteudo);
                }
                catch (HttpRequestException ex) when (tentativa < maxTentativas)
                {
                    _logger.LogWarning(ex, "Falha de rede ao chamar o Gemini (tentativa {Tentativa})", tentativa);
                    await EsperarBackoffAsync(tentativa, cancellationToken);
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && tentativa < maxTentativas)
                {
                    _logger.LogWarning("Timeout ao chamar o Gemini (tentativa {Tentativa})", tentativa);
                    await EsperarBackoffAsync(tentativa, cancellationToken);
                }
            }
        }

        /// <summary>
        /// A resposta do Gemini embrulha o JSON do modelo em
        /// candidates[0].content.parts[0].text.
        /// </summary>
        private static JsonDocument ExtrairJsonDaResposta(string conteudo)
        {
            using var envelope = JsonDocument.Parse(conteudo);
            var raiz = envelope.RootElement;

            if (raiz.TryGetProperty("promptFeedback", out var pf) &&
                pf.TryGetProperty("blockReason", out var motivo))
            {
                throw new InvalidOperationException(
                    $"O Gemini bloqueou o prompt: {motivo.GetString()}");
            }

            if (!raiz.TryGetProperty("candidates", out var candidatos) ||
                candidatos.ValueKind != JsonValueKind.Array ||
                candidatos.GetArrayLength() == 0)
            {
                throw new InvalidOperationException($"Resposta do Gemini sem candidatos: {Resumir(conteudo)}");
            }

            var candidato = candidatos[0];

            if (candidato.TryGetProperty("finishReason", out var fim) &&
                fim.GetString() is string razao &&
                !string.Equals(razao, "STOP", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"O Gemini interrompeu a geração: {razao}");
            }

            // Modelos com raciocínio podem devolver partes de "thought" antes da
            // resposta: pegamos a primeira parte de texto que não seja pensamento.
            string? texto = null;

            foreach (var parte in candidato.GetProperty("content").GetProperty("parts").EnumerateArray())
            {
                if (parte.TryGetProperty("thought", out var pensamento) &&
                    pensamento.ValueKind == JsonValueKind.True)
                {
                    continue;
                }

                if (parte.TryGetProperty("text", out var t) && !string.IsNullOrWhiteSpace(t.GetString()))
                {
                    texto = t.GetString();
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(texto))
                throw new InvalidOperationException("O Gemini devolveu um conteúdo vazio.");

            return JsonDocument.Parse(texto);
        }

        private static bool DevoTentarNovamente(HttpStatusCode status) =>
            status == HttpStatusCode.TooManyRequests ||
            status == HttpStatusCode.RequestTimeout ||
            (int)status >= 500;

        private static Task EsperarBackoffAsync(int tentativa, CancellationToken ct) =>
            Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, tentativa)), ct);

        private static string Resumir(string conteudo) =>
            conteudo.Length <= 500 ? conteudo : conteudo[..500] + "...";
    }
}
