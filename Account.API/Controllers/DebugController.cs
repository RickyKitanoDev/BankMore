using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Account.API.Domain.Interfaces;

namespace Account.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DebugController> _logger;

    public DebugController(
        IDbConnectionFactory connectionFactory,
        ILogger<DebugController> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    [HttpGet("movimentos/{contaId}")]
    public async Task<IActionResult> GetMovimentos(Guid contaId)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT 
                Id,
                ContaCorrenteId,
                Valor,
                Tipo,
                IdentificacaoRequisicao,
                DataHora
            FROM Movimento
            WHERE ContaCorrenteId = @ContaId
            ORDER BY DataHora DESC
            LIMIT 50";

        var movimentos = await connection.QueryAsync(sql, new { ContaId = contaId.ToString() });

        var resultado = new
        {
            ContaId = contaId,
            TotalMovimentos = movimentos.Count(),
            Debitos = movimentos.Where(m => m.Tipo == 'D'),
            Creditos = movimentos.Where(m => m.Tipo == 'C'),
            TotalDebitos = movimentos.Where(m => m.Tipo == 'D').Sum(m => (decimal)m.Valor),
            TotalCreditos = movimentos.Where(m => m.Tipo == 'C').Sum(m => (decimal)m.Valor),
            SaldoCalculado = movimentos.Where(m => m.Tipo == 'C').Sum(m => (decimal)m.Valor) -
                           movimentos.Where(m => m.Tipo == 'D').Sum(m => (decimal)m.Valor),
            Movimentos = movimentos
        };

        return Ok(resultado);
    }

    [HttpGet("movimentos/buscar/{identificacao}")]
    public async Task<IActionResult> BuscarPorIdentificacao(string identificacao)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT 
                Id,
                ContaCorrenteId,
                Valor,
                Tipo,
                IdentificacaoRequisicao,
                DataHora
            FROM Movimento
            WHERE IdentificacaoRequisicao LIKE @Identificacao
            ORDER BY DataHora DESC";

        var movimentos = await connection.QueryAsync(
            sql, 
            new { Identificacao = $"%{identificacao}%" });

        return Ok(new
        {
            Identificacao = identificacao,
            Total = movimentos.Count(),
            Movimentos = movimentos
        });
    }

    [HttpGet("contas")]
    public async Task<IActionResult> GetContas()
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT 
                c.Id,
                c.NumeroConta,
                c.Cpf,
                c.Nome,
                c.Ativo,
                (SELECT COALESCE(SUM(CASE WHEN Tipo = 'C' THEN Valor ELSE 0 END), 0) -
                        COALESCE(SUM(CASE WHEN Tipo = 'D' THEN Valor ELSE 0 END), 0)
                 FROM Movimento m 
                 WHERE m.ContaCorrenteId = c.Id) as Saldo
            FROM ContaCorrente c
            ORDER BY c.NumeroConta";

        var contas = await connection.QueryAsync(sql);

        return Ok(contas);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Database = "SQLite",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        });
    }

    [HttpPost("limpar")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> LimparDados([FromQuery] string confirmacao)
    {
        if (confirmacao != "CONFIRMO_LIMPAR_TUDO")
            return BadRequest("Confirmação inválida");

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync("DELETE FROM Movimento");
        await connection.ExecuteAsync("DELETE FROM ContaCorrente");

        _logger.LogWarning("⚠️ BANCO DE DADOS LIMPO!");

        return Ok("Dados removidos com sucesso");
    }
}
