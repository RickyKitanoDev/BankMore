namespace Transfer.API.Infrastructure.Http;

public interface IAccountApiClient
{
    Task<decimal> ObterSaldo(int numeroConta, string token);
    Task<bool> ValidarConta(int numeroConta, string token);
    Task<bool> RealizarMovimentacaoAsync(string token, string identificacaoRequisicao, int? numeroConta, decimal valor, char tipo);
}
