using Account.API.Application.Commands;
using Account.API.Domain.Exceptions;
using Account.API.Domain.Interfaces;
using MediatR;

namespace Account.API.Application.Handlers;

public class InativarContaHandler : IRequestHandler<InativarContaCommand>
{
    private readonly IContaRepository _repository;
    private readonly IPasswordHasher _hasher;

    public InativarContaHandler(IContaRepository repository, IPasswordHasher hasher)
    {
        _repository = repository;
        _hasher = hasher;
    }

    public async Task Handle(InativarContaCommand request, CancellationToken cancellationToken)
    {
        var conta = await _repository.ObterPorIdAsync(request.ContaId);

        if (conta is null)
            throw new BusinessException("Conta inválida", "INVALID_ACCOUNT");

        if (!_hasher.Verify(request.Senha, conta.SenhaHash))
            throw new BusinessException("Senha inválida", "USER_UNAUTHORIZED");

        await _repository.InativarAsync(request.ContaId);
    }
}
