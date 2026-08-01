using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Lingol.Pedagogico.Infrastructure.Services
{
    public class IaHttpClient : IIaAtividadeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IaHttpClient> _logger;

        public IaHttpClient(HttpClient httpClient, ILogger<IaHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<QuestaoGeradaDto>> GerarAtividadeAsync(
            string livro,
            string capituloOuAssunto,
            string materia,
            string? perfilAeeContexto,
            CancellationToken cancellationToken)
        {
            var payload = new
            {
                livro,
                capituloOuAssunto,
                materia,
                perfilAeeContexto
            };

            var response = await _httpClient.PostAsJsonAsync(
                "v1/gerar-atividade",
                payload,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var questoes = new List<QuestaoGeradaDto>();

            if (root.TryGetProperty("questoes", out var questoesElement) &&
                questoesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var q in questoesElement.EnumerateArray())
                {
                    var enunciado = q.GetProperty("enunciado").GetString() ?? string.Empty;
                    var tipo = q.GetProperty("tipo").GetString() ?? string.Empty;
                    var gabarito = q.GetProperty("gabarito").GetString() ?? string.Empty;

                    var alternativas = new List<string>();
                    if (q.TryGetProperty("alternativas", out var altElement) &&
                        altElement.ValueKind == JsonValueKind.Array)
                    {
                        alternativas.AddRange(altElement.EnumerateArray().Select(a => a.GetString() ?? string.Empty));
                    }

                    questoes.Add(new QuestaoGeradaDto(enunciado, tipo, alternativas, gabarito));
                }
            }

            return questoes;
        }

        public async Task<CorrecaoResultado> CorrigirRespostaAsync(
            Atividade atividade,
            RespostaAluno respostaAluno,
            CancellationToken cancellationToken)
        {
            var payload = new
            {
                atividadeId = atividade.Id,
                turmaId = atividade.TurmaId,
                alunoId = respostaAluno.AlunoId
                // aqui você poderia enviar respostas detalhadas
            };

            var response = await _httpClient.PostAsJsonAsync(
                "v1/corrigir-atividade",
                payload,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var nota = root.GetProperty("nota").GetDecimal();
            var feedback = root.GetProperty("feedbackGeral").GetString() ?? string.Empty;

            var dificuldades = new List<DificuldadeIdentificadaDto>();
            if (root.TryGetProperty("dificuldades", out var difElement) &&
                difElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var d in difElement.EnumerateArray())
                {
                    var questaoId = d.GetProperty("questaoId").GetGuid();
                    var tipoStr = d.GetProperty("tipo").GetString() ?? "Interpretacao";
                    var descricao = d.GetProperty("descricao").GetString() ?? string.Empty;

                    var tipo = Enum.Parse<TipoDificuldade>(tipoStr, ignoreCase: true);
                    dificuldades.Add(new DificuldadeIdentificadaDto(tipo, questaoId, descricao));
                }
            }

            return new CorrecaoResultado(nota, feedback, dificuldades);
        }
    }
}
