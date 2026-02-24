using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Account.Tests.Repositories;

/// <summary>
/// Testes do comportamento de cache do MemoryCache usado pelo CachedMovimentoRepository
/// </summary>
public class CachedMovimentoRepositoryTests
{
    private readonly IMemoryCache _cache;

    public CachedMovimentoRepositoryTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    [Fact]
    public void Cache_DeveArmazenarERecuperarSaldo()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var saldoEsperado = 1000.00m;
        var cacheKey = $"saldo:{contaId}";

        // Act - Armazena no cache
        _cache.Set(cacheKey, saldoEsperado);

        // Assert - Recupera do cache
        var saldoRecuperado = _cache.Get<decimal>(cacheKey);
        saldoRecuperado.Should().Be(saldoEsperado);
    }

    [Fact]
    public void Cache_DeveRetornarNull_QuandoChaveNaoExiste()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var cacheKey = $"saldo:{contaId}";

        // Act
        var saldo = _cache.Get<decimal?>(cacheKey);

        // Assert
        saldo.Should().BeNull();
    }

    [Fact]
    public void Cache_DeveExpirar_ComAbsoluteExpiration()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var saldoInicial = 500.00m;
        var cacheKey = $"saldo:{contaId}";

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(100)
        };

        // Act
        _cache.Set(cacheKey, saldoInicial, cacheOptions);

        // Assert - Antes de expirar
        _cache.Get<decimal>(cacheKey).Should().Be(saldoInicial);

        // Aguarda expiração
        Thread.Sleep(150);

        // Assert - Após expiração
        _cache.Get<decimal?>(cacheKey).Should().BeNull();
    }

    [Fact]
    public void Cache_DeveSuportarSlidingExpiration()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var saldo = 1000.00m;
        var cacheKey = $"saldo:{contaId}";

        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMilliseconds(200),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(500)
        };

        _cache.Set(cacheKey, saldo, cacheOptions);

        // Act - Acessa antes de 200ms (renova sliding)
        Thread.Sleep(150);
        var value1 = _cache.Get<decimal>(cacheKey);
        value1.Should().Be(saldo);

        // Aguarda mais 150ms
        Thread.Sleep(150);
        var value2 = _cache.Get<decimal>(cacheKey);
        value2.Should().Be(saldo);

        // Aguarda 250ms sem acessar (sliding expira)
        Thread.Sleep(250);
        var value3 = _cache.Get<decimal?>(cacheKey);
        value3.Should().BeNull();
    }

    [Fact]
    public void Cache_DeveInvalidarAoRemover()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var saldoInicial = 1000.00m;
        var cacheKey = $"saldo:{contaId}";

        _cache.Set(cacheKey, saldoInicial);
        _cache.Get<decimal>(cacheKey).Should().Be(saldoInicial);

        // Act - Simula invalidação após movimentação
        _cache.Remove(cacheKey);

        // Assert - Cache invalidado
        _cache.Get<decimal?>(cacheKey).Should().BeNull();
    }

    [Fact]
    public void Cache_DeveArmazenarMultiplasContas()
    {
        // Arrange
        var conta1Id = Guid.NewGuid();
        var conta2Id = Guid.NewGuid();
        var conta3Id = Guid.NewGuid();

        // Act
        _cache.Set($"saldo:{conta1Id}", 100.00m);
        _cache.Set($"saldo:{conta2Id}", 200.00m);
        _cache.Set($"saldo:{conta3Id}", 300.00m);

        // Assert
        _cache.Get<decimal>($"saldo:{conta1Id}").Should().Be(100.00m);
        _cache.Get<decimal>($"saldo:{conta2Id}").Should().Be(200.00m);
        _cache.Get<decimal>($"saldo:{conta3Id}").Should().Be(300.00m);
    }

    [Fact]
    public void Cache_DeveInvalidarApenasConta_Especifica()
    {
        // Arrange
        var conta1Id = Guid.NewGuid();
        var conta2Id = Guid.NewGuid();

        _cache.Set($"saldo:{conta1Id}", 100.00m);
        _cache.Set($"saldo:{conta2Id}", 200.00m);

        // Act - Invalida apenas conta1
        _cache.Remove($"saldo:{conta1Id}");

        // Assert
        _cache.Get<decimal?>($"saldo:{conta1Id}").Should().BeNull();
        _cache.Get<decimal>($"saldo:{conta2Id}").Should().Be(200.00m);
    }

    [Fact]
    public void Cache_DeveAtualizarValor()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var cacheKey = $"saldo:{contaId}";

        // Act
        _cache.Set(cacheKey, 100.00m);
        _cache.Get<decimal>(cacheKey).Should().Be(100.00m);

        _cache.Set(cacheKey, 200.00m);

        // Assert
        _cache.Get<decimal>(cacheKey).Should().Be(200.00m);
    }

    [Fact]
    public void Cache_DeveTerPerformanceRapida()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var cacheKey = $"saldo:{contaId}";
        _cache.Set(cacheKey, 1000.00m);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - 1000 leituras
        for (int i = 0; i < 1000; i++)
        {
            _ = _cache.Get<decimal>(cacheKey);
        }

        stopwatch.Stop();

        // Assert - Deve ser extremamente rápido (< 50ms)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50);
    }
}
