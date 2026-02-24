using Account.API.Domain.Interfaces;
using Dapper;

namespace Account.API.Infrastructure.Repositories;

public class MovimentoRepository : IMovimentoRepository
{
    private readonly IDbConnectionFactory _factory;

    public MovimentoRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<bool> ExistePorIdentificacao(string identificacaoRequisicao)
    {
        using var connection = _factory.CreateConnection();

        var sql = """
            SELECT COUNT(1)
            FROM Movimento
            WHERE IdentificacaoRequisicao = @Identificacao
        """;

        var count = await connection.ExecuteScalarAsync<int>(
            sql,
            new { Identificacao = identificacaoRequisicao });

        return count > 0;
    }

    public async Task Adicionar(
        string identificacaoRequisicao,
        Guid contaCorrenteId,
        decimal valor,
        char tipo)
    {
        using var connection = _factory.CreateConnection();

        // Get the exact Id format from the database to match FK constraint
        var getIdSql = "SELECT Id FROM ContaCorrente WHERE UPPER(Id) = UPPER(@Id)";
        var exactId = await connection.QueryFirstOrDefaultAsync<string>(getIdSql, new { Id = contaCorrenteId.ToString() });

        // This should never happen as we validate in the handler, but add safeguard
        if (string.IsNullOrEmpty(exactId))
            throw new InvalidOperationException($"Conta {contaCorrenteId} não encontrada - validação falhou");

        var sql = """
            INSERT INTO Movimento
            (Id, ContaCorrenteId, IdentificacaoRequisicao, Valor, Tipo, DataMovimento)
            VALUES (@Id, @ContaCorrenteId, @IdentificacaoRequisicao, @Valor, @Tipo, @Data)
        """;

        await connection.ExecuteAsync(sql, new
        {
            Id = Guid.NewGuid().ToString(),
            ContaCorrenteId = exactId, // Use the exact format from DB
            IdentificacaoRequisicao = identificacaoRequisicao,
            Valor = valor,
            Tipo = tipo.ToString(),
            Data = DateTime.UtcNow.ToString("o")
        });
    }

    public async Task<decimal> ObterSaldo(Guid contaCorrenteId)
    {
        using var connection = _factory.CreateConnection();

        var sql = """
            SELECT 
                COALESCE(SUM(
                    CASE 
                        WHEN Tipo = 'C' THEN Valor
                        WHEN Tipo = 'D' THEN -Valor
                        ELSE 0
                    END
                ), 0)
            FROM Movimento
            WHERE UPPER(ContaCorrenteId) = UPPER(@ContaId)
        """;

        var saldo = await connection.ExecuteScalarAsync<decimal>(
            sql,
            new { ContaId = contaCorrenteId.ToString() });

        return saldo;
    }
}
