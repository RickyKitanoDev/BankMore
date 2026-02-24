using Tarifa.API.Domain.Entities;

namespace Tarifa.API.Domain.Interfaces;

public interface ITarifacaoRepository
{
    Task<bool> ExistePorIdentificacao(string identificacaoTransferencia);
    Task AdicionarAsync(Tarifacao tarifacao);
}
