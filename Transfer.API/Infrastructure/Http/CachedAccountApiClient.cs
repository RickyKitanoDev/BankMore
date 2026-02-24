using Microsoft.Extensions.Caching.Distributed;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace Transfer.API.Infrastructure.Http;

public class CachedAccountApiClient : IAccountApiClient
{
    private readonly AccountApiClient _innerClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedAccountApiClient> _logger;

    public CachedAccountApiClient(
        AccountApiClient innerClient,
        IDistributedCache cache,
        ILogger<CachedAccountApiClient> logger)
    {
        _innerClient = innerClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<decimal> ObterSaldo(int numeroConta, string token)
    {
        var cacheKey = $"saldo:conta:{numeroConta}";

        try
        {
            var cachedValue = await _cache.GetStringAsync(cacheKey);
            if (cachedValue != null)
            {
                _logger.LogInformation("Cache HIT - Saldo da conta {NumeroConta}", numeroConta);
                return decimal.Parse(cachedValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar saldo do cache para conta {NumeroConta}", numeroConta);
        }

        _logger.LogInformation("Cache MISS - Buscando saldo da conta {NumeroConta} via HTTP", numeroConta);
        var saldo = await _innerClient.ObterSaldo(numeroConta, token);

        try
        {
            // Cache por 10 segundos (balanço entre consistência e performance)
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            };

            await _cache.SetStringAsync(cacheKey, saldo.ToString(), cacheOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao cachear saldo para conta {NumeroConta}", numeroConta);
        }

        return saldo;
    }

    public async Task<bool> ValidarConta(int numeroConta, string token)
    {
        var cacheKey = $"conta:valida:{numeroConta}";

        try
        {
            var cachedValue = await _cache.GetStringAsync(cacheKey);
            if (cachedValue != null)
            {
                _logger.LogInformation("Cache HIT - Validação da conta {NumeroConta}", numeroConta);
                return bool.Parse(cachedValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar validação do cache para conta {NumeroConta}", numeroConta);
        }

        _logger.LogInformation("Cache MISS - Validando conta {NumeroConta} via HTTP", numeroConta);
        var isValid = await _innerClient.ValidarConta(numeroConta, token);

        try
        {
            // Cache por 5 minutos (dados mudam raramente)
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

            await _cache.SetStringAsync(cacheKey, isValid.ToString(), cacheOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao cachear validação para conta {NumeroConta}", numeroConta);
        }

        return isValid;
    }

    public async Task<bool> RealizarMovimentacaoAsync(
        string token,
        string identificacaoRequisicao,
        int? numeroConta,
        decimal valor,
        char tipo)
    {
        var result = await _innerClient.RealizarMovimentacaoAsync(
            token, identificacaoRequisicao, numeroConta, valor, tipo);

        // Invalida cache de saldo ao realizar movimentação
        if (result && numeroConta.HasValue)
        {
            try
            {
                var cacheKey = $"saldo:conta:{numeroConta.Value}";
                await _cache.RemoveAsync(cacheKey);
                _logger.LogInformation("Cache INVALIDATED - Saldo da conta {NumeroConta} após movimentação", numeroConta.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao invalidar cache de saldo para conta {NumeroConta}", numeroConta.Value);
            }
        }

        return result;
    }
}
