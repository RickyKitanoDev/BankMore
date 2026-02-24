using Account.API.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Account.API.Infrastructure.Repositories;

public class CachedMovimentoRepository : IMovimentoRepository
{
    private readonly IMovimentoRepository _innerRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedMovimentoRepository> _logger;

    public CachedMovimentoRepository(
        MovimentoRepository innerRepository,
        IMemoryCache cache,
        ILogger<CachedMovimentoRepository> logger)
    {
        _innerRepository = innerRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<decimal> ObterSaldo(Guid contaCorrenteId)
    {
        var cacheKey = $"saldo:{contaCorrenteId}";

        if (_cache.TryGetValue(cacheKey, out decimal saldo))
        {
            _logger.LogInformation("Cache HIT - Saldo da conta {ContaId}", contaCorrenteId);
            return saldo;
        }

        _logger.LogInformation("Cache MISS - Buscando saldo da conta {ContaId} no banco", contaCorrenteId);
        saldo = await _innerRepository.ObterSaldo(contaCorrenteId);

        // Cache por 30 segundos com sliding expiration de 10s
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
            SlidingExpiration = TimeSpan.FromSeconds(10)
        };

        _cache.Set(cacheKey, saldo, cacheOptions);

        return saldo;
    }

    public async Task Adicionar(string identificacao, Guid contaCorrenteId, decimal valor, char tipo)
    {
        await _innerRepository.Adicionar(identificacao, contaCorrenteId, valor, tipo);

        // Invalida cache do saldo ao adicionar movimento
        var cacheKey = $"saldo:{contaCorrenteId}";
        _cache.Remove(cacheKey);
        _logger.LogInformation("Cache INVALIDATED - Saldo da conta {ContaId} após movimento", contaCorrenteId);
    }

    public async Task<bool> ExistePorIdentificacao(string identificacao)
    {
        return await _innerRepository.ExistePorIdentificacao(identificacao);
    }
}
