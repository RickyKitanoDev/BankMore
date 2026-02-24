
using Account.API.Application.Commands;
using Account.API.Domain.Entities;
using Account.API.Domain.Exceptions;
using Account.API.Domain.Interfaces;
using Account.API.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Account.API.Application.Handlers;

public class RegisterHandler : IRequestHandler<RegisterCommand, int>
{
    private readonly IContaRepository _repository;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<RegisterHandler> _logger;

    public RegisterHandler(
        IContaRepository repository,
        IPasswordHasher hasher,
        ILogger<RegisterHandler> logger)
    {
        _repository = repository;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task<int> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // validate CPF format first
        if (!CpfValidator.IsValid(request.Cpf))
            throw new BusinessException("CPF inválido", "INVALID_DOCUMENT");

        var existente = await _repository.ObterPorCpfAsync(request.Cpf);

        if (existente != null)
            throw new BusinessException("CPF já cadastrado", "CPF_EXISTS");

        // Normalize and validate password
        var senha = request.Senha?.Trim();
        if (string.IsNullOrEmpty(senha))
            throw new BusinessException("Senha inválida", "INVALID_PASSWORD");

        senha = senha.Normalize(NormalizationForm.FormC);

        var senhaHash = _hasher.Hash(senha);

        var conta = new ContaCorrente(
            Guid.NewGuid(),
            request.NumeroConta,
            request.Cpf,
            request.Nome,
            senhaHash,
            true
        );

        await _repository.AdicionarAsync(conta);

        // return the account number as requested by the evaluator
        return conta.NumeroConta;
    }
}
