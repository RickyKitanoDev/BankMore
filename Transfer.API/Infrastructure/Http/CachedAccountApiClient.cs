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

    public async Task<decimal> ObterSaldo(string contaId, string token)
    {
        var cacheKey = $"saldo:conta:{contaId}";

        try
        {
            var cachedValue = await _cache.GetStringAsync(cacheKey);
            if (cachedValue != null)
            {
                _logger.LogInformation("Cache HIT - Saldo da conta {ContaId}", contaId);
                return decimal.Parse(cachedValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar saldo do cache para conta {ContaId}", contaId);
        }

        _logger.LogInformation("Cache MISS - Buscando saldo da conta {ContaId} via HTTP", contaId);
        var saldo = await _innerClient.ObterSaldo(contaId, token);

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
            _logger.LogWarning(ex, "Erro ao cachear saldo para conta {ContaId}", contaId);
        }

        return saldo;
    }

    public async Task<bool> ValidarConta(string contaId, string token)
    {
        var cacheKey = $"conta:valida:{contaId}";

        try
        {
            var cachedValue = await _cache.GetStringAsync(cacheKey);
            if (cachedValue != null)
            {
                _logger.LogInformation("Cache HIT - Validação da conta {ContaId}", contaId);
                return bool.Parse(cachedValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar validação do cache para conta {ContaId}", contaId);
        }

        _logger.LogInformation("Cache MISS - Validando conta {ContaId} via HTTP", contaId);
        var isValid = await _innerClient.ValidarConta(contaId, token);

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
            _logger.LogWarning(ex, "Erro ao cachear validação para conta {ContaId}", contaId);
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
        if (result)
        {
            try
            {
                // Extrai ContaId do token JWT para invalidar cache correto
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var contaIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "ContaId")?.Value;

                if (!string.IsNullOrEmpty(contaIdClaim))
                {
                    var cacheKey = $"saldo:conta:{contaIdClaim}";
                    await _cache.RemoveAsync(cacheKey);
                    _logger.LogInformation("Cache INVALIDATED - Saldo da conta {ContaId} após movimentação", contaIdClaim);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao invalidar cache de saldo");
            }
        }

        return result;
    }
}
