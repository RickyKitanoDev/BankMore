using Account.API.Application.Commands;
using Account.API.Domain.Interfaces;
using MediatR;
using System.Security.Cryptography;
using System.Text;

namespace Account.API.Application.Handlers;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private readonly IContaRepository _repository;

    public ForgotPasswordHandler(IContaRepository repository)
    {
        _repository = repository;
    }

    public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var conta = await _repository.ObterPorCpfAsync(request.Cpf);
        if (conta is null)
            throw new Exception("Conta não encontrada");

        // generate token id and a short 6-digit numeric code (for easier user input)
        var tokenId = Guid.NewGuid().ToString();

        // secure random 6-digit code
        var number = RandomNumberGenerator.GetInt32(0, 1_000_000);
        var code = number.ToString("D6");

        // hash code with SHA256 before storing
        using var sha = SHA256.Create();
        var tokenHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(code)));

        var expires = DateTime.UtcNow.AddHours(1);

        await _repository.AdicionarPasswordResetAsync(tokenId, conta.Id.ToString(), tokenHash, expires);

        // return code id and code (in real app send code via email/SMS) — here returned to caller for testing
        return new ForgotPasswordResult(tokenId, code);
    }
}
