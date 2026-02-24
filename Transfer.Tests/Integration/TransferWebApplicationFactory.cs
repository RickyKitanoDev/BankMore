using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Http;
using Moq;
using Transfer.API.Domain.Interfaces;
using Transfer.API.Infrastructure.Persistence;
using Transfer.API.Infrastructure.Repositories;
using Transfer.API.Infrastructure.Http;
using Transfer.API.Infrastructure.Kafka;

namespace Transfer.Tests.Integration;

public class TransferWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IKafkaProducer> MockKafkaProducer { get; } = new();
    public Mock<IAccountApiClient> MockAccountApiClient { get; } = new();
    public Mock<IHttpContextAccessor> MockHttpContextAccessor { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=transfer_test.db",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["Jwt:Key"] = "super-secret-key-for-testing-purposes-with-at-least-32-characters-long",
                ["Jwt:Issuer"] = "TransferAPI-Test",
                ["Jwt:Audience"] = "TransferAPI-Test",
                ["AccountApi:BaseUrl"] = "http://localhost:5001",
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:Topics:TransferenciasRealizadas"] = "transferencias-realizadas-test"
            }!);
        });

        builder.ConfigureServices(services =>
        {
            // Remove o Kafka Producer real e usa o mock
            var descriptorKafka = services.SingleOrDefault(
                d => d.ServiceType == typeof(IKafkaProducer));
            if (descriptorKafka != null)
            {
                services.Remove(descriptorKafka);
            }
            services.AddSingleton(MockKafkaProducer.Object);

            // Remove o AccountApiClient real e usa o mock
            var descriptorHttpClient = services.SingleOrDefault(
                d => d.ServiceType == typeof(IAccountApiClient));
            if (descriptorHttpClient != null)
            {
                services.Remove(descriptorHttpClient);
            }
            services.AddScoped<IAccountApiClient>(_ => MockAccountApiClient.Object);

            // Remove HttpContextAccessor real e usa o mock
            var descriptorHttpContext = services.SingleOrDefault(
                d => d.ServiceType == typeof(IHttpContextAccessor));
            if (descriptorHttpContext != null)
            {
                services.Remove(descriptorHttpContext);
            }
            services.AddSingleton(MockHttpContextAccessor.Object);

            // Remove Redis Cache e usa Memory Cache para testes
            var descriptorCache = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDistributedCache));
            if (descriptorCache != null)
            {
                services.Remove(descriptorCache);
            }
            services.AddDistributedMemoryCache();

            // Serviços normais
            services.AddScoped<IDbConnectionFactory, SqliteConnectionFactory>();
            services.AddScoped<ITransferenciaRepository, TransferenciaRepository>();
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

        await Dapper.SqlMapper.ExecuteAsync(connection, "DELETE FROM Transferencia");
    }

    public void SetupSuccessfulVerification(string token = "fake-test-token")
    {
        // Mock: HttpContext com token
        var mockHttpContext = new Mock<HttpContext>();
        var mockRequest = new Mock<HttpRequest>();
        var headers = new HeaderDictionary
        {
            { "Authorization", $"Bearer {token}" }
        };

        mockRequest.Setup(r => r.Headers).Returns(headers);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        MockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        // Mock: ValidarConta sempre retorna true (agora usa int)
        MockAccountApiClient
            .Setup(x => x.ValidarConta(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Mock: ObterSaldo sempre retorna saldo suficiente (agora usa int)
        MockAccountApiClient
            .Setup(x => x.ObterSaldo(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(1000m);

        // Mock: RealizarMovimentacaoAsync sempre tem sucesso (agora usa int?)
        MockAccountApiClient
            .Setup(x => x.RealizarMovimentacaoAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<int?>(), 
                It.IsAny<decimal>(), 
                It.IsAny<char>()))
            .ReturnsAsync(true);

        // Mock: Kafka PublishAsync sempre tem sucesso
        MockKafkaProducer
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);
    }

    public void ResetMocks()
    {
        MockKafkaProducer.Reset();
        MockAccountApiClient.Reset();
        MockHttpContextAccessor.Reset();
    }
}
