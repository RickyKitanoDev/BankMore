using MediatR;
using Account.API.Domain.Exceptions;
using Account.API.Infrastructure.Security;
using Account.API.Application.Commands;
using Account.API.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Account.API.Application.Handlers;

public class LoginHandler : IRequestHandler<LoginCommand, string>
{
    private readonly IContaRepository _repository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(
        IContaRepository repository,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        ILogger<LoginHandler> logger)
    {
        _repository = repository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<string> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var conta = await _repository
            .ObterPorNumeroOuCpfAsync(request.NumeroOuCpf);

        if (conta is null)
            throw new BusinessException(
                "Usuário não encontrado",
                "USER_UNAUTHORIZED");

        // Normalize incoming password before verify
        var senha = request.Senha?.Trim();
        senha = senha?.Normalize(NormalizationForm.FormC);

        if (!_passwordHasher.Verify(senha ?? string.Empty, conta.SenhaHash))
        {
            _logger.LogWarning("Falha de autenticação para conta {AccountId}", conta.Id);
            throw new BusinessException(
                "Senha inválida",
                "USER_UNAUTHORIZED");
        }

        if (!conta.Ativo)
            throw new BusinessException(
                "Conta inativa",
                "INACTIVE_ACCOUNT");

        return _jwtService.GenerateToken(
            conta.Id,
            conta.NumeroConta);
    }
}
