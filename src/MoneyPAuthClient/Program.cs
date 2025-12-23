using Microsoft.Extensions.Configuration;
using MoneyPAuthClient.Models;
using MoneyPAuthClient.Services;

namespace MoneyPAuthClient;

/// <summary>
/// Aplicação de exemplo demonstrando o uso do serviço de autenticação MoneyP
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("   MoneyP OAuth 2.0 Authentication Client  ");
        Console.WriteLine("===========================================\n");

        try
        {
            // Carrega as configurações do arquivo appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var authConfig = new AuthConfig();
            configuration.GetSection("AuthConfig").Bind(authConfig);

            // Valida as configurações
            ValidateConfig(authConfig);

            Console.WriteLine("Configurações carregadas:");
            Console.WriteLine($"  - Client ID: {MaskString(authConfig.ClientId)}");
            Console.WriteLine($"  - Token Endpoint: {authConfig.TokenEndpoint}");
            Console.WriteLine($"  - Private Key Path: {authConfig.PrivateKeyPath}");
            Console.WriteLine($"  - Scopes: {authConfig.Scopes}");
            Console.WriteLine($"  - JWT Expiration: {authConfig.JwtExpirationSeconds} segundos\n");

            // Cria o serviço de autenticação
            using var authService = new MoneyPAuthService(authConfig);

            Console.WriteLine("Obtendo token de acesso...\n");

            // Obtém o token de acesso
            var accessToken = await authService.GetAccessTokenAsync();

            Console.WriteLine("Token obtido com sucesso!");
            Console.WriteLine($"  - Access Token: {MaskString(accessToken, 50)}");

            var tokenResponse = authService.GetCurrentTokenResponse();
            if (tokenResponse != null)
            {
                Console.WriteLine($"  - Token Type: {tokenResponse.TokenType}");
                Console.WriteLine($"  - Expires In: {tokenResponse.ExpiresIn} segundos");
                Console.WriteLine($"  - Scope: {tokenResponse.Scope}");
                Console.WriteLine($"  - Obtained At: {tokenResponse.ObtainedAt:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"  - Is Expired: {tokenResponse.IsExpired()}");
            }

            Console.WriteLine("\n===========================================");
            Console.WriteLine("Exemplo de uso do token em requisições:");
            Console.WriteLine("===========================================\n");

            // Demonstra como usar o token em requisições
            Console.WriteLine("// Opção 1: Obter o token e usar manualmente");
            Console.WriteLine($"var token = await authService.GetAccessTokenAsync();");
            Console.WriteLine($"httpClient.DefaultRequestHeaders.Authorization = ");
            Console.WriteLine($"    new AuthenticationHeaderValue(\"Bearer\", token);\n");

            Console.WriteLine("// Opção 2: Usar o HttpClient já configurado");
            Console.WriteLine($"using var client = await authService.CreateAuthorizedClientAsync();");
            Console.WriteLine($"var response = await client.GetAsync(\"https://api.moneyp.dev.br/endpoint\");\n");

            // Demonstração de chamada à API (comentada para não executar)
            Console.WriteLine("===========================================");
            Console.WriteLine("Demonstração de chamada à API:");
            Console.WriteLine("===========================================\n");

            using var authorizedClient = await authService.CreateAuthorizedClientAsync();
            Console.WriteLine($"HttpClient configurado com Authorization: Bearer {accessToken}");
            Console.WriteLine("\nPronto para fazer requisições à API MoneyP!");
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"\n[ERRO] Arquivo não encontrado: {ex.Message}");
            Console.WriteLine("\nCertifique-se de que:");
            Console.WriteLine("  1. O arquivo 'private.pem' existe no diretório da aplicação");
            Console.WriteLine("  2. O arquivo 'appsettings.json' está configurado corretamente");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"\n[ERRO] Falha na requisição HTTP: {ex.Message}");
            Console.WriteLine("\nVerifique:");
            Console.WriteLine("  1. Se o client_id está correto");
            Console.WriteLine("  2. Se a chave privada corresponde à chave pública cadastrada");
            Console.WriteLine("  3. Se os scopes solicitados são válidos");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"  Inner Exception: {ex.InnerException.Message}");
            }
        }
    }

    /// <summary>
    /// Valida as configurações obrigatórias
    /// </summary>
    private static void ValidateConfig(AuthConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.ClientId) || config.ClientId == "SEU_CLIENT_ID_AQUI")
            errors.Add("ClientId não configurado");

        if (string.IsNullOrWhiteSpace(config.TokenEndpoint))
            errors.Add("TokenEndpoint não configurado");

        if (string.IsNullOrWhiteSpace(config.PrivateKeyPath))
            errors.Add("PrivateKeyPath não configurado");

        if (string.IsNullOrWhiteSpace(config.Scopes))
            errors.Add("Scopes não configurado");

        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"Configurações inválidas:\n  - {string.Join("\n  - ", errors)}");
        }
    }

    /// <summary>
    /// Mascara uma string para exibição segura
    /// </summary>
    private static string MaskString(string value, int visibleChars = 8)
    {
        if (string.IsNullOrEmpty(value))
            return "[vazio]";

        if (value.Length <= visibleChars)
            return new string('*', value.Length);

        return value[..visibleChars] + "..." + new string('*', Math.Min(10, value.Length - visibleChars));
    }
}
