namespace Account.API.Domain.Interfaces;

public interface IMovimentoRepository
{
    Task<bool> ExistePorIdentificacao(string identificacaoRequisicao);

    Task Adicionar(
        string identificacaoRequisicao,
        Guid contaCorrenteId,
        decimal valor,
        char tipo);

    Task<decimal> ObterSaldo(Guid contaCorrenteId);
}
