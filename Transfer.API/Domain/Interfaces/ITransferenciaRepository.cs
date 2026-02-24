using Transfer.API.Domain.Entities;

namespace Transfer.API.Domain.Interfaces;

public interface ITransferenciaRepository
{
    Task<bool> ExistePorIdentificacao(string identificacaoRequisicao);
    Task AdicionarAsync(Transferencia transferencia);
}
