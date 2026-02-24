using Tarifa.API.Application.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Tarifa.Tests.Configuration;

public class TarifaConfigurationTests
{
    [Fact]
    public void TarifaConfiguration_DeveInicializarComValorConfigurado()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["Tarifa:ValorPorTransferencia"]).Returns("100"); // Sem ponto decimal

        // Act
        var config = new TarifaConfiguration(configMock.Object);

        // Assert
        config.ValorPorTransferencia.Should().Be(100m);
    }

    [Fact]
    public void TarifaConfiguration_DeveUsarValorPadrao_QuandoNaoConfigurado()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["Tarifa:ValorPorTransferencia"]).Returns((string?)null);

        // Act
        var config = new TarifaConfiguration(configMock.Object);

        // Assert
        config.ValorPorTransferencia.Should().Be(2.00m); // Valor padrão
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("5", 5)]
    [InlineData("10", 10)]
    public void TarifaConfiguration_DeveLerDiferentesValores(string valorConfig, decimal valorEsperado)
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["Tarifa:ValorPorTransferencia"]).Returns(valorConfig);

        // Act
        var config = new TarifaConfiguration(configMock.Object);

        // Assert
        config.ValorPorTransferencia.Should().Be(valorEsperado);
    }

    [Fact]
    public void TarifaConfiguration_DeveSuportarValoresInteiros()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["Tarifa:ValorPorTransferencia"]).Returns("199");

        // Act
        var config = new TarifaConfiguration(configMock.Object);

        // Assert
        config.ValorPorTransferencia.Should().Be(199m);
    }

    [Fact]
    public void TarifaConfiguration_DeveUsarPadrao_ParaStringVazia()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["Tarifa:ValorPorTransferencia"]).Returns("");

        // Act
        var config = new TarifaConfiguration(configMock.Object);

        // Assert
        config.ValorPorTransferencia.Should().Be(2.00m);
    }
}
