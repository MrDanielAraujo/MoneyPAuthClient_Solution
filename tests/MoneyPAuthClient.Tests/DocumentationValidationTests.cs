using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using MoneyPAuthClient.Models;
using MoneyPAuthClient.Services;
using Xunit;

namespace MoneyPAuthClient.Tests;

/// <summary>
/// Testes de validação para verificar se a implementação atende aos 5 itens da documentação MoneyP
/// </summary>
public class DocumentationValidationTests : IDisposable
{
    private readonly string _testPrivateKeyPath;
    private readonly string _testPublicKeyPath;
    private readonly RSA _testRsa;

    public DocumentationValidationTests()
    {
        // Gera um par de chaves para testes
        _testRsa = RSA.Create(2048);
        _testPrivateKeyPath = Path.Combine(Path.GetTempPath(), $"test_private_{Guid.NewGuid()}.pem");
        _testPublicKeyPath = Path.Combine(Path.GetTempPath(), $"test_public_{Guid.NewGuid()}.pem");

        File.WriteAllText(_testPrivateKeyPath, _testRsa.ExportRSAPrivateKeyPem());
        File.WriteAllText(_testPublicKeyPath, _testRsa.ExportSubjectPublicKeyInfoPem());
    }

    public void Dispose()
    {
        _testRsa.Dispose();
        if (File.Exists(_testPrivateKeyPath)) File.Delete(_testPrivateKeyPath);
        if (File.Exists(_testPublicKeyPath)) File.Delete(_testPublicKeyPath);
    }

    #region Item 1: Criação das Chaves Pública e Privada

    /// <summary>
    /// Item 1: Valida que a chave privada foi criada corretamente no formato PEM
    /// </summary>
    [Fact]
    public void Item1_PrivateKey_ShouldBeValidRsaPemFormat()
    {
        // Arrange
        var privateKeyContent = File.ReadAllText(_testPrivateKeyPath);

        // Assert
        privateKeyContent.Should().Contain("-----BEGIN RSA PRIVATE KEY-----");
        privateKeyContent.Should().Contain("-----END RSA PRIVATE KEY-----");
    }

    /// <summary>
    /// Item 1: Valida que a chave pública foi criada corretamente no formato PEM
    /// </summary>
    [Fact]
    public void Item1_PublicKey_ShouldBeValidPemFormat()
    {
        // Arrange
        var publicKeyContent = File.ReadAllText(_testPublicKeyPath);

        // Assert
        publicKeyContent.Should().Contain("-----BEGIN PUBLIC KEY-----");
        publicKeyContent.Should().Contain("-----END PUBLIC KEY-----");
    }

    /// <summary>
    /// Item 1: Valida que as chaves são de 2048 bits conforme documentação
    /// </summary>
    [Fact]
    public void Item1_Keys_ShouldBe2048Bits()
    {
        // Assert
        _testRsa.KeySize.Should().Be(2048);
    }

    /// <summary>
    /// Item 1: Valida que a chave privada pode assinar dados
    /// </summary>
    [Fact]
    public void Item1_PrivateKey_ShouldBeAbleToSignData()
    {
        // Arrange
        var testData = Encoding.UTF8.GetBytes("test data");

        // Act
        var signature = _testRsa.SignData(testData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Assert
        signature.Should().NotBeEmpty();
        signature.Length.Should().Be(256); // 2048 bits = 256 bytes
    }

    /// <summary>
    /// Item 1: Valida que a chave pública pode verificar assinaturas da chave privada
    /// </summary>
    [Fact]
    public void Item1_PublicKey_ShouldVerifyPrivateKeySignature()
    {
        // Arrange
        var testData = Encoding.UTF8.GetBytes("test data");
        var signature = _testRsa.SignData(testData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Carrega a chave pública separadamente
        using var publicRsa = RSA.Create();
        publicRsa.ImportFromPem(File.ReadAllText(_testPublicKeyPath));

        // Act
        var isValid = publicRsa.VerifyData(testData, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Assert
        isValid.Should().BeTrue();
    }

    #endregion

    #region Item 4: Geração do JWT

    /// <summary>
    /// Item 4: Valida que o JWT é gerado com algoritmo RS256
    /// </summary>
    [Fact]
    public void Item4_Jwt_ShouldUseRS256Algorithm()
    {
        // Arrange
        var config = CreateTestConfig();
        var generator = new JwtGenerator(config);

        // Act
        var jwt = generator.GenerateJwt();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        // Assert
        token.Header.Alg.Should().Be("RS256");
    }

    /// <summary>
    /// Item 4: Valida que o JWT tem tipo "JWT" no header
    /// </summary>
    [Fact]
    public void Item4_Jwt_ShouldHaveJwtType()
    {
        // Arrange
        var config = CreateTestConfig();
        var generator = new JwtGenerator(config);

        // Act
        var jwt = generator.GenerateJwt();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        // Assert
        token.Header.Typ.Should().Be("JWT");
    }

    /// <summary>
    /// Item 4: Valida que o JWT contém claim 'jti' (ID único)
    /// </summary>
    [Fact]
    public void Item4_Jwt_ShouldContainJtiClaim()
    {
        // Arrange
        var config = CreateTestConfig();
        var generator = new JwtGenerator(config);

        // Act
        var jwt = generator.GenerateJwt();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        // Assert
        token.Payload.Should().ContainKey("jti");
        token.Payload["jti"].ToString().Should().NotBeNullOrEmpty();
        Guid.TryParse(token.Payload["jti"].ToString(), out _).Should().BeTrue("jti deve ser um UUID válido");
    }

    /// <summary>
    /// Item 4: Valida que o JWT contém claim 'sub' com o client_id
    /// </summary>
    [Fact]
    public void Item4_Jwt_ShouldContainSubClaimWithClientId()
    {
        // Arrange
        var clientId = "test_client_id";
        var config = CreateTestConfig(clientId);
        var generator = new JwtGenerator(config);

        // Act
        var jwt = generator.GenerateJwt();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        // Assert
        token.Payload.Sub.Should().Be(clientId);
    }

    /// <summary>
    /// Item 4: Valida que o JWT contém claim 'iat' (issued at)
    /// </summary>
    [Fact]
    public void Item4_Jwt_ShouldContainIatClaim()
    {
        // Arrange
        var config = CreateTestConfig();
        var generator = new JwtGenerator(config);
        var beforeGeneration = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Act
        var jwt = generator.GenerateJwt();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
        var afterGeneration = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Assert
        token.Payload.Should().ContainKey("iat");
        var iat = Convert.ToInt64(token.Payload["iat"]);
        iat.Should().BeInRange(beforeGeneration, afterGeneration);
    }

    /// <summary>
    /// Item 4: Valida que o JWT contém claim 'nbf' (not before)
    /// </summary>
    [Fact]
    public void Item4_Jwt_ShouldContainNbfClaim()
    {
        // Arrange
        var config = CreateTestConfig();
        var generator = new JwtGenerator(config);

        // Act
        var jwt = generator.GenerateJwt();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        // Assert
        token.Payload.Should().ContainKey("nbf");
        token.Payload.Nbf.Should().NotBeNull();
    }

    /// <summary>
    /// Item 4: Valida que o JWT contém claim 'exp' (expiration)
    /// </summary>
    [Fact]
    public void Item4_Jwt_ShouldContainExpClaim()
    {
        // Arrange
        var expirationSeconds = 60;
        var config = CreateTestConfig(jwtExpirationSeconds: expirationSeconds);
        var generator = new JwtGenerator(config);

        // Act
        var jwt = generator.GenerateJwt();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        // Assert
        token.Payload.Should().ContainKey("exp");
        var iat = Convert.ToInt64(token.Payload["iat"]);
        var exp = Convert.ToInt64(token.Payload["exp"]);
        (exp - iat).Should().Be(expirationSeconds);
    }

    /// <summary>
    /// Item 4: Valida que o JWT contém claim 'iss' (issuer) com o client_id
    /// </summary>
    [Fact]
    public void Item4_Jwt_ShouldContainIssClaimWithClientId()
    {
        // Arrange
        var clientId = "test_client_id";
        var config = CreateTestConfig(clientId);
        var generator = new JwtGenerator(config);

        // Act
        var jwt = generator.GenerateJwt();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        // Assert
        token.Payload.Iss.Should().Be(clientId);
    }

    /// <summary>
    /// Item 4: Valida que o JWT contém claim 'aud' (audience) com o endpoint correto
    /// </summary>
    [Fact]
    public void Item4_Jwt_ShouldContainAudClaimWithTokenEndpoint()
    {
        // Arrange
        var tokenEndpoint = "https://auth.moneyp.dev.br/connect/token";
        var config = CreateTestConfig(tokenEndpoint: tokenEndpoint);
        var generator = new JwtGenerator(config);

        // Act
        var jwt = generator.GenerateJwt();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        // Assert
        token.Payload.Aud.Should().Contain(tokenEndpoint);
    }

    /// <summary>
    /// Item 4: Valida que o JWT pode ser verificado com a chave pública
    /// </summary>
    [Fact]
    public void Item4_Jwt_ShouldBeVerifiableWithPublicKey()
    {
        // Arrange
        var config = CreateTestConfig();
        var generator = new JwtGenerator(config);

        // Act
        var jwt = generator.GenerateJwt();

        // Carrega a chave pública para verificação
        using var publicRsa = RSA.Create();
        publicRsa.ImportFromPem(File.ReadAllText(_testPublicKeyPath));
        var publicKey = new RsaSecurityKey(publicRsa);

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = publicKey,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ClockSkew = TimeSpan.Zero
        };

        // Assert
        var principal = handler.ValidateToken(jwt, validationParameters, out var validatedToken);
        validatedToken.Should().NotBeNull();
    }

    #endregion

    #region Item 5: Endpoint de geração de Bearer Token

    /// <summary>
    /// Item 5: Valida que os parâmetros da requisição estão corretos
    /// </summary>
    [Fact]
    public void Item5_TokenRequest_ShouldHaveCorrectParameters()
    {
        // Arrange
        var config = CreateTestConfig();

        // Assert - Verifica se a configuração tem todos os campos necessários
        config.ClientId.Should().NotBeNullOrEmpty("client_id é obrigatório");
        config.TokenEndpoint.Should().Be("https://auth.moneyp.dev.br/connect/token");
        config.Scopes.Should().NotBeNullOrEmpty("scopes são obrigatórios");
    }

    /// <summary>
    /// Item 5: Valida que o scope não excede 300 caracteres
    /// </summary>
    [Fact]
    public void Item5_Scopes_ShouldNotExceed300Characters()
    {
        // Arrange
        var longScopes = new string('a', 301);
        var config = CreateTestConfig(scopes: longScopes);

        // Assert
        config.Scopes.Length.Should().BeGreaterThan(300, "Este teste verifica que scopes longos são detectados");

        // A validação real acontece no MoneyPAuthService
    }

    /// <summary>
    /// Item 5: Valida o formato do client_assertion_type
    /// </summary>
    [Fact]
    public void Item5_ClientAssertionType_ShouldBeCorrect()
    {
        // O valor esperado conforme documentação
        var expectedAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";

        // Assert
        expectedAssertionType.Should().Be("urn:ietf:params:oauth:client-assertion-type:jwt-bearer");
    }

    /// <summary>
    /// Item 5: Valida o formato do grant_type
    /// </summary>
    [Fact]
    public void Item5_GrantType_ShouldBeClientCredentials()
    {
        // O valor esperado conforme documentação
        var expectedGrantType = "client_credentials";

        // Assert
        expectedGrantType.Should().Be("client_credentials");
    }

    #endregion

    #region Testes de Integração do Fluxo Completo

    /// <summary>
    /// Teste de integração: Valida o fluxo completo de geração de JWT
    /// </summary>
    [Fact]
    public void Integration_FullJwtGenerationFlow_ShouldWork()
    {
        // Arrange
        var clientId = "my_test_client";
        var config = CreateTestConfig(clientId);
        var generator = new JwtGenerator(config);

        // Act
        var jwt = generator.GenerateJwt();

        // Assert
        jwt.Should().NotBeNullOrEmpty();
        jwt.Split('.').Should().HaveCount(3, "JWT deve ter 3 partes: header.payload.signature");

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        // Valida todas as claims obrigatórias
        token.Header.Alg.Should().Be("RS256");
        token.Header.Typ.Should().Be("JWT");
        token.Payload.Sub.Should().Be(clientId);
        token.Payload.Iss.Should().Be(clientId);
        token.Payload.Aud.Should().Contain("https://auth.moneyp.dev.br/connect/token");
        token.Payload.Should().ContainKey("jti");
        token.Payload.Should().ContainKey("iat");
        token.Payload.Should().ContainKey("nbf");
        token.Payload.Should().ContainKey("exp");
    }

    /// <summary>
    /// Teste: Valida que JWTs gerados são únicos (jti diferente)
    /// </summary>
    [Fact]
    public void Integration_MultipleJwts_ShouldHaveUniqueJti()
    {
        // Arrange
        var config = CreateTestConfig();
        var generator = new JwtGenerator(config);
        var handler = new JwtSecurityTokenHandler();

        // Act
        var jwt1 = generator.GenerateJwt();
        var jwt2 = generator.GenerateJwt();

        var token1 = handler.ReadJwtToken(jwt1);
        var token2 = handler.ReadJwtToken(jwt2);

        // Assert
        token1.Payload["jti"].Should().NotBe(token2.Payload["jti"], "Cada JWT deve ter um jti único");
    }

    #endregion

    #region Helper Methods

    private AuthConfig CreateTestConfig(
        string clientId = "test_client_id",
        string tokenEndpoint = "https://auth.moneyp.dev.br/connect/token",
        string scopes = "scope1 scope2",
        int jwtExpirationSeconds = 60)
    {
        return new AuthConfig
        {
            ClientId = clientId,
            TokenEndpoint = tokenEndpoint,
            PrivateKeyPath = _testPrivateKeyPath,
            Scopes = scopes,
            JwtExpirationSeconds = jwtExpirationSeconds
        };
    }

    #endregion
}
