using Account.API.Application.Commands;
using Account.API.Domain.Exceptions;
using Account.API.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Account.API.Application.Handlers;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IContaRepository _repository;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<ResetPasswordHandler> _logger;

    public ResetPasswordHandler(IContaRepository repository, IPasswordHasher hasher, ILogger<ResetPasswordHandler> logger)
    {
        _repository = repository;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var row = await _repository.ObterPasswordResetAsync(request.CodeId);
        if (row is null)
            throw new BusinessException("Token inválido", "INVALID_TOKEN");

        var (contaId, tokenHash, expires, used) = row.Value;

        // log debug info for diagnosis (do not log secrets)
        _logger.LogDebug("Reset attempt for CodeId {CodeId}: Expires={Expires:u}, Used={Used}", request.CodeId, expires, used);

        if (used || expires < DateTime.UtcNow)
            throw new BusinessException("Token expirado ou já utilizado", "INVALID_TOKEN");

        using var sha = SHA256.Create();
        var computed = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(request.Code)));

        if (!string.Equals(computed, tokenHash, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("Token inválido", "INVALID_TOKEN");

        // update password
        var senha = request.NovaSenha?.Trim();
        if (string.IsNullOrEmpty(senha))
            throw new BusinessException("Senha inválida", "INVALID_PASSWORD");

        var senhaNorm = senha.Normalize(System.Text.NormalizationForm.FormC);
        var hash = _hasher.Hash(senhaNorm);

        await _repository.AtualizarSenhaAsync(Guid.Parse(contaId), hash);
        await _repository.MarcarTokenComoUsadoAsync(request.CodeId);
    }
}
