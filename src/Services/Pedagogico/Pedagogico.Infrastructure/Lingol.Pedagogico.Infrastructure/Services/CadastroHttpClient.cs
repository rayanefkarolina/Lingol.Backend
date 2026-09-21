using System.Net;
using System.Net.Http.Json;
using Lingol.Contracts.Cadastro.Dtos;
using Lingol.Pedagogico.Application.Abstractions;

namespace Lingol.Pedagogico.Infrastructure.Services
{
    public class CadastroHttpClient : ICadastroClient
    {
        private readonly HttpClient _httpClient;

        public CadastroHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<TurmaCadastroDto?> ObterTurmaAsync(Guid turmaId, CancellationToken ct)
        {
            var response = await _httpClient.GetAsync($"api/turmas/{turmaId}", ct);

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TurmaCadastroDto>(ct);
        }

        public async Task<AlunoCadastroDto?> ObterAlunoAsync(Guid alunoId, CancellationToken ct)
        {
            var response = await _httpClient.GetAsync($"api/alunos/{alunoId}", ct);

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AlunoCadastroDto>(ct);
        }

        public async Task<List<AlunoCadastroDto>> ObterAlunosDaTurmaAsync(Guid turmaId, CancellationToken ct)
        {
            var response = await _httpClient.GetAsync($"api/turmas/{turmaId}/alunos", ct);

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
                return new List<AlunoCadastroDto>();

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<List<AlunoCadastroDto>>(ct);
            return result ?? new List<AlunoCadastroDto>();
        }
    }
}
