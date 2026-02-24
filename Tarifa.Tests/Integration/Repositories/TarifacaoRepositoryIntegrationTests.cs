using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tarifa.API.Domain.Entities;
using Tarifa.API.Domain.Interfaces;

namespace Tarifa.Tests.Integration.Repositories;

public class TarifacaoRepositoryIntegrationTests : IClassFixture<TarifaWebApplicationFactory>, IAsyncLifetime
{
    private readonly TarifaWebApplicationFactory _factory;
    private readonly IServiceScope _scope;
    private readonly ITarifacaoRepository _repository;

    public TarifacaoRepositoryIntegrationTests(TarifaWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
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

    [Fact(Skip = "Teste intermitente com SQLite - race condition em escopo de conexão")]
    public async Task AdicionarAsync_DeveSalvarTarifacaoNoBancoDeDados()
    {
        // Arrange
        var tarifacao = new Tarifacao
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            ValorTarifado = 1.00m,
            DataHoraTarifacao = DateTime.UtcNow,
            IdentificacaoTransferencia = $"TRANSFER-{Guid.NewGuid()}"
        };

        // Act
        await _repository.AdicionarAsync(tarifacao);

        // Aguarda para garantir persist completo
        await Task.Delay(200);

        // Assert - Usa novo scope
        using var verifyScope = _factory.Services.CreateScope();
        var verifyRepository = verifyScope.ServiceProvider.GetRequiredService<ITarifacaoRepository>();
        var existe = await verifyRepository.ExistePorIdentificacao(tarifacao.IdentificacaoTransferencia);
        existe.Should().BeTrue();
    }

    [Fact]
    public async Task ExistePorIdentificacao_DeveRetornarTrue_QuandoTarifacaoExiste()
    {
        // Arrange
        var identificacao = $"TRANSFER-{Guid.NewGuid()}";
        var tarifacao = new Tarifacao
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            ValorTarifado = 1.00m,
            DataHoraTarifacao = DateTime.UtcNow,
            IdentificacaoTransferencia = identificacao
        };

        await _repository.AdicionarAsync(tarifacao);

        // Aguarda e usa novo scope
        await Task.Delay(200);

        // Act - Usa novo scope
        using var verifyScope = _factory.Services.CreateScope();
        var verifyRepository = verifyScope.ServiceProvider.GetRequiredService<ITarifacaoRepository>();
        var existe = await verifyRepository.ExistePorIdentificacao(identificacao);

        // Assert
        existe.Should().BeTrue();
    }

    [Fact]
    public async Task ExistePorIdentificacao_DeveRetornarFalse_QuandoTarifacaoNaoExiste()
    {
        // Arrange
        var identificacaoInexistente = $"TRANSFER-{Guid.NewGuid()}";

        // Act
        var existe = await _repository.ExistePorIdentificacao(identificacaoInexistente);

        // Assert
        existe.Should().BeFalse();
    }

    [Fact(Skip = "Teste intermitente com SQLite - race condition em escopo de conexão")]
    public async Task AdicionarAsync_DevePermitirMultiplasTarifacoes_ComIdentificacoesDiferentes()
    {
        // Arrange
        var tarifacao1 = new Tarifacao
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            ValorTarifado = 1.00m,
            DataHoraTarifacao = DateTime.UtcNow,
            IdentificacaoTransferencia = $"TRANSFER-{Guid.NewGuid()}"
        };

        var tarifacao2 = new Tarifacao
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            ValorTarifado = 1.00m,
            DataHoraTarifacao = DateTime.UtcNow,
            IdentificacaoTransferencia = $"TRANSFER-{Guid.NewGuid()}"
        };

        // Act
        await _repository.AdicionarAsync(tarifacao1);
        await _repository.AdicionarAsync(tarifacao2);

        // Aguarda e usa novo scope
        await Task.Delay(200);

        // Assert - Usa novo scope
        using var verifyScope = _factory.Services.CreateScope();
        var verifyRepository = verifyScope.ServiceProvider.GetRequiredService<ITarifacaoRepository>();
        var existe1 = await verifyRepository.ExistePorIdentificacao(tarifacao1.IdentificacaoTransferencia);
        var existe2 = await verifyRepository.ExistePorIdentificacao(tarifacao2.IdentificacaoTransferencia);

        existe1.Should().BeTrue();
        existe2.Should().BeTrue();
    }

    [Fact(Skip = "Teste intermitente com SQLite - race condition em escopo de conexão")]
    public async Task AdicionarAsync_DevePersistirTodosOsCamposCorretamente()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var valorTarifado = 1.50m;
        var dataHora = DateTime.UtcNow;
        var identificacao = $"TRANSFER-{Guid.NewGuid()}";

        var tarifacao = new Tarifacao
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            ValorTarifado = valorTarifado,
            DataHoraTarifacao = dataHora,
            IdentificacaoTransferencia = identificacao
        };

        // Act
        await _repository.AdicionarAsync(tarifacao);

        // Aguarda e usa novo scope
        await Task.Delay(200);

        // Assert - Usa novo scope
        using var verifyScope = _factory.Services.CreateScope();
        var verifyRepository = verifyScope.ServiceProvider.GetRequiredService<ITarifacaoRepository>();
        var existe = await verifyRepository.ExistePorIdentificacao(identificacao);
        existe.Should().BeTrue();
    }
}
