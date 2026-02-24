using Account.API.Application.DTOs;
using Account.API.Application.Queries;
using Account.API.Domain.Exceptions;
using Account.API.Domain.Interfaces;
using MediatR;

namespace Account.API.Application.Handlers;

public class ObterSaldoHandler : IRequestHandler<ObterSaldoQuery, SaldoResult>
{
    private readonly IContaRepository _contaRepository;
    private readonly IMovimentoRepository _movimentoRepository;

    public ObterSaldoHandler(
        IContaRepository contaRepository,
        IMovimentoRepository movimentoRepository)
    {
        _contaRepository = contaRepository;
        _movimentoRepository = movimentoRepository;
    }

    public async Task<SaldoResult> Handle(ObterSaldoQuery request, CancellationToken cancellationToken)
    {
        // 1. Get account by Id
        var conta = await _contaRepository.ObterPorIdAsync(request.ContaId);

        // 2. Validate account exists
        if (conta is null)
            throw new BusinessException("Conta inválida", "INVALID_ACCOUNT");

        // 3. Validate account is active
        if (!conta.Ativo)
            throw new BusinessException("Conta inativa", "INACTIVE_ACCOUNT");

        // 4. Calculate balance (sum of credits - sum of debits)
        var saldo = await _movimentoRepository.ObterSaldo(conta.Id);

        // 5. Return result with account info, current date/time and balance
        return new SaldoResult(
            NumeroConta: conta.NumeroConta,
            Nome: conta.Nome,
            DataHoraResposta: DateTime.UtcNow,
            Saldo: saldo
        );
    }
}
