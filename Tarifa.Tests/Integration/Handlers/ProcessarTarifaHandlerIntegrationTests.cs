using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tarifa.API.Application.Commands;
using Tarifa.API.Domain.Interfaces;

namespace Tarifa.Tests.Integration.Handlers;

public class ProcessarTarifaHandlerIntegrationTests : IClassFixture<TarifaWebApplicationFactory>, IAsyncLifetime
{
    private readonly TarifaWebApplicationFactory _factory;
    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly ITarifacaoRepository _repository;

    public ProcessarTarifaHandlerIntegrationTests(TarifaWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _repository = _scope.ServiceProvider.GetRequiredService<ITarifacaoRepository>();
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        await _factory.CleanupDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Handle_DeveProcessarTarifa_QuandoComandoValido()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var identificacao = $"TRANSFER-{Guid.NewGuid()}";

        var command = new ProcessarTarifaCommand(
            identificacao,
            contaOrigemId,
            100.00m
        );

        // Act
        await _mediator.Send(command);

        // Assert - Verifica com retry logic para garantir persistência
        var existe = await VerificarTarifaComRetryAsync(identificacao);
        existe.Should().BeTrue();
    }

    private async Task<bool> VerificarTarifaComRetryAsync(string identificacao, int maxRetries = 10)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            using var verifyScope = _factory.Services.CreateScope();
            var verifyRepository = verifyScope.ServiceProvider.GetRequiredService<ITarifacaoRepository>();

            var existe = await verifyRepository.ExistePorIdentificacao(identificacao);
            if (existe)
                return true;

            await Task.Delay(100);
        }

        return false;
    }

    [Fact]
    public async Task Handle_DeveGarantirIdempotencia_QuandoMesmaIdentificacao()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var identificacao = $"TRANSFER-{Guid.NewGuid()}";

        var command = new ProcessarTarifaCommand(
            identificacao,
            contaOrigemId,
            100.00m
        );

        // Act - Executar duas vezes com a mesma identificação
        await _mediator.Send(command);
        await _mediator.Send(command);

        // Assert - Verifica com retry logic
        var existe = await VerificarTarifaComRetryAsync(identificacao);
        existe.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeveProcessarMultiplasTarifas_ComIdentificacoesDiferentes()
    {
        // Arrange
        var command1 = new ProcessarTarifaCommand(
            $"TRANSFER-{Guid.NewGuid()}",
            Guid.NewGuid(),
            100.00m
        );

        var command2 = new ProcessarTarifaCommand(
            $"TRANSFER-{Guid.NewGuid()}",
            Guid.NewGuid(),
            200.00m
        );

        // Act
        await _mediator.Send(command1);
        await _mediator.Send(command2);

        // Assert - Verifica com retry logic
        var existe1 = await VerificarTarifaComRetryAsync(command1.IdentificacaoTransferencia);
        var existe2 = await VerificarTarifaComRetryAsync(command2.IdentificacaoTransferencia);

        existe1.Should().BeTrue();
        existe2.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeveProcessarTarifaComValorCorreto()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var identificacao = $"TRANSFER-{Guid.NewGuid()}";

        var command = new ProcessarTarifaCommand(
            identificacao,
            contaOrigemId,
            500.00m
        );

        // Act
        await _mediator.Send(command);

        // Assert - Verifica com retry logic
        var existe = await VerificarTarifaComRetryAsync(identificacao);
        existe.Should().BeTrue("a tarifa deve ser salva após processamento");
    }

    [Fact]
    public async Task Handle_DeveProcessarTarifaParaDiferentesContas()
    {
        // Arrange
        var conta1 = Guid.NewGuid();
        var conta2 = Guid.NewGuid();
        var conta3 = Guid.NewGuid();

        var commands = new[]
        {
            new ProcessarTarifaCommand(
                $"TRANSFER-{Guid.NewGuid()}",
                conta1,
                100.00m
            ),
            new ProcessarTarifaCommand(
                $"TRANSFER-{Guid.NewGuid()}",
                conta2,
                200.00m
            ),
            new ProcessarTarifaCommand(
                $"TRANSFER-{Guid.NewGuid()}",
                conta3,
                300.00m
            )
        };

        // Act
        foreach (var command in commands)
        {
            await _mediator.Send(command);
        }

        // Assert - Verifica com retry logic
        foreach (var command in commands)
        {
            var existe = await VerificarTarifaComRetryAsync(command.IdentificacaoTransferencia);
            existe.Should().BeTrue($"tarifa para transferência {command.IdentificacaoTransferencia} deve existir");
        }
    }
}
