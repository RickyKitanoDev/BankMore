using Tarifa.API.Application.Commands;
using Tarifa.API.Application.Configuration;
using Tarifa.API.Application.Handlers;
using Tarifa.API.Domain.Entities;
using Tarifa.API.Domain.Interfaces;
using Tarifa.API.Infrastructure.Kafka;
using Tarifa.API.Infrastructure.Kafka.Events;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tarifa.Tests.Handlers;

public class ProcessarTarifaHandlerTests
{
    private readonly Mock<ITarifacaoRepository> _repositoryMock;
    private readonly Mock<IKafkaProducer> _kafkaProducerMock;
    private readonly TarifaConfiguration _tarifaConfig;
    private readonly Mock<ILogger<ProcessarTarifaHandler>> _loggerMock;
    private readonly ProcessarTarifaHandler _handler;

    public ProcessarTarifaHandlerTests()
    {
        _repositoryMock = new Mock<ITarifacaoRepository>();
        _kafkaProducerMock = new Mock<IKafkaProducer>();
        _loggerMock = new Mock<ILogger<ProcessarTarifaHandler>>();

        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x["Tarifa:ValorPorTransferencia"]).Returns("100");
        _tarifaConfig = new TarifaConfiguration(configurationMock.Object);

        _handler = new ProcessarTarifaHandler(
            _repositoryMock.Object,
            _kafkaProducerMock.Object,
            _tarifaConfig,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_DeveProcessarTarifaComSucesso()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var command = new ProcessarTarifaCommand("transfer-123", contaId, 100.00m);

        _repositoryMock
            .Setup(x => x.ExistePorIdentificacao(command.IdentificacaoTransferencia))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(x => x.AdicionarAsync(It.IsAny<Tarifacao>()))
            .Returns(Task.CompletedTask);

        _kafkaProducerMock
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<TarifaRealizadaEvent>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(x => x.ExistePorIdentificacao(command.IdentificacaoTransferencia), Times.Once);
        _repositoryMock.Verify(x => x.AdicionarAsync(It.IsAny<Tarifacao>()), Times.Once);
        _kafkaProducerMock.Verify(x => x.PublishAsync("tarifas-realizadas", It.IsAny<TarifaRealizadaEvent>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRespeitarIdempotencia()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var command = new ProcessarTarifaCommand("duplicated-456", contaId, 100.00m);

        _repositoryMock
            .Setup(x => x.ExistePorIdentificacao(command.IdentificacaoTransferencia))
            .ReturnsAsync(true); // Já existe

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Não deve processar
        _repositoryMock.Verify(x => x.ExistePorIdentificacao(command.IdentificacaoTransferencia), Times.Once);
        _repositoryMock.Verify(x => x.AdicionarAsync(It.IsAny<Tarifacao>()), Times.Never);
        _kafkaProducerMock.Verify(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DevePublicarNoTopicoCorreto()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var command = new ProcessarTarifaCommand("topic-test-777", contaId, 150.00m);

        _repositoryMock
            .Setup(x => x.ExistePorIdentificacao(command.IdentificacaoTransferencia))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(x => x.AdicionarAsync(It.IsAny<Tarifacao>()))
            .Returns(Task.CompletedTask);

        _kafkaProducerMock
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<TarifaRealizadaEvent>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _kafkaProducerMock.Verify(x => x.PublishAsync("tarifas-realizadas", It.IsAny<TarifaRealizadaEvent>()), Times.Once);
    }
}
