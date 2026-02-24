using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Account.API.Domain.Interfaces;
using Account.API.Infrastructure.Persistence;
using Account.API.Infrastructure.Repositories;
using Account.API.Infrastructure.Security;
using Account.API.Infrastructure.Authentication;

namespace Account.Tests.Integration;

public class AccountWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=account_test.db",
                ["Jwt:Key"] = "super-secret-key-for-testing-purposes-with-at-least-32-characters-long",
                ["Jwt:Issuer"] = "AccountAPI-Test",
                ["Jwt:Audience"] = "AccountAPI-Test",
                ["Jwt:ExpireMinutes"] = "120",
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:GroupId"] = "account-service-test",
                ["Kafka:Topics:TarifasRealizadas"] = "tarifas-realizadas-test"
            }!);
        });

        builder.ConfigureServices(services =>
        {
            // Remove o Kafka Consumer Service para testes
            var descriptorConsumer = services.SingleOrDefault(
                d => d.ImplementationType?.Name == "TarifaConsumerService");

            if (descriptorConsumer != null)
            {
                services.Remove(descriptorConsumer);
            }

            // Usar banco de dados de teste
            services.AddScoped<IDbConnectionFactory, SqliteConnectionFactory>();
            services.AddScoped<IContaRepository, ContaRepository>();
            services.AddScoped<MovimentoRepository>();
            services.AddScoped<IMovimentoRepository, CachedMovimentoRepository>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<DbInitializer>();

            // Memory Cache para testes
            services.AddMemoryCache();
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await initializer.InitializeAsync();
    }

    public async Task CleanupDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using var connection = factory.CreateConnection();

        // Limpa todas as tabelas
        await Dapper.SqlMapper.ExecuteAsync(connection, "DELETE FROM Movimento");
        await Dapper.SqlMapper.ExecuteAsync(connection, "DELETE FROM ContaCorrente");
    }
}

