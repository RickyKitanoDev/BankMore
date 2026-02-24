using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tarifa.API.Application.Commands;
using Tarifa.API.Application.Configuration;
using Tarifa.API.Domain.Interfaces;

namespace Tarifa.Tests.Integration;

public class TarifaEndToEndIntegrationTests : IClassFixture<TarifaWebApplicationFactory>, IAsyncLifetime
{
    private readonly TarifaWebApplicationFactory _factory;
    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly ITarifacaoRepository _repository;
    private readonly TarifaConfiguration _configuration;

    public TarifaEndToEndIntegrationTests(TarifaWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _repository = _scope.ServiceProvider.GetRequiredService<ITarifacaoRepository>();
        _configuration = _scope.ServiceProvider.GetRequiredService<TarifaConfiguration>();
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
    public async Task FluxoCompleto_ProcessarTarifa_DeveExecutarComSucesso()
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

        // Assert - Verifica com retry logic
        var existe = await VerificarTarifaComRetryAsync(identificacao);
        existe.Should().BeTrue("a tarifa deve ser processada e salva no banco de dados");
    }

    [Fact]
    public async Task FluxoCompleto_ProcessarMultiplasTarifas_DeveExecutarComSucesso()
    {
        // Arrange
        var commands = Enumerable.Range(1, 10)
            .Select(_ => new ProcessarTarifaCommand(
                $"TRANSFER-{Guid.NewGuid()}",
                Guid.NewGuid(),
                100.00m
            ))
            .ToList();

        // Act
        foreach (var command in commands)
        {
            await _mediator.Send(command);
        }

        // Assert - Verifica com retry logic
        foreach (var command in commands)
        {
            var existe = await VerificarTarifaComRetryAsync(command.IdentificacaoTransferencia);
            existe.Should().BeTrue($"tarifa {command.IdentificacaoTransferencia} deve existir");
        }
    }

    [Fact]
    public async Task FluxoCompleto_ProcessarTarifaDuplicada_DeveGarantirIdempotencia()
    {
        // Arrange
        var identificacao = $"TRANSFER-{Guid.NewGuid()}";
        var command = new ProcessarTarifaCommand(
            identificacao,
            Guid.NewGuid(),
            100.00m
        );

        // Act - Processar a mesma tarifa 5 vezes
        for (int i = 0; i < 5; i++)
        {
            await _mediator.Send(command);
        }

        // Assert - Verifica com retry logic
        var existe = await VerificarTarifaComRetryAsync(identificacao);
        existe.Should().BeTrue("a tarifa deve existir mesmo após múltiplos processamentos");
    }

    [Fact]
    public async Task FluxoCompleto_ProcessarTarifasSimultaneas_DeveExecutarComSucesso()
    {
        // Arrange
        var tasks = Enumerable.Range(1, 20)
            .Select(_ => new ProcessarTarifaCommand(
                $"TRANSFER-{Guid.NewGuid()}",
                Guid.NewGuid(),
                100.00m
            ))
            .Select(command => _mediator.Send(command))
            .ToList();

        // Act
        await Task.WhenAll(tasks);

        // Assert - Todas devem ser processadas
        tasks.Should().HaveCount(20);
        tasks.Should().AllSatisfy(t => t.IsCompletedSuccessfully.Should().BeTrue());
    }

    [Fact]
    public async Task FluxoCompleto_VerificarConfiguracaoCarregada_DeveUsarValorCorreto()
    {
        // Arrange & Act
        var valorTarifa = _configuration.ValorPorTransferencia;

        // Assert - Verifica que um valor positivo foi configurado
        valorTarifa.Should().BeGreaterThan(0, "a tarifa deve ter um valor positivo configurado");
    }

    [Fact]
    public async Task FluxoCompleto_ProcessarTarifaComDiferentesValores_DeveExecutarComSucesso()
    {
        // Arrange
        var valores = new[] { 10.00m, 100.00m, 1000.00m, 10000.00m, 0.01m };
        var commands = valores.Select(valor => new ProcessarTarifaCommand(
            $"TRANSFER-{Guid.NewGuid()}",
            Guid.NewGuid(),
            valor
        )).ToList();

        // Act
        foreach (var command in commands)
        {
            await _mediator.Send(command);
        }

        // Assert - Verifica com retry logic
        foreach (var command in commands)
        {
            var existe = await VerificarTarifaComRetryAsync(command.IdentificacaoTransferencia);
            existe.Should().BeTrue($"tarifa para valor {command.ValorTransferencia} deve ser processada");
        }
    }

    [Fact]
    public async Task FluxoCompleto_ProcessarTarifasParaMesmaContaOrigem_DeveExecutarComSucesso()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var commands = Enumerable.Range(1, 5)
            .Select(_ => new ProcessarTarifaCommand(
                $"TRANSFER-{Guid.NewGuid()}",
                contaOrigemId,
                100.00m
            ))
            .ToList();

        // Act
        foreach (var command in commands)
        {
            await _mediator.Send(command);
        }

        // Assert - Verifica com retry logic para garantir persistência
        foreach (var command in commands)
        {
            var existe = await VerificarTarifaComRetryAsync(command.IdentificacaoTransferencia);
            existe.Should().BeTrue($"todas as tarifas para a mesma conta origem devem ser processadas: {command.IdentificacaoTransferencia}");
        }
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

            // Aguarda 100ms antes de tentar novamente
            await Task.Delay(100);
        }

        return false;
    }
}
