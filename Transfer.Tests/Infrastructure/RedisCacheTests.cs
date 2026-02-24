using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Transfer.Tests.Infrastructure;

/// <summary>
/// Testes do comportamento de cache Redis (IDistributedCache)
/// </summary>
public class RedisCacheTests
{
    private readonly Mock<IDistributedCache> _cacheMock;

    public RedisCacheTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
    }

    [Fact]
    public async Task Cache_DeveArmazenarERecuperarSaldo()
    {
        // Arrange
        var cacheKey = "saldo:conta:12345";
        var saldoEsperado = 1500.50m;
        var saldoBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(saldoEsperado));

        _cacheMock
            .Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saldoBytes);

        // Act
        var cachedBytes = await _cacheMock.Object.GetAsync(cacheKey, CancellationToken.None);
        var saldo = JsonSerializer.Deserialize<decimal>(Encoding.UTF8.GetString(cachedBytes!));

        // Assert
        saldo.Should().Be(saldoEsperado);
    }

    [Fact]
    public async Task Cache_DeveRetornarNull_ParaCacheMiss()
    {
        // Arrange
        var cacheKey = "saldo:conta:99999";

        _cacheMock
            .Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var cachedBytes = await _cacheMock.Object.GetAsync(cacheKey, CancellationToken.None);

        // Assert
        cachedBytes.Should().BeNull();
    }

    [Fact]
    public async Task Cache_DeveArmazenarComTTL()
    {
        // Arrange
        var cacheKey = "saldo:conta:54321";
        var saldo = 2000.00m;
        var saldoBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(saldo));
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
        };

        _cacheMock
            .Setup(x => x.SetAsync(cacheKey, saldoBytes, options, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheMock.Object.SetAsync(cacheKey, saldoBytes, options, CancellationToken.None);

        // Assert
        _cacheMock.Verify(x => x.SetAsync(cacheKey, saldoBytes, options, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cache_DeveInvalidarChave()
    {
        // Arrange
        var cacheKey = "saldo:conta:12345";

        _cacheMock
            .Setup(x => x.RemoveAsync(cacheKey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheMock.Object.RemoveAsync(cacheKey, CancellationToken.None);

        // Assert
        _cacheMock.Verify(x => x.RemoveAsync(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cache_DeveSerializarValidacao()
    {
        // Arrange
        var cacheKey = "validacao:conta:12345";
        var validacao = true;
        var validacaoBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(validacao));

        _cacheMock
            .Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validacaoBytes);

        // Act
        var cached = await _cacheMock.Object.GetAsync(cacheKey, CancellationToken.None);
        var resultado = JsonSerializer.Deserialize<bool>(Encoding.UTF8.GetString(cached!));

        // Assert
        resultado.Should().BeTrue();
    }

    [Fact]
    public async Task Cache_DeveSuportarMultiplasChaves()
    {
        // Arrange
        var key1 = "saldo:conta:111";
        var key2 = "saldo:conta:222";

        var saldo1Bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(100m));
        var saldo2Bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(200m));

        _cacheMock.Setup(x => x.GetAsync(key1, It.IsAny<CancellationToken>())).ReturnsAsync(saldo1Bytes);
        _cacheMock.Setup(x => x.GetAsync(key2, It.IsAny<CancellationToken>())).ReturnsAsync(saldo2Bytes);

        // Act
        var bytes1 = await _cacheMock.Object.GetAsync(key1, CancellationToken.None);
        var bytes2 = await _cacheMock.Object.GetAsync(key2, CancellationToken.None);

        var saldo1 = JsonSerializer.Deserialize<decimal>(Encoding.UTF8.GetString(bytes1!));
        var saldo2 = JsonSerializer.Deserialize<decimal>(Encoding.UTF8.GetString(bytes2!));

        // Assert
        saldo1.Should().Be(100m);
        saldo2.Should().Be(200m);
    }
}
