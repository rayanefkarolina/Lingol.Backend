namespace Lingol.Pedagogico.Infrastructure.Services
{
    /// <summary>Configuração do provedor de IA (Google AI Studio / Gemini).</summary>
    public class GeminiOptions
    {
        public const string SecaoConfig = "Gemini";

        /// <summary>
        /// Token gerado no Google AI Studio. Deve vir de user-secrets ou variável de
        /// ambiente — nunca de appsettings.json versionado.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";

        public string Model { get; set; } = "gemini-3.6-flash";

        public int TimeoutSegundos { get; set; } = 90;

        /// <summary>Tentativas totais por chamada (1 = sem retry).</summary>
        public int MaxTentativas { get; set; } = 3;

        public double Temperatura { get; set; } = 0.7;
    }
}
