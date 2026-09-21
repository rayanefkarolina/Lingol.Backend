using System.Net.Http.Headers;

namespace Lingol.Pedagogico.API.Http;

/// <summary>
/// Repassa o Authorization da requisição atual para as chamadas ao microsserviço
/// de Cadastro, que também valida o mesmo JWT.
/// </summary>
public class AuthHeaderPropagationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthHeaderPropagationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(authorization) &&
            AuthenticationHeaderValue.TryParse(authorization, out var header))
        {
            request.Headers.Authorization = header;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
