using Transfer.API.Domain.Entities;
using Transfer.API.Domain.Interfaces;
using Dapper;

namespace Transfer.API.Infrastructure.Repositories;

public class TransferenciaRepository : ITransferenciaRepository
{
    private readonly IDbConnectionFactory _factory;

    public TransferenciaRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<bool> ExistePorIdentificacao(string identificacaoRequisicao)
    {
        using var connection = _factory.CreateConnection();

        var sql = "SELECT COUNT(1) FROM Transferencia WHERE IdentificacaoRequisicao = @Identificacao";

        var count = await connection.ExecuteScalarAsync<int>(sql, new { Identificacao = identificacaoRequisicao });

        return count > 0;
    }

    public async Task AdicionarAsync(Transferencia transferencia)
    {
        using var connection = _factory.CreateConnection();

        var sql = @"INSERT INTO Transferencia 
                    (Id, ContaOrigemId, ContaDestinoId, Valor, DataTransferencia, IdentificacaoRequisicao, Status)
                    VALUES (@Id, @ContaOrigemId, @ContaDestinoId, @Valor, @DataTransferencia, @IdentificacaoRequisicao, @Status)";

        await connection.ExecuteAsync(sql, new
        {
            Id = transferencia.Id.ToString(),
            ContaOrigemId = transferencia.ContaOrigemId.ToString(),
            ContaDestinoId = transferencia.ContaDestinoId.ToString(),
            transferencia.Valor,
            DataTransferencia = transferencia.DataTransferencia.ToString("o"),
            transferencia.IdentificacaoRequisicao,
            transferencia.Status
        });
    }
}
