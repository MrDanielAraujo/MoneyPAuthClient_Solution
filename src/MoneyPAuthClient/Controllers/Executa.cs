using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoneyPAuthClient.Models;
using MoneyPAuthClient.Services;

namespace MoneyPAuthClient.Controllers;

[Route("api/[controller]")]
[ApiController]
public class Executa : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Rum()
    {

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
            //// ValidateConfig(authConfig);
           
            // Cria o serviço de autenticação
            using var authService = new MoneyPAuthService(authConfig);

            // Obtém o token de acesso
            //// var accessToken = await authService.GetAccessTokenAsync();

            //// var tokenResponse = authService.GetCurrentTokenResponse();
            //// if (tokenResponse != null)
            //// {
            ////    return Ok(tokenResponse);
            //// }

            using var authorizedClient = await authService.CreateAuthorizedClientAsync();
            
            
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.ext.dbs.moneyp.dev.br/api/Conta/Saldo?NumeroBanco=274&Conta.Agencia=0001&Conta.AgenciaDigito=8&Conta.Conta=244838&Conta.ContaDigito=9&Conta.ContaPgto=02448389&Conta.TipoConta=3&Conta.ModeloConta=1");
            var response = await authorizedClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return Ok( await response.Content.ReadAsStringAsync());
            
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"\n[ERRO] Arquivo não encontrado: {ex.Message}");
            Console.WriteLine("\nCertifique-se de que:");
            Console.WriteLine("  1. O arquivo 'private.pem' existe no diretório da aplicação");
            Console.WriteLine("  2. O arquivo 'appsettings.json' está configurado corretamente");
            
            return BadRequest();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"\n[ERRO] Falha na requisição HTTP: {ex.Message}");
            Console.WriteLine("\nVerifique:");
            Console.WriteLine("  1. Se o client_id está correto");
            Console.WriteLine("  2. Se a chave privada corresponde à chave pública cadastrada");
            Console.WriteLine("  3. Se os scopes solicitados são válidos");

            return BadRequest();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"  Inner Exception: {ex.InnerException.Message}");
            }

            return BadRequest();
        }

        //// return Ok(new { message = "Tudo deu certo" });
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

