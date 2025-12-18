using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using MoneyPAuthClient.Models;

namespace MoneyPAuthClient.Services;

/// <summary>
/// Serviço responsável pela geração de tokens JWT assinados com RS256
/// </summary>
public class JwtGenerator
{
    private readonly AuthConfig _config;
    private RSA? _privateKey;

    public JwtGenerator(AuthConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Carrega a chave privada RSA do arquivo PEM
    /// </summary>
    /// <returns>Chave RSA carregada</returns>
    /// <exception cref="FileNotFoundException">Arquivo de chave não encontrado</exception>
    /// <exception cref="InvalidOperationException">Erro ao carregar a chave</exception>
    private RSA LoadPrivateKey()
    {
        if (_privateKey != null)
            return _privateKey;

        if (!File.Exists(_config.PrivateKeyPath))
        {
            throw new FileNotFoundException(
                $"Arquivo de chave privada não encontrado: {_config.PrivateKeyPath}");
        }

        try
        {
            var privateKeyPem = File.ReadAllText(_config.PrivateKeyPath);
            _privateKey = RSA.Create();
            _privateKey.ImportFromPem(privateKeyPem);
            return _privateKey;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Erro ao carregar a chave privada: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gera um token JWT assinado com RS256 conforme especificação da API MoneyP
    /// </summary>
    /// <returns>Token JWT assinado</returns>
    public string GenerateJwt()
    {
        var rsa = LoadPrivateKey();
        var securityKey = new RsaSecurityKey(rsa);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;
        var unixNow = new DateTimeOffset(now).ToUnixTimeSeconds();
        var unixExp = new DateTimeOffset(now.AddSeconds(_config.JwtExpirationSeconds)).ToUnixTimeSeconds();

        // Gera um identificador único para o token (jti)
        var jti = Guid.NewGuid().ToString();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(JwtRegisteredClaimNames.Sub, _config.ClientId),
            new Claim(JwtRegisteredClaimNames.Iat, unixNow.ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Nbf, unixNow.ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, unixExp.ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Iss, _config.ClientId),
            new Claim(JwtRegisteredClaimNames.Aud, _config.TokenEndpoint)
        };

        var header = new JwtHeader(credentials);
        var payload = new JwtPayload(claims);
        var token = new JwtSecurityToken(header, payload);

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Libera os recursos da chave RSA
    /// </summary>
    public void Dispose()
    {
        _privateKey?.Dispose();
    }
}
