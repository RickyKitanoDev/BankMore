using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tarifa.API.Application.Configuration;
using Tarifa.API.Domain.Interfaces;
using Tarifa.API.Infrastructure.Kafka;
using Tarifa.API.Infrastructure.Persistence;
using Tarifa.API.Infrastructure.Repositories;

namespace Tarifa.Tests.Integration;

public class TarifaWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Jwt:Key"] = "test-key-for-integration-tests-purposes-only-min-32-chars",
                ["Jwt:Issuer"] = "TarifaAPI-Test",
                ["Jwt:Audience"] = "TarifaAPI-Test",
                ["Jwt:ExpireMinutes"] = "120",
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:GroupId"] = "tarifa-service-test",
                ["Kafka:Topics:TransferenciasRealizadas"] = "transferencias-realizadas-test",
                ["Kafka:Topics:TarifasRealizadas"] = "tarifas-realizadas-test",
                ["ConnectionStrings:DefaultConnection"] = "Data Source=tarifa_test.db",
                ["Tarifa:ValorPorTransferencia"] = "2.00"
            }!);
        });

        builder.ConfigureServices(services =>
        {
            // Remove o HostedService do Kafka Consumer para os testes
            var descriptorConsumer = services.SingleOrDefault(
                d => d.ImplementationType?.Name == "TarifaConsumerService");

            if (descriptorConsumer != null)
            {
                services.Remove(descriptorConsumer);
            }

            // Remove o KafkaProducer real e adiciona um mock
            var descriptorProducer = services.SingleOrDefault(
                d => d.ServiceType == typeof(IKafkaProducer));

            if (descriptorProducer != null)
            {
                services.Remove(descriptorProducer);
            }

            // Usar banco de dados de teste
            services.AddScoped<IDbConnectionFactory, SqliteConnectionFactory>();
            services.AddScoped<ITarifacaoRepository, TarifacaoRepository>();

            // Mock do Kafka Producer para testes
            services.AddSingleton<IKafkaProducer>(sp => 
            {
                var mock = new Moq.Mock<IKafkaProducer>();
                mock.Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
                    .Returns(Task.CompletedTask);
                return mock.Object;
            });

            services.AddSingleton<TarifaConfiguration>();
            services.AddScoped<DbInitializer>();
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
        
        await Dapper.SqlMapper.ExecuteAsync(connection, "DELETE FROM Tarifacao");
    }
}
