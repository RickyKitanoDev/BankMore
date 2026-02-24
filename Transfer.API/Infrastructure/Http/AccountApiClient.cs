using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Transfer.API.Infrastructure.Http;

public class AccountApiClient : IAccountApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AccountApiClient> _logger;

    public AccountApiClient(HttpClient httpClient, ILogger<AccountApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> RealizarMovimentacaoAsync(
        string token, 
        string identificacaoRequisicao, 
        int? numeroConta, 
        decimal valor, 
        char tipo)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/movimentacao");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                identificacaoRequisicao,
                contaId = numeroConta, // Campo opcional - se null, Account.API usa do token
                valor,
                tipo
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Movimentação realizada com sucesso: {Id}, Tipo: {Tipo}, Valor: {Valor}, Conta: {Conta}", 
                    identificacaoRequisicao, tipo, valor, numeroConta?.ToString() ?? "token");
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Falha na movimentação: {StatusCode}, {Error}", 
                response.StatusCode, errorContent);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao chamar Account API para movimentação {Id}", identificacaoRequisicao);
            return false;
        }
    }

    public async Task<decimal> ObterSaldo(int numeroConta, string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/movimentacao/saldo");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SaldoResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Saldo ?? 0m;
            }

            _logger.LogWarning("Falha ao obter saldo: {StatusCode}", response.StatusCode);
            return 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter saldo da conta {NumeroConta}", numeroConta);
            return 0m;
        }
    }

    public async Task<bool> ValidarConta(int numeroConta, string token)
    {
        try
        {
            // Para validar a conta, tentamos fazer uma chamada ao endpoint de saldo
            // Se retornar sucesso, a conta existe e está ativa
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/movimentacao/saldo");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar conta {NumeroConta}", numeroConta);
            return false;
        }
    }

    private class SaldoResponse
    {
        public decimal Saldo { get; set; }
    }
}
