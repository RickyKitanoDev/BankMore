using Tarifa.API.Application.Commands;
using Tarifa.API.Application.Services;
using Tarifa.API.Infrastructure.Kafka.Events;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace Tarifa.Tests.Services;

public class TarifaConsumerServiceTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<TarifaConsumerService>> _loggerMock;
    private readonly IServiceProvider _serviceProvider;

    public TarifaConsumerServiceTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<TarifaConsumerService>>();

        // Setup service provider com mediator mockado
        var services = new ServiceCollection();
        services.AddSingleton(_mediatorMock.Object);
        _serviceProvider = services.BuildServiceProvider();

        // Mock configuration
        _configurationMock.Setup(x => x["Kafka:BootstrapServers"]).Returns("localhost:9092");
        _configurationMock.Setup(x => x["Kafka:GroupId"]).Returns("tarifa-service");
        _configurationMock.Setup(x => x["Kafka:Topics:TransferenciasRealizadas"]).Returns("transferencias-realizadas");
    }

    [Fact]
    public void ProcessarTarifaCommand_DeveSerCriadoCorretamente()
    {
        // Arrange
        var identificacao = "transfer-123";
        var contaId = Guid.NewGuid();
        var valor = 100.00m;

        // Act
        var command = new ProcessarTarifaCommand(identificacao, contaId, valor);

        // Assert
        command.IdentificacaoTransferencia.Should().Be(identificacao);
        command.ContaOrigemId.Should().Be(contaId);
        command.ValorTransferencia.Should().Be(valor);
    }

    [Fact]
    public void TransferenciaRealizadaEvent_DeveDeserializarCorretamente()
    {
        // Arrange
        var evento = new TransferenciaRealizadaEvent
        {
            IdentificacaoRequisicao = "transfer-456",
            ContaOrigemId = Guid.NewGuid(),
            ContaDestinoId = Guid.NewGuid(),
            Valor = 250.00m,
            DataTransferencia = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(evento);

        // Act
        var desserializado = JsonSerializer.Deserialize<TransferenciaRealizadaEvent>(json);

        // Assert
        desserializado.Should().NotBeNull();
        desserializado!.IdentificacaoRequisicao.Should().Be(evento.IdentificacaoRequisicao);
        desserializado.ContaOrigemId.Should().Be(evento.ContaOrigemId);
        desserializado.Valor.Should().Be(evento.Valor);
    }

    [Fact]
    public async Task Mediator_DeveEnviarComandoCorretamente()
    {
        // Arrange
        var command = new ProcessarTarifaCommand(
            "mediator-test",
            Guid.NewGuid(),
            300.00m
        );

        _mediatorMock
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        // Act
        await _mediatorMock.Object.Send(command, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(x => x.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Configuration_DeveConterValoresPadrao()
    {
        // Arrange & Act
        var bootstrapServers = _configurationMock.Object["Kafka:BootstrapServers"];
        var groupId = _configurationMock.Object["Kafka:GroupId"];
        var topic = _configurationMock.Object["Kafka:Topics:TransferenciasRealizadas"];

        // Assert
        bootstrapServers.Should().Be("localhost:9092");
        groupId.Should().Be("tarifa-service");
        topic.Should().Be("transferencias-realizadas");
    }

    [Theory]
    [InlineData("transfer-1", 10.00)]
    [InlineData("transfer-2", 100.00)]
    [InlineData("transfer-3", 1000.00)]
    [InlineData("transfer-4", 0.01)]
    public void ProcessarTarifaCommand_DeveAceitarDiferentesValores(string id, decimal valor)
    {
        // Arrange & Act
        var command = new ProcessarTarifaCommand(id, Guid.NewGuid(), valor);

        // Assert
        command.IdentificacaoTransferencia.Should().Be(id);
        command.ValorTransferencia.Should().Be(valor);
    }
}
