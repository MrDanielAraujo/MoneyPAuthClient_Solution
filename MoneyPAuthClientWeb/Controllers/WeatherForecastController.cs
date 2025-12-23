using Microsoft.AspNetCore.Mvc;
using MoneyPAuthClient.Models;
using MoneyPAuthClient.Services;
using System.Linq.Expressions;
using System.Security.Authentication;
using System.Text;

namespace MoneyPAuthClientWeb.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        

        [HttpGet("/Login")]
        public async Task<ActionResult> Login()
        {

            var accessToken = string.Empty;

            try
            {
                // Carrega as configurações do arquivo appsettings.json
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var authConfig = new AuthConfig();
                configuration.GetSection("AuthConfig").Bind(authConfig);

                

                // Cria o serviço de autenticação
                using var authService = new MoneyPAuthService(authConfig);

                //Console.WriteLine("Obtendo token de acesso...\n");

                // Obtém o token de acesso
                accessToken = await authService.GetAccessTokenAsync();

                
                var tokenResponse = authService.GetCurrentTokenResponse();
                
                using var authorizedClient = await authService.CreateAuthorizedClientAsync();
                
            }
            catch (FileNotFoundException ex)
            {
                var resultado = new StringBuilder();

                resultado.Append($"\n[ERRO] Arquivo não encontrado: {ex.Message}");
                resultado.Append("\nCertifique-se de que:");
                resultado.Append("  1. O arquivo 'private.pem' existe no diretório da aplicação");
                resultado.Append("  2. O arquivo 'appsettings.json' está configurado corretamente");

                return BadRequest(resultado.ToString());
            }
            catch (HttpRequestException ex)
            {
                
                var resultado = new StringBuilder();
                resultado.Append($"\n[ERRO] Falha na requisição HTTP: {ex.Message}");
                resultado.Append("\nVerifique:");
                resultado.Append("  1. Se o client_id está correto");
                resultado.Append("  2. Se a chave privada corresponde à chave pública cadastrada");
                resultado.Append("  3. Se os scopes solicitados são válidos");

                return BadRequest(resultado.ToString());
            }
            catch (Exception ex)
            {
                var resultado = new StringBuilder();

                resultado.Append($"\n[ERRO] {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    resultado.Append($"  Inner Exception: {ex.InnerException.Message}");
                }

                return BadRequest(resultado.ToString());
            }
            
            /*
            var client = new HttpClient();

            
            var request = new HttpRequestMessage(HttpMethod.Post, "https://auth.moneyp.dev.br/connect/token");
            request.Headers.Add("Cache-Control", "no-cache");
            var collection = new List<KeyValuePair<string, string>>();
            collection.Add(new("grant_type", "client_credentials"));
            collection.Add(new("client_id", "dbs.api.ext.intra"));
            collection.Add(new("scope", "api.ext api.conta.saldo api.conta.extrato"));
            collection.Add(new("client_assertion", "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiIzNWI3N2Q5Ni1mMmNjLTRjZWQtYmNmZC04YmIwNDNkMDgwZmYiLCJzdWIiOiJkYnMuYXBpLmV4dC5pbnRyYSIsImlhdCI6MTc2NjQzNDE3MywibmJmIjoxNzY2NDM0MTczLCJleHAiOjE3NjY0Mzc3NzMsImlzcyI6ImRicy5hcGkuZXh0LmludHJhIiwiYXVkIjoiaHR0cHM6Ly9hdXRoLm1vbmV5cC5kZXYuYnIvY29ubmVjdC90b2tlbiJ9.XzfuK23YKfLiPUPpHJiePwczthsK7K9bT9U9z-VUHv4IrS31115EAOz8XBsMi5rs2zJTWgv5QgLs04fXp9kXJRX3eueenK5htlA0W0HQVwHnxQysJ4spXV5GcDTDRHjkcEf889ZppVXULAGZ-9Z0WSgcNJg6S9co3f2ICkJTmnWuUKcETwMzfjUbQNAesJ5wdaaaRxed_jUpYSed6pfN8cYcDNyDATwGIbRpKPPStGVUJ0toKQ9107eVjAc3TX5aUyrExZIwq_lNSrYB3Z6klNYvR4FoutrO4YfR6VHyBg-bMgiAbexIDxUTKvOR7nme_x7Xo2pgKXrMUre0rCPJcw"));
            collection.Add(new("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"));
            var content = new FormUrlEncodedContent(collection);
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            */

            return Ok(accessToken);
        }
    }
}
