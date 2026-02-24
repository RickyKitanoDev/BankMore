using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Transfer.API.Application.Commands;

namespace Transfer.Tests.Integration;

public class TransferEndToEndIntegrationTests : IClassFixture<TransferWebApplicationFactory>, IAsyncLifetime
{
    private readonly TransferWebApplicationFactory _factory;
    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;

    public TransferEndToEndIntegrationTests(TransferWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        await _factory.CleanupDatabaseAsync();
        _factory.ResetMocks();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task FluxoCompleto_TransferenciaSimples_DeveExecutarComSucesso()
    {
        // Arrange
        var valor = 250.00m;
        var contaDestinoNumero = 54321;

        _factory.SetupSuccessfulVerification();

        var command = new RealizarTransferenciaCommand(
            $"TRANSFER-{Guid.NewGuid()}",
            contaDestinoNumero,
            valor
        )
        {
            ContaOrigemId = Guid.NewGuid(),
            ContaOrigemNumero = 12345
        };

        // Act
        await _mediator.Send(command);

        // Assert - Verifica movimentações (2 chamadas: débito + crédito)
        _factory.MockAccountApiClient.Verify(
            x => x.RealizarMovimentacaoAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<int?>(), 
                It.IsAny<decimal>(), 
                It.IsAny<char>()),
            Times.Exactly(2),
            "Deve realizar 2 movimentações (débito + crédito)");

        _factory.MockKafkaProducer.Verify(
            x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>()),
            Times.Once,
            "Deve publicar evento no Kafka");
    }

    [Fact]
    public async Task FluxoCompleto_MultiplasTransferencias_DeveExecutarEmSequencia()
    {
        // Arrange - Simula múltiplas transferências
        _factory.SetupSuccessfulVerification();

        var commands = Enumerable.Range(1, 3)
            .Select(i => new RealizarTransferenciaCommand(
                $"TRANSFER-{Guid.NewGuid()}",
                54321 + i,
                50.00m
            )
            {
                ContaOrigemId = Guid.NewGuid(),
                ContaOrigemNumero = 12345
            })
            .ToList();

        // Act
        foreach (var command in commands)
        {
            await _mediator.Send(command);
        }

        // Assert
        _factory.MockAccountApiClient.Verify(
            x => x.RealizarMovimentacaoAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<int?>(), 
                It.IsAny<decimal>(), 
                It.IsAny<char>()),
            Times.AtLeast(3),
            "Deve realizar movimentações para cada transferência");

        _factory.MockKafkaProducer.Verify(
            x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>()),
            Times.Exactly(3),
            "Deve publicar 3 eventos");
    }

    [Fact]
    public async Task FluxoCompleto_TransferenciaIdempotente_NaoDeveDuplicar()
    {
        // Arrange
        var identificacao = $"TRANSFER-{Guid.NewGuid()}";

        _factory.SetupSuccessfulVerification();

        var command = new RealizarTransferenciaCommand(
            identificacao,
            54321,
            100.00m
        )
        {
            ContaOrigemId = Guid.NewGuid(),
            ContaOrigemNumero = 12345
        };

        // Act - Executar 3 vezes
        await _mediator.Send(command);
        await _mediator.Send(command);
        await _mediator.Send(command);

        // Assert - Evento deve ser publicado apenas uma vez
        _factory.MockKafkaProducer.Verify(
            x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>()),
            Times.Once,
            "Evento Kafka deve ser publicado apenas uma vez (idempotência)");
    }

    [Fact]
    public async Task FluxoCompleto_FalhaValidacao_DeveReverterTransacao()
    {
        // Arrange
        _factory.MockAccountApiClient
            .Setup(x => x.ValidarConta(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var command = new RealizarTransferenciaCommand(
            $"TRANSFER-{Guid.NewGuid()}",
            54321,
            100.00m
        )
        {
            ContaOrigemId = Guid.NewGuid(),
            ContaOrigemNumero = 12345
        };

        // Act & Assert
        var act = async () => await _mediator.Send(command);
        await act.Should().ThrowAsync<Exception>();

        // Verificar que nenhuma movimentação foi realizada
        _factory.MockAccountApiClient.Verify(
            x => x.RealizarMovimentacaoAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<int?>(), 
                It.IsAny<decimal>(), 
                It.IsAny<char>()),
            Times.Never,
            "Não deve realizar movimentações se validação falhar");

        _factory.MockKafkaProducer.Verify(
            x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>()),
            Times.Never,
            "Não deve publicar evento se validação falhar");
    }

    [Fact]
    public async Task FluxoCompleto_TransferenciasMesmoValor_DeveProcessarTodas()
    {
        // Arrange
        var valor = 100.00m;
        _factory.SetupSuccessfulVerification();

        var transferencias = Enumerable.Range(1, 5)
            .Select(i => new RealizarTransferenciaCommand(
                $"TRANSFER-{Guid.NewGuid()}",
                54321 + i,
                valor
            )
            {
                ContaOrigemId = Guid.NewGuid(),
                ContaOrigemNumero = 12345
            })
            .ToList();

        // Act
        foreach (var transferencia in transferencias)
        {
            await _mediator.Send(transferencia);
        }

        // Assert
        _factory.MockKafkaProducer.Verify(
            x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>()),
            Times.Exactly(5),
            "Deve publicar 5 eventos");
    }

    [Fact]
    public async Task FluxoCompleto_DiferentesValores_DeveProcessarCorretamente()
    {
        // Arrange
        var valores = new[] { 10.00m, 100.00m, 1000.00m };
        _factory.SetupSuccessfulVerification();

        var commands = valores.Select((valor, i) => new RealizarTransferenciaCommand(
            $"TRANSFER-{Guid.NewGuid()}",
            54321 + i,
            valor
        )
        {
            ContaOrigemId = Guid.NewGuid(),
            ContaOrigemNumero = 12345
        }).ToList();

        // Act
        foreach (var command in commands)
        {
            await _mediator.Send(command);
        }

        // Assert
        _factory.MockKafkaProducer.Verify(
            x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>()),
            Times.Exactly(3),
            "Deve publicar evento para cada transferência");
    }
}
