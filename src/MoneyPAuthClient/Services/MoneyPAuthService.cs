using System.Net.Http.Headers;
using System.Text.Json;
using MoneyPAuthClient.Models;

namespace MoneyPAuthClient.Services;

/// <summary>
/// Serviço de autenticação OAuth 2.0 para a API MoneyP
/// Gerencia a obtenção e renovação automática de tokens Bearer
/// </summary>
public class MoneyPAuthService : IDisposable
{
    private readonly AuthConfig _config;
    private readonly JwtGenerator _jwtGenerator;
    private readonly HttpClient _httpClient;
    private TokenResponse? _currentToken;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public MoneyPAuthService(AuthConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _jwtGenerator = new JwtGenerator(config);
        _httpClient = new HttpClient();
    }

    public MoneyPAuthService(AuthConfig config, HttpClient httpClient)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _jwtGenerator = new JwtGenerator(config);
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Obtém um token de acesso válido, renovando automaticamente se necessário
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Token de acesso Bearer válido</returns>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Verifica se o token atual ainda é válido
            if (_currentToken != null && !_currentToken.IsExpired())
            {
                return _currentToken.AccessToken;
            }

            // Obtém um novo token
            _currentToken = await RequestTokenAsync(cancellationToken);
            return _currentToken.AccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <summary>
    /// Força a renovação do token, mesmo que o atual ainda seja válido
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Novo token de acesso Bearer</returns>
    public async Task<string> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            _currentToken = await RequestTokenAsync(cancellationToken);
            return _currentToken.AccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <summary>
    /// Obtém a resposta completa do token atual
    /// </summary>
    /// <returns>Resposta completa do token ou null se não houver token</returns>
    public TokenResponse? GetCurrentTokenResponse()
    {
        return _currentToken;
    }

    /// <summary>
    /// Cria um HttpClient configurado com o token de autorização
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>HttpClient configurado com Bearer token</returns>
    public async Task<HttpClient> CreateAuthorizedClientAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>
    /// Realiza a requisição de token ao endpoint de autenticação
    /// </summary>
    private async Task<TokenResponse> RequestTokenAsync(CancellationToken cancellationToken)
    {
        // Gera o JWT assinado
        var jwt = _jwtGenerator.GenerateJwt();

        // Valida o tamanho dos scopes (máximo 300 caracteres)
        if (_config.Scopes.Length > 300)
        {
            throw new InvalidOperationException(
                $"O campo 'scope' aceita no máximo 300 caracteres. Atual: {_config.Scopes.Length}");
        }

        // Prepara os parâmetros da requisição
        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _config.ClientId,
            ["scope"] = _config.Scopes,
            ["client_assertion"] = jwt,
            ["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"
        };

        var content = new FormUrlEncodedContent(parameters);

        // Configura os headers
        using var request = new HttpRequestMessage(HttpMethod.Post, _config.TokenEndpoint)
        {
            Content = content
        };
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
       

        // Envia a requisição
        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = JsonSerializer.Deserialize<TokenErrorResponse>(responseContent);
            throw new HttpRequestException(
                $"Erro ao obter token: {errorResponse?.Error} - {errorResponse?.ErrorDescription}. " +
                $"Status: {response.StatusCode}");
        }

        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Resposta de token inválida ou vazia");
        }

        tokenResponse.ObtainedAt = DateTime.UtcNow;
        return tokenResponse;
    }

    public void Dispose()
    {
        _tokenLock.Dispose();
        _httpClient.Dispose();
        _jwtGenerator.Dispose();
        GC.SuppressFinalize(this);
    }
}
