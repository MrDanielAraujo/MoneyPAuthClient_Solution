namespace MoneyPAuthClient.Models;

/// <summary>
/// Configurações de autenticação OAuth 2.0 para a API MoneyP
/// </summary>
public class AuthConfig
{
    /// <summary>
    /// Identificador do cliente fornecido pela equipe MoneyP
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// URL do endpoint de autenticação
    /// </summary>
    public string TokenEndpoint { get; set; } = "https://auth.moneyp.dev.br/connect/token";

    /// <summary>
    /// Caminho para o arquivo da chave privada PEM
    /// </summary>
    public string PrivateKeyPath { get; set; } = "private.pem";

    /// <summary>
    /// Escopos de acesso separados por espaço (máximo 300 caracteres)
    /// </summary>
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// Tempo de expiração do JWT em segundos (padrão: 60 segundos)
    /// </summary>
    public int JwtExpirationSeconds { get; set; } = 60;
}
