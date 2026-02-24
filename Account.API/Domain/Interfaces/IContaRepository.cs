using Account.API.Domain.Entities;

namespace Account.API.Domain.Interfaces;

public interface IContaRepository
{
    Task<ContaCorrente?> ObterPorNumeroAsync(int numeroConta);
    Task<ContaCorrente?> ObterPorCpfAsync(string cpf);
    Task<ContaCorrente?> ObterPorNumeroOuCpfAsync(string numeroOuCpf);
    Task AdicionarAsync(ContaCorrente conta);

    Task InativarAsync(Guid contaId);
    Task<ContaCorrente?> ObterPorIdAsync(Guid contaId);
    Task<bool> ExistePorCpfAsync(string cpf);

    // Password reset helpers
    Task AdicionarPasswordResetAsync(string id, string contaId, string tokenHash, DateTime expires);
    Task<(string ContaId, string TokenHash, DateTime Expires, bool Used)?> ObterPasswordResetAsync(string tokenId);
    Task<(string ContaId, string TokenHash, DateTime Expires, bool Used)?> ObterPasswordResetRawAsync(string tokenId);
    Task MarcarTokenComoUsadoAsync(string tokenId);

    // Update password
    Task AtualizarSenhaAsync(Guid contaId, string senhaHash);
}
