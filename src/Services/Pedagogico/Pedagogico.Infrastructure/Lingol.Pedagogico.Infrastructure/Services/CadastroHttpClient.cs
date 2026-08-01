using Lingol.Pedagogico.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using static Lingol.Pedagogico.Application.Dtos.CadastroDtos;

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
            return await _httpClient.GetFromJsonAsync<TurmaCadastroDto>($"api/turmas/{turmaId}", ct);
        }

        public async Task<AlunoCadastroDto?> ObterAlunoAsync(Guid alunoId, CancellationToken ct)
        {
            return await _httpClient.GetFromJsonAsync<AlunoCadastroDto>($"api/alunos/{alunoId}", ct);
        }

        public async Task<List<AlunoCadastroDto>> ObterAlunosDaTurmaAsync(Guid turmaId, CancellationToken ct)
        {
            var result = await _httpClient.GetFromJsonAsync<List<AlunoCadastroDto>>(
                $"api/turmas/{turmaId}/alunos", ct);

            return result ?? new List<AlunoCadastroDto>();
        }
    }
}
