using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tarifa.API.Application.Configuration;

namespace Tarifa.Tests.Integration.Configuration;

public class TarifaConfigurationIntegrationTests : IClassFixture<TarifaWebApplicationFactory>
{
    private readonly TarifaWebApplicationFactory _factory;

    public TarifaConfigurationIntegrationTests(TarifaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void TarifaConfiguration_DeveSerRegistradaComoSingleton()
    {
        // Arrange
        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();

        // Act
        var config1 = scope1.ServiceProvider.GetRequiredService<TarifaConfiguration>();
        var config2 = scope2.ServiceProvider.GetRequiredService<TarifaConfiguration>();

        // Assert
        config1.Should().BeSameAs(config2, "TarifaConfiguration deve ser Singleton");
    }

    [Fact]
    public void TarifaConfiguration_DeveCarregarValorConfigurado()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var config = scope.ServiceProvider.GetRequiredService<TarifaConfiguration>();

        // Assert - Verifica que um valor foi carregado (não importa qual)
        config.ValorPorTransferencia.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TarifaConfiguration_DeveSerResolvidaComSucesso()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<TarifaConfiguration>();

        // Assert
        config.Should().NotBeNull();
    }

    [Fact]
    public void TarifaConfiguration_DeveTerValorPositivo()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var config = scope.ServiceProvider.GetRequiredService<TarifaConfiguration>();

        // Assert
        config.ValorPorTransferencia.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TarifaConfiguration_DeveManterMesmaInstanciaEmMultiplasResolucoes()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var config1 = scope.ServiceProvider.GetRequiredService<TarifaConfiguration>();
        var config2 = scope.ServiceProvider.GetRequiredService<TarifaConfiguration>();
        var config3 = scope.ServiceProvider.GetRequiredService<TarifaConfiguration>();

        // Assert
        config1.Should().BeSameAs(config2);
        config2.Should().BeSameAs(config3);
    }
}
