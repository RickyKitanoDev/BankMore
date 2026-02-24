using Transfer.API.Application.Commands;
using Transfer.API.Application.Handlers;
using Transfer.API.Domain.Entities;
using Transfer.API.Domain.Exceptions;
using Transfer.API.Domain.Interfaces;
using Transfer.API.Infrastructure.Http;
using Transfer.API.Infrastructure.Kafka;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Transfer.Tests.Handlers;

public class RealizarTransferenciaHandlerTests
{
    private readonly Mock<ITransferenciaRepository> _repositoryMock;
    private readonly Mock<IAccountApiClient> _accountApiMock;
    private readonly Mock<IKafkaProducer> _kafkaProducerMock;
    private readonly Mock<ILogger<RealizarTransferenciaHandler>> _loggerMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly RealizarTransferenciaHandler _handler;

    public RealizarTransferenciaHandlerTests()
    {
        _repositoryMock = new Mock<ITransferenciaRepository>();
        _accountApiMock = new Mock<IAccountApiClient>();
        _kafkaProducerMock = new Mock<IKafkaProducer>();
        _loggerMock = new Mock<ILogger<RealizarTransferenciaHandler>>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _configurationMock = new Mock<IConfiguration>();

        // Mock HttpContext com token
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Bearer fake-jwt-token";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _handler = new RealizarTransferenciaHandler(
            _repositoryMock.Object,
            _accountApiMock.Object,
            _kafkaProducerMock.Object,
            _loggerMock.Object,
            _httpContextAccessorMock.Object,
            _configurationMock.Object
        );
    }

    [Fact]
    public async Task Handle_DeveRealizarTransferenciaComSucesso()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var command = new RealizarTransferenciaCommand(
            "transfer-123",
            54321,
            100.00m
        )
        {
            ContaOrigemId = contaOrigemId,
            ContaOrigemNumero = 12345
        };

        _repositoryMock
            .Setup(x => x.ExistePorIdentificacao(command.IdentificacaoRequisicao))
            .ReturnsAsync(false);

        _accountApiMock
            .Setup(x => x.RealizarMovimentacaoAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                command.Valor,
                'D'))
            .ReturnsAsync(true);

        _accountApiMock
            .Setup(x => x.RealizarMovimentacaoAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                command.ContaDestinoNumero,
                command.Valor,
                'C'))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.AdicionarAsync(It.IsAny<Transferencia>()))
            .Returns(Task.CompletedTask);

        _kafkaProducerMock
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _accountApiMock.Verify(x => x.RealizarMovimentacaoAsync(It.IsAny<string>(), It.IsAny<string>(), null, command.Valor, 'D'), Times.Once);
        _accountApiMock.Verify(x => x.RealizarMovimentacaoAsync(It.IsAny<string>(), It.IsAny<string>(), command.ContaDestinoNumero, command.Valor, 'C'), Times.Once);
        _kafkaProducerMock.Verify(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRespeitarIdempotencia()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var command = new RealizarTransferenciaCommand(
            "duplicated-456",
            54321,
            100.00m
        )
        {
            ContaOrigemId = contaOrigemId,
            ContaOrigemNumero = 12345
        };

        _repositoryMock
            .Setup(x => x.ExistePorIdentificacao(command.IdentificacaoRequisicao))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _accountApiMock.Verify(x => x.RealizarMovimentacaoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<decimal>(), It.IsAny<char>()), Times.Never);
        _repositoryMock.Verify(x => x.AdicionarAsync(It.IsAny<Transferencia>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task Handle_DeveLancarExcecao_QuandoValorInvalido(decimal valorInvalido)
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var command = new RealizarTransferenciaCommand(
            "invalid-789",
            54321,
            valorInvalido
        )
        {
            ContaOrigemId = contaOrigemId,
            ContaOrigemNumero = 12345
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task Handle_DeveLancarExcecao_QuandoTokenNaoEncontrado()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var command = new RealizarTransferenciaCommand(
            "no-token-111",
            54321,
            100.00m
        )
        {
            ContaOrigemId = contaOrigemId,
            ContaOrigemNumero = 12345
        };

        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _repositoryMock
            .Setup(x => x.ExistePorIdentificacao(command.IdentificacaoRequisicao))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task Handle_DevePublicarEventoKafkaComDadosCorretos()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var command = new RealizarTransferenciaCommand(
            "kafka-555",
            54321,
            200.00m
        )
        {
            ContaOrigemId = contaOrigemId,
            ContaOrigemNumero = 12345
        };

        _repositoryMock
            .Setup(x => x.ExistePorIdentificacao(command.IdentificacaoRequisicao))
            .ReturnsAsync(false);

        _accountApiMock
            .Setup(x => x.RealizarMovimentacaoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<decimal>(), It.IsAny<char>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.AdicionarAsync(It.IsAny<Transferencia>()))
            .Returns(Task.CompletedTask);

        _kafkaProducerMock
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _kafkaProducerMock.Verify(x => x.PublishAsync("transferencias-realizadas", It.IsAny<object>()), Times.Once);
    }
}
