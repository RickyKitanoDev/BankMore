using Account.API.Application.Commands;
using Account.API.Domain.Exceptions;
using Account.API.Domain.Interfaces;
using MediatR;

namespace Account.API.Application.Handlers;

public class RealizarMovimentacaoHandler
    : IRequestHandler<RealizarMovimentacaoCommand>
{
    private readonly IContaRepository _contaRepository;
    private readonly IMovimentoRepository _movimentoRepository;

    public RealizarMovimentacaoHandler(
        IContaRepository contaRepository,
        IMovimentoRepository movimentoRepository)
    {
        _contaRepository = contaRepository;
        _movimentoRepository = movimentoRepository;
    }

    public async Task Handle(
        RealizarMovimentacaoCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate value is positive
        if (request.Valor <= 0)
            throw new BusinessException("Valor inválido", "INVALID_VALUE");

        // 2. Validate type is C or D
        if (request.Tipo != 'C' && request.Tipo != 'D')
            throw new BusinessException("Tipo inválido", "INVALID_TYPE");

        // 3. Get target account (use NumeroConta from request or from token if not provided)
        var conta = await _contaRepository
            .ObterPorNumeroAsync(request.NumeroConta!.Value);

        // 4. Validate account exists
        if (conta is null)
            throw new BusinessException("Conta inválida", "INVALID_ACCOUNT");

        // 5. Validate account is active
        if (!conta.Ativo)
            throw new BusinessException("Conta inativa", "INACTIVE_ACCOUNT");

        // 6. If target account is different from logged user, only credit is allowed
        if (request.ContaOrigemId.HasValue && conta.Id != request.ContaOrigemId.Value && request.Tipo == 'D')
            throw new BusinessException("Apenas crédito permitido para conta diferente", "INVALID_TYPE");

        // 7. Idempotency check (after all validations to ensure consistent behavior)
        if (await _movimentoRepository
            .ExistePorIdentificacao(request.IdentificacaoRequisicao))
            return; // Already processed, return success (idempotent)

        // 8. Persist movement
        await _movimentoRepository.Adicionar(
            request.IdentificacaoRequisicao,
            conta.Id,
            request.Valor,
            request.Tipo
        );
    }
}

