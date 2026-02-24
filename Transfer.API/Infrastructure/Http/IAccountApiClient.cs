namespace Transfer.API.Infrastructure.Http;

public interface IAccountApiClient
{
    Task<decimal> ObterSaldo(string contaId, string token);
    Task<bool> ValidarConta(string contaId, string token);
    Task<bool> RealizarMovimentacaoAsync(string token, string identificacaoRequisicao, int? numeroConta, decimal valor, char tipo);
}
