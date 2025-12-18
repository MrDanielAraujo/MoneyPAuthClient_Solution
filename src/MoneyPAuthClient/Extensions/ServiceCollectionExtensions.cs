using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoneyPAuthClient.Models;
using MoneyPAuthClient.Services;

namespace MoneyPAuthClient.Extensions;

/// <summary>
/// Extensões para configuração de injeção de dependência
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adiciona os serviços de autenticação MoneyP ao container de DI
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    /// <param name="configuration">Configuração da aplicação</param>
    /// <returns>Coleção de serviços configurada</returns>
    public static IServiceCollection AddMoneyPAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Registra as configurações
        var authConfig = new AuthConfig();
        configuration.GetSection("AuthConfig").Bind(authConfig);
        services.AddSingleton(authConfig);

        // Registra o serviço de autenticação como Singleton
        services.AddSingleton<MoneyPAuthService>();

        return services;
    }

    /// <summary>
    /// Adiciona os serviços de autenticação MoneyP com configuração customizada
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    /// <param name="configureOptions">Ação para configurar as opções</param>
    /// <returns>Coleção de serviços configurada</returns>
    public static IServiceCollection AddMoneyPAuthentication(
        this IServiceCollection services,
        Action<AuthConfig> configureOptions)
    {
        var authConfig = new AuthConfig();
        configureOptions(authConfig);
        services.AddSingleton(authConfig);
        services.AddSingleton<MoneyPAuthService>();

        return services;
    }
}
