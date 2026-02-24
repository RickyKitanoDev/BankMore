using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Transfer.API.Application.Commands;
using Transfer.API.Domain.Interfaces;

namespace Transfer.Tests.Integration.Transferencia;

public class RealizarTransferenciaIntegrationTests : IClassFixture<TransferWebApplicationFactory>, IAsyncLifetime
{
    private readonly TransferWebApplicationFactory _factory;
    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly ITransferenciaRepository _repository;

    public RealizarTransferenciaIntegrationTests(TransferWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _repository = _scope.ServiceProvider.GetRequiredService<ITransferenciaRepository>();
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
    public async Task RealizarTransferencia_ComDadosValidos_DeveExecutarComSucesso()
    {
        // Arrange
        var identificacao = $"TRANSFER-{Guid.NewGuid()}";
        var contaDestinoNumero = 54321;
        var valor = 100.00m;

        _factory.SetupSuccessfulVerification();

        var command = new RealizarTransferenciaCommand(
            identificacao,
            contaDestinoNumero,
            valor
        )
        {
            ContaOrigemId = Guid.NewGuid(),
            ContaOrigemNumero = 12345
        };

        // Act
        await _mediator.Send(command);

        // Assert - Verifica com retry
        var existe = await VerificarTransferenciaComRetryAsync(identificacao);
        existe.Should().BeTrue();

        // Verifica que movimentações foram realizadas (débito e crédito)
        _factory.MockAccountApiClient.Verify(
            x => x.RealizarMovimentacaoAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<int?>(), 
                It.IsAny<decimal>(), 
                'D'), // Débito
            Times.Once,
            "Deve realizar débito");

        _factory.MockAccountApiClient.Verify(
            x => x.RealizarMovimentacaoAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<int?>(), 
                It.IsAny<decimal>(), 
                'C'), // Crédito
            Times.Once,
            "Deve realizar crédito");

        // Verifica que Kafka foi chamado
        _factory.MockKafkaProducer.Verify(
            x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task RealizarTransferencia_DeveGarantirIdempotencia()
    {
        // Arrange
        var identificacao = $"TRANSFER-{Guid.NewGuid()}";
        var contaDestinoNumero = 54321;

        _factory.SetupSuccessfulVerification();

        var command = new RealizarTransferenciaCommand(
            identificacao,
            contaDestinoNumero,
            100.00m
        )
        {
            ContaOrigemId = Guid.NewGuid(),
            ContaOrigemNumero = 12345
        };

        // Act - Executar duas vezes com mesma identificação
        await _mediator.Send(command);
        await _mediator.Send(command);

        // Assert - Deve existir apenas uma transferência
        var existe = await VerificarTransferenciaComRetryAsync(identificacao);
        existe.Should().BeTrue();

        // Kafka deve ser chamado apenas uma vez (idempotência)
        _factory.MockKafkaProducer.Verify(
            x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>()),
            Times.Once,
            "Evento só deve ser publicado na primeira vez");
    }

    [Fact]
    public async Task RealizarTransferencia_DeveRejeitarContaOrigemInvalida()
    {
        // Arrange
        var identificacao = $"TRANSFER-{Guid.NewGuid()}";

        // Mock: Conta origem não existe
        _factory.MockAccountApiClient
            .Setup(x => x.ValidarConta(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var command = new RealizarTransferenciaCommand(
            identificacao,
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
    }

    [Fact]
    public async Task RealizarTransferencia_DeveRejeitarContaDestinoInvalida()
    {
        // Arrange
        var identificacao = $"TRANSFER-{Guid.NewGuid()}";

        // Mock: Conta origem OK, mas destino inválido
        _factory.MockAccountApiClient
            .Setup(x => x.ValidarConta(It.Is<int>(num => num == 12345), It.IsAny<string>()))
            .ReturnsAsync(true);

        _factory.MockAccountApiClient
            .Setup(x => x.ValidarConta(It.Is<int>(num => num == 54321), It.IsAny<string>()))
            .ReturnsAsync(false);

        var command = new RealizarTransferenciaCommand(
            identificacao,
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
    }

    [Fact]
    public async Task RealizarTransferencia_DeveProcessarMultiplasTransferencias()
    {
        // Arrange
        _factory.SetupSuccessfulVerification();

        var commands = Enumerable.Range(1, 5)
            .Select(i => new RealizarTransferenciaCommand(
                $"TRANSFER-{Guid.NewGuid()}",
                54321 + i,
                100.00m
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

        // Assert - Verifica com retry
        foreach (var command in commands)
        {
            var existe = await VerificarTransferenciaComRetryAsync(command.IdentificacaoRequisicao);
            existe.Should().BeTrue($"transferência {command.IdentificacaoRequisicao} deve existir");
        }
    }

    [Fact]
    public async Task RealizarTransferencia_ComDiferentesValores_DeveExecutarComSucesso()
    {
        // Arrange
        _factory.SetupSuccessfulVerification();

        var valores = new[] { 10.00m, 100.00m, 1000.00m, 0.01m };
        var commands = valores.Select((valor, i) =>
            new RealizarTransferenciaCommand(
                $"TRANSFER-{Guid.NewGuid()}",
                54321 + i,
                valor
            )
            {
                ContaOrigemId = Guid.NewGuid(),
                ContaOrigemNumero = 12345
            }
        ).ToList();

        // Act
        foreach (var command in commands)
        {
            await _mediator.Send(command);
        }

        // Assert
        foreach (var command in commands)
        {
            var existe = await VerificarTransferenciaComRetryAsync(command.IdentificacaoRequisicao);
            existe.Should().BeTrue($"transferência de valor {command.Valor} deve ser processada");
        }
    }

    private async Task<bool> VerificarTransferenciaComRetryAsync(string identificacao, int maxRetries = 10)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            using var verifyScope = _factory.Services.CreateScope();
            var verifyRepository = verifyScope.ServiceProvider.GetRequiredService<ITransferenciaRepository>();

            var existe = await verifyRepository.ExistePorIdentificacao(identificacao);
            if (existe)
                return true;

            await Task.Delay(100);
        }

        return false;
    }
}
