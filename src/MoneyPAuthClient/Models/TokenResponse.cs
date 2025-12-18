using System.Text.Json.Serialization;

namespace MoneyPAuthClient.Models;

/// <summary>
/// Resposta do endpoint de autenticação OAuth 2.0
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// Token de acesso Bearer
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Tipo do token (Bearer)
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    /// <summary>
    /// Tempo de expiração em segundos
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Escopos concedidos
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>
    /// Data/hora de obtenção do token
    /// </summary>
    [JsonIgnore]
    public DateTime ObtainedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Verifica se o token está expirado ou próximo de expirar
    /// </summary>
    /// <param name="bufferSeconds">Margem de segurança em segundos antes da expiração</param>
    /// <returns>True se o token está expirado ou próximo de expirar</returns>
    public bool IsExpired(int bufferSeconds = 60)
    {
        return DateTime.UtcNow >= ObtainedAt.AddSeconds(ExpiresIn - bufferSeconds);
    }
}

/// <summary>
/// Resposta de erro do endpoint de autenticação
/// </summary>
public class TokenErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}
