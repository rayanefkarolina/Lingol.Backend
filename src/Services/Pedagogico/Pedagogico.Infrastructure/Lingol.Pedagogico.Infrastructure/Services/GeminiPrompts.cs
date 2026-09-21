using System.Text;
using Lingol.Pedagogico.Domain.Entities;

namespace Lingol.Pedagogico.Infrastructure.Services
{
    /// <summary>
    /// Monta os prompts e os schemas de resposta (JSON Mode) enviados ao Gemini.
    /// Separado do cliente HTTP para que o texto pedagógico possa evoluir sem
    /// mexer na mecânica de chamada.
    /// </summary>
    internal static class GeminiPrompts
    {
        public const string InstrucaoSistemaGeracao =
            "Você é um professor de Língua Portuguesa do Ensino Fundamental brasileiro, especialista " +
            "em recuperação de defasagem escolar e em educação inclusiva. Você elabora questões " +
            "alinhadas à BNCC, com linguagem adequada ao ano escolar informado. Responda SEMPRE " +
            "apenas com o JSON pedido, sem texto fora dele.";

        public const string InstrucaoSistemaDiagnostico =
            "Você é um professor de Língua Portuguesa que analisa erros de alunos do Ensino " +
            "Fundamental e classifica a dificuldade pedagógica por trás de cada erro. Seu tom com o " +
            "aluno é encorajador, nunca punitivo. Responda SEMPRE apenas com o JSON pedido.";

        public static string MontarPromptGeracao(
            string livro,
            string capituloOuAssunto,
            string materia,
            int ano,
            int numQuestoes,
            string? perfilAeeContexto)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Gere exatamente {numQuestoes} questões de múltipla escolha de {materia}.");
            sb.AppendLine();
            sb.AppendLine("Contexto da turma:");
            sb.AppendLine($"- Livro de referência: {livro}");
            sb.AppendLine($"- Capítulo/assunto: {capituloOuAssunto}");
            sb.AppendLine($"- Ano escolar: {ano}º ano do Ensino Fundamental");

            if (!string.IsNullOrWhiteSpace(perfilAeeContexto))
            {
                sb.AppendLine($"- Atendimento Educacional Especializado na turma: {perfilAeeContexto}");
                sb.AppendLine("  Adapte para esses perfis: enunciados curtos e diretos, uma instrução por frase,");
                sb.AppendLine("  vocabulário concreto e sem duplo sentido, evitando pegadinhas.");
            }

            sb.AppendLine();
            sb.AppendLine("Regras obrigatórias:");
            sb.AppendLine("1. Esta é a primeira atividade da turma, usada para DIAGNOSTICAR dificuldades.");
            sb.AppendLine("   Cubra habilidades variadas dentro do assunto, com dificuldade crescente.");
            sb.AppendLine("2. Cada questão tem exatamente 4 alternativas, rotuladas \"A) \", \"B) \", \"C) \" e \"D) \".");
            sb.AppendLine("3. O campo gabarito contém SOMENTE a letra da alternativa correta (A, B, C ou D).");
            sb.AppendLine("4. A explicação descreve a regra gramatical envolvida em 1 a 2 frases, em tom");
            sb.AppendLine("   encorajador, porque ela é mostrada ao aluno quando ele erra.");
            sb.AppendLine("5. O campo habilidade traz o conceito avaliado por extenso, em no máximo 8");
            sb.AppendLine("   palavras (ex.: \"substantivo próprio\", \"concordância verbal\",");
            sb.AppendLine("   \"interpretação de texto\"). NUNCA use código da BNCC (ex.: EF06LP01)");
            sb.AppendLine("   nem sigla: esse texto é exibido ao professor no mapa de lacunas.");
            sb.AppendLine("6. Escreva tudo em português do Brasil.");

            return sb.ToString();
        }

        /// <summary>Schema de resposta obrigatório para a geração de atividade.</summary>
        public static object SchemaGeracao() => new
        {
            type = "OBJECT",
            properties = new
            {
                questoes = new
                {
                    type = "ARRAY",
                    items = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            enunciado = new { type = "STRING" },
                            alternativas = new
                            {
                                type = "ARRAY",
                                items = new { type = "STRING" },
                                minItems = 4,
                                maxItems = 4
                            },
                            gabarito = new { type = "STRING", @enum = new[] { "A", "B", "C", "D" } },
                            explicacao = new { type = "STRING" },
                            habilidade = new { type = "STRING" }
                        },
                        required = new[] { "enunciado", "alternativas", "gabarito", "explicacao", "habilidade" }
                    }
                }
            },
            required = new[] { "questoes" }
        };

        public static string MontarPromptDiagnostico(
            Atividade atividade,
            RespostaAluno entrega,
            IReadOnlyList<(Questao Questao, RespostaQuestao Resposta)> itens)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Analise o desempenho deste aluno e classifique as dificuldades pedagógicas.");
            sb.AppendLine();
            sb.AppendLine($"Assunto: {atividade.CapituloOuAssunto} ({atividade.Materia})");
            sb.AppendLine($"Livro: {atividade.Livro}");
            sb.AppendLine($"Resultado: {entrega.Acertos} acertos e {entrega.Erros} erros em {entrega.TotalQuestoes} questões.");
            sb.AppendLine();
            sb.AppendLine("Respostas:");

            foreach (var (questao, resposta) in itens)
            {
                sb.AppendLine($"- questaoId: {questao.Id}");
                sb.AppendLine($"  enunciado: {questao.Enunciado}");
                sb.AppendLine($"  gabarito: {questao.GabaritoOuCriterio}");
                sb.AppendLine($"  resposta do aluno: {(string.IsNullOrWhiteSpace(resposta.RespostaEscolhida) ? "(em branco)" : resposta.RespostaEscolhida)}");
                sb.AppendLine($"  acertou: {(resposta.EstaCorreta ? "sim" : "não")}");
            }

            sb.AppendLine();
            sb.AppendLine("Regras obrigatórias:");
            sb.AppendLine("1. Gere uma dificuldade APENAS para as questões que o aluno errou.");
            sb.AppendLine("2. O campo questaoId deve repetir exatamente um dos ids listados acima.");
            sb.AppendLine("3. O campo tipo deve ser um destes valores, sem acento e sem variação:");
            sb.AppendLine("   Interpretacao, Ortografia, Gramatica, Vocabulario, Coesao, Pontuacao, OrganizacaoTexto.");
            sb.AppendLine("4. A descrição explica, em 1 frase, o que o aluno não dominou.");
            sb.AppendLine("5. O feedbackGeral fala com o aluno em 2 frases, em tom de incentivo,");
            sb.AppendLine("   reconhecendo o que ele acertou antes de apontar o que treinar.");
            sb.AppendLine("6. Escreva tudo em português do Brasil.");

            return sb.ToString();
        }

        /// <summary>Schema de resposta obrigatório para o diagnóstico.</summary>
        public static object SchemaDiagnostico() => new
        {
            type = "OBJECT",
            properties = new
            {
                feedbackGeral = new { type = "STRING" },
                dificuldades = new
                {
                    type = "ARRAY",
                    items = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            questaoId = new { type = "STRING" },
                            tipo = new
                            {
                                type = "STRING",
                                @enum = new[]
                                {
                                    "Interpretacao", "Ortografia", "Gramatica", "Vocabulario",
                                    "Coesao", "Pontuacao", "OrganizacaoTexto"
                                }
                            },
                            descricao = new { type = "STRING" }
                        },
                        required = new[] { "questaoId", "tipo", "descricao" }
                    }
                }
            },
            required = new[] { "feedbackGeral", "dificuldades" }
        };
    }
}
