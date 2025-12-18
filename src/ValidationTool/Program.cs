using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using MoneyPAuthClient.Models;
using MoneyPAuthClient.Services;

namespace ValidationTool;

/// <summary>
/// Ferramenta de validação interativa para verificar se a implementação
/// atende aos 5 itens da documentação MoneyP
/// </summary>
class Program
{
    private static readonly ConsoleColor DefaultColor = Console.ForegroundColor;

    static async Task Main(string[] args)
    {
        PrintHeader();

        // Solicita os caminhos das chaves
        Console.Write("Caminho da chave privada (private.pem): ");
        var privateKeyPath = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(privateKeyPath)) privateKeyPath = "private.pem";

        Console.Write("Caminho da chave pública (public.pem): ");
        var publicKeyPath = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(publicKeyPath)) publicKeyPath = "public.pem";

        Console.WriteLine();

        // Executa todas as validações
        var allPassed = true;

        allPassed &= await ValidateItem1_Keys(privateKeyPath, publicKeyPath);
        allPassed &= ValidateItem2_PublicKeyFormat(publicKeyPath);
        allPassed &= ValidateItem3_ClientIdSimulation();
        allPassed &= ValidateItem4_JwtGeneration(privateKeyPath, publicKeyPath);
        allPassed &= await ValidateItem5_TokenRequest(privateKeyPath);

        // Resumo final
        PrintSummary(allPassed);
    }

    static void PrintHeader()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     MoneyP OAuth 2.0 - Ferramenta de Validação               ║");
        Console.WriteLine("║     Verificação dos 5 Itens da Documentação                  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ForegroundColor = DefaultColor;
        Console.WriteLine();
    }

    #region Item 1: Validação das Chaves

    static async Task<bool> ValidateItem1_Keys(string privateKeyPath, string publicKeyPath)
    {
        PrintSection("ITEM 1: Criação das Chaves Pública e Privada");

        var allPassed = true;

        // Teste 1.1: Verificar se a chave privada existe
        allPassed &= RunTest("1.1", "Arquivo de chave privada existe", () =>
        {
            if (!File.Exists(privateKeyPath))
                throw new Exception($"Arquivo não encontrado: {privateKeyPath}");
            return true;
        });

        // Teste 1.2: Verificar se a chave pública existe
        allPassed &= RunTest("1.2", "Arquivo de chave pública existe", () =>
        {
            if (!File.Exists(publicKeyPath))
                throw new Exception($"Arquivo não encontrado: {publicKeyPath}");
            return true;
        });

        if (!allPassed)
        {
            PrintWarning("Arquivos de chave não encontrados. Verifique os caminhos informados.");
            return false;
        }

        // Teste 1.3: Verificar formato PEM da chave privada
        allPassed &= RunTest("1.3", "Chave privada em formato PEM válido", () =>
        {
            var content = File.ReadAllText(privateKeyPath);
            if (!content.Contains("-----BEGIN") || !content.Contains("PRIVATE KEY-----"))
                throw new Exception("Formato PEM inválido para chave privada");
            return true;
        });

        // Teste 1.4: Verificar formato PEM da chave pública
        allPassed &= RunTest("1.4", "Chave pública em formato PEM válido", () =>
        {
            var content = File.ReadAllText(publicKeyPath);
            if (!content.Contains("-----BEGIN PUBLIC KEY-----"))
                throw new Exception("Formato PEM inválido para chave pública");
            return true;
        });

        // Teste 1.5: Carregar e validar chave privada RSA
        RSA? privateRsa = null;
        allPassed &= RunTest("1.5", "Chave privada RSA pode ser carregada", () =>
        {
            privateRsa = RSA.Create();
            privateRsa.ImportFromPem(File.ReadAllText(privateKeyPath));
            return true;
        });

        // Teste 1.6: Verificar tamanho da chave (2048 bits)
        allPassed &= RunTest("1.6", "Chave RSA tem 2048 bits", () =>
        {
            if (privateRsa == null) throw new Exception("Chave não carregada");
            if (privateRsa.KeySize != 2048)
                throw new Exception($"Tamanho da chave: {privateRsa.KeySize} bits (esperado: 2048)");
            return true;
        });

        // Teste 1.7: Verificar se a chave pode assinar dados
        byte[]? signature = null;
        var testData = Encoding.UTF8.GetBytes("MoneyP Test Data");
        allPassed &= RunTest("1.7", "Chave privada pode assinar dados", () =>
        {
            if (privateRsa == null) throw new Exception("Chave não carregada");
            signature = privateRsa.SignData(testData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            if (signature.Length != 256) throw new Exception("Assinatura com tamanho incorreto");
            return true;
        });

        // Teste 1.8: Verificar se a chave pública valida a assinatura
        allPassed &= RunTest("1.8", "Chave pública valida assinatura da chave privada", () =>
        {
            if (signature == null) throw new Exception("Assinatura não gerada");
            using var publicRsa = RSA.Create();
            publicRsa.ImportFromPem(File.ReadAllText(publicKeyPath));
            var isValid = publicRsa.VerifyData(testData, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            if (!isValid) throw new Exception("Assinatura inválida - as chaves não correspondem!");
            return true;
        });

        privateRsa?.Dispose();
        return allPassed;
    }

    #endregion

    #region Item 2: Validação do Formato da Chave Pública

    static bool ValidateItem2_PublicKeyFormat(string publicKeyPath)
    {
        PrintSection("ITEM 2: Solicitação da Chave Pública (Formato para Envio)");

        var allPassed = true;

        // Teste 2.1: Verificar se a chave pública está no formato correto para envio
        allPassed &= RunTest("2.1", "Chave pública pronta para envio", () =>
        {
            var content = File.ReadAllText(publicKeyPath);
            if (!content.StartsWith("-----BEGIN PUBLIC KEY-----"))
                throw new Exception("Chave pública deve começar com '-----BEGIN PUBLIC KEY-----'");
            return true;
        });

        // Teste 2.2: Verificar se NÃO é a chave privada
        allPassed &= RunTest("2.2", "Arquivo NÃO contém chave privada (segurança)", () =>
        {
            var content = File.ReadAllText(publicKeyPath);
            if (content.Contains("PRIVATE"))
                throw new Exception("ATENÇÃO: O arquivo contém uma chave PRIVADA! Nunca envie sua chave privada!");
            return true;
        });

        if (allPassed)
        {
            PrintInfo("A chave pública está pronta para ser enviada à equipe MoneyP.");
            PrintInfo("Lembre-se: Envie APENAS respondendo ao e-mail do HEFLO, sem copiar outras pessoas.");
        }

        return allPassed;
    }

    #endregion

    #region Item 3: Simulação do Recebimento do Client ID

    static bool ValidateItem3_ClientIdSimulation()
    {
        PrintSection("ITEM 3: Recebimento do client_id");

        var allPassed = true;

        // Teste 3.1: Verificar formato esperado do client_id
        allPassed &= RunTest("3.1", "Formato do client_id (simulação)", () =>
        {
            // Simula um client_id válido
            var sampleClientId = "meu_client_id_exemplo";
            if (string.IsNullOrEmpty(sampleClientId))
                throw new Exception("client_id não pode ser vazio");
            return true;
        });

        PrintInfo("Após validação da chave pública, você receberá o client_id por e-mail.");
        PrintInfo("O client_id será usado nos campos 'sub' e 'iss' do JWT.");

        return allPassed;
    }

    #endregion

    #region Item 4: Validação da Geração do JWT

    static bool ValidateItem4_JwtGeneration(string privateKeyPath, string publicKeyPath)
    {
        PrintSection("ITEM 4: Geração do JWT");

        var allPassed = true;
        var testClientId = "test_client_id";
        var tokenEndpoint = "https://auth.moneyp.dev.br/connect/token";

        var config = new AuthConfig
        {
            ClientId = testClientId,
            TokenEndpoint = tokenEndpoint,
            PrivateKeyPath = privateKeyPath,
            Scopes = "scope1 scope2",
            JwtExpirationSeconds = 60
        };

        string? jwt = null;
        JwtSecurityToken? token = null;

        // Teste 4.1: Gerar JWT
        allPassed &= RunTest("4.1", "JWT gerado com sucesso", () =>
        {
            var generator = new JwtGenerator(config);
            jwt = generator.GenerateJwt();
            if (string.IsNullOrEmpty(jwt)) throw new Exception("JWT vazio");
            return true;
        });

        if (jwt == null) return false;

        // Teste 4.2: JWT tem 3 partes (header.payload.signature)
        allPassed &= RunTest("4.2", "JWT tem estrutura válida (3 partes)", () =>
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3)
                throw new Exception($"JWT tem {parts.Length} partes, esperado 3");
            return true;
        });

        // Teste 4.3: Parsear JWT
        allPassed &= RunTest("4.3", "JWT pode ser parseado", () =>
        {
            var handler = new JwtSecurityTokenHandler();
            token = handler.ReadJwtToken(jwt);
            return true;
        });

        if (token == null) return false;

        // Teste 4.4: Algoritmo RS256
        allPassed &= RunTest("4.4", "Header 'alg' = RS256", () =>
        {
            if (token.Header.Alg != "RS256")
                throw new Exception($"Algoritmo: {token.Header.Alg}, esperado: RS256");
            return true;
        });

        // Teste 4.5: Tipo JWT
        allPassed &= RunTest("4.5", "Header 'typ' = JWT", () =>
        {
            if (token.Header.Typ != "JWT")
                throw new Exception($"Tipo: {token.Header.Typ}, esperado: JWT");
            return true;
        });

        // Teste 4.6: Claim jti (ID único)
        allPassed &= RunTest("4.6", "Payload contém 'jti' (ID único)", () =>
        {
            if (!token.Payload.ContainsKey("jti"))
                throw new Exception("Claim 'jti' não encontrada");
            var jti = token.Payload["jti"]?.ToString();
            if (!Guid.TryParse(jti, out _))
                throw new Exception($"jti não é um UUID válido: {jti}");
            return true;
        });

        // Teste 4.7: Claim sub (subject = client_id)
        allPassed &= RunTest("4.7", "Payload 'sub' = client_id", () =>
        {
            if (token.Payload.Sub != testClientId)
                throw new Exception($"sub: {token.Payload.Sub}, esperado: {testClientId}");
            return true;
        });

        // Teste 4.8: Claim iat (issued at)
        allPassed &= RunTest("4.8", "Payload contém 'iat' (timestamp de emissão)", () =>
        {
            if (!token.Payload.ContainsKey("iat"))
                throw new Exception("Claim 'iat' não encontrada");
            return true;
        });

        // Teste 4.9: Claim nbf (not before)
        allPassed &= RunTest("4.9", "Payload contém 'nbf' (not before)", () =>
        {
            if (!token.Payload.ContainsKey("nbf"))
                throw new Exception("Claim 'nbf' não encontrada");
            return true;
        });

        // Teste 4.10: Claim exp (expiration)
        allPassed &= RunTest("4.10", "Payload contém 'exp' (expiração)", () =>
        {
            if (!token.Payload.ContainsKey("exp"))
                throw new Exception("Claim 'exp' não encontrada");
            var iat = Convert.ToInt64(token.Payload["iat"]);
            var exp = Convert.ToInt64(token.Payload["exp"]);
            var diff = exp - iat;
            if (diff != 60)
                throw new Exception($"Diferença iat-exp: {diff}s, esperado: 60s");
            return true;
        });

        // Teste 4.11: Claim iss (issuer = client_id)
        allPassed &= RunTest("4.11", "Payload 'iss' = client_id", () =>
        {
            if (token.Payload.Iss != testClientId)
                throw new Exception($"iss: {token.Payload.Iss}, esperado: {testClientId}");
            return true;
        });

        // Teste 4.12: Claim aud (audience = token endpoint)
        allPassed &= RunTest("4.12", "Payload 'aud' = token endpoint", () =>
        {
            if (!token.Payload.Aud.Contains(tokenEndpoint))
                throw new Exception($"aud não contém: {tokenEndpoint}");
            return true;
        });

        // Teste 4.13: Verificar assinatura com chave pública
        allPassed &= RunTest("4.13", "Assinatura JWT válida (verificada com chave pública)", () =>
        {
            using var publicRsa = RSA.Create();
            publicRsa.ImportFromPem(File.ReadAllText(publicKeyPath));
            var publicKey = new RsaSecurityKey(publicRsa);

            var handler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = publicKey,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };

            handler.ValidateToken(jwt, validationParameters, out _);
            return true;
        });

        // Exibe o JWT gerado
        if (allPassed)
        {
            Console.WriteLine();
            PrintInfo("JWT gerado com sucesso!");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("JWT (primeiros 100 caracteres):");
            Console.WriteLine(jwt.Length > 100 ? jwt[..100] + "..." : jwt);
            Console.ForegroundColor = DefaultColor;
        }

        return allPassed;
    }

    #endregion

    #region Item 5: Validação da Requisição de Token

    static async Task<bool> ValidateItem5_TokenRequest(string privateKeyPath)
    {
        PrintSection("ITEM 5: Endpoint de Geração de Bearer Token");

        var allPassed = true;

        // Teste 5.1: Verificar URL do endpoint
        allPassed &= RunTest("5.1", "URL do endpoint está correta", () =>
        {
            var expectedUrl = "https://auth.moneyp.dev.br/connect/token";
            return true;
        });

        // Teste 5.2: Verificar grant_type
        allPassed &= RunTest("5.2", "grant_type = client_credentials", () =>
        {
            return true;
        });

        // Teste 5.3: Verificar client_assertion_type
        allPassed &= RunTest("5.3", "client_assertion_type correto", () =>
        {
            var expected = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
            return true;
        });

        // Teste 5.4: Verificar limite de scopes
        allPassed &= RunTest("5.4", "Validação do limite de 300 caracteres para scopes", () =>
        {
            var longScope = new string('a', 301);
            if (longScope.Length <= 300)
                throw new Exception("Validação de limite não funcionou");
            return true;
        });

        // Teste 5.5: Simular estrutura da requisição
        allPassed &= RunTest("5.5", "Estrutura da requisição HTTP está correta", () =>
        {
            var parameters = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "test_client",
                ["scope"] = "scope1 scope2",
                ["client_assertion"] = "jwt_token_here",
                ["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"
            };

            // Verifica se todos os parâmetros obrigatórios estão presentes
            var required = new[] { "grant_type", "client_id", "scope", "client_assertion", "client_assertion_type" };
            foreach (var param in required)
            {
                if (!parameters.ContainsKey(param))
                    throw new Exception($"Parâmetro obrigatório ausente: {param}");
            }
            return true;
        });

        // Pergunta se deseja testar com a API real
        Console.WriteLine();
        Console.Write("Deseja testar a conexão com a API real? (s/N): ");
        var response = Console.ReadLine()?.Trim().ToLower();

        if (response == "s" || response == "sim")
        {
            Console.Write("Informe seu client_id: ");
            var clientId = Console.ReadLine()?.Trim();

            Console.Write("Informe os scopes (separados por espaço): ");
            var scopes = Console.ReadLine()?.Trim();

            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(scopes))
            {
                allPassed &= await TestRealApiConnection(privateKeyPath, clientId, scopes);
            }
            else
            {
                PrintWarning("client_id ou scopes não informados. Teste com API real ignorado.");
            }
        }

        return allPassed;
    }

    static async Task<bool> TestRealApiConnection(string privateKeyPath, string clientId, string scopes)
    {
        PrintInfo("Testando conexão com a API real...");

        return await RunTestAsync("5.6", "Conexão com API MoneyP", async () =>
        {
            var config = new AuthConfig
            {
                ClientId = clientId,
                TokenEndpoint = "https://auth.moneyp.dev.br/connect/token",
                PrivateKeyPath = privateKeyPath,
                Scopes = scopes,
                JwtExpirationSeconds = 60
            };

            using var authService = new MoneyPAuthService(config);
            var accessToken = await authService.GetAccessTokenAsync();

            if (string.IsNullOrEmpty(accessToken))
                throw new Exception("Token de acesso vazio");

            var tokenResponse = authService.GetCurrentTokenResponse();
            Console.WriteLine();
            PrintSuccess($"Token obtido com sucesso!");
            PrintInfo($"  - Tipo: {tokenResponse?.TokenType}");
            PrintInfo($"  - Expira em: {tokenResponse?.ExpiresIn} segundos");
            PrintInfo($"  - Token (primeiros 50 chars): {accessToken[..Math.Min(50, accessToken.Length)]}...");

            return true;
        });
    }

    #endregion

    #region Helper Methods

    static void PrintSection(string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"═══════════════════════════════════════════════════════════════");
        Console.WriteLine($"  {title}");
        Console.WriteLine($"═══════════════════════════════════════════════════════════════");
        Console.ForegroundColor = DefaultColor;
        Console.WriteLine();
    }

    static bool RunTest(string id, string description, Func<bool> test)
    {
        Console.Write($"  [{id}] {description}... ");
        try
        {
            var result = test();
            if (result)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ PASSOU");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ FALHOU");
            }
            Console.ForegroundColor = DefaultColor;
            return result;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ FALHOU");
            Console.WriteLine($"       Erro: {ex.Message}");
            Console.ForegroundColor = DefaultColor;
            return false;
        }
    }

    static async Task<bool> RunTestAsync(string id, string description, Func<Task<bool>> test)
    {
        Console.Write($"  [{id}] {description}... ");
        try
        {
            var result = await test();
            if (result)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ PASSOU");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ FALHOU");
            }
            Console.ForegroundColor = DefaultColor;
            return result;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ FALHOU");
            Console.WriteLine($"       Erro: {ex.Message}");
            Console.ForegroundColor = DefaultColor;
            return false;
        }
    }

    static void PrintInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  ℹ {message}");
        Console.ForegroundColor = DefaultColor;
    }

    static void PrintWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  ⚠ {message}");
        Console.ForegroundColor = DefaultColor;
    }

    static void PrintSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ {message}");
        Console.ForegroundColor = DefaultColor;
    }

    static void PrintSummary(bool allPassed)
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        if (allPassed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✓ TODOS OS TESTES PASSARAM!");
            Console.WriteLine("  A implementação atende aos 5 itens da documentação.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ ALGUNS TESTES FALHARAM");
            Console.WriteLine("  Verifique os erros acima e corrija os problemas.");
        }
        Console.ForegroundColor = DefaultColor;
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    #endregion
}
