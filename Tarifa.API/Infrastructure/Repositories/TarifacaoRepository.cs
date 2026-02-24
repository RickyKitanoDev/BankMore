using Tarifa.API.Domain.Entities;
using Tarifa.API.Domain.Interfaces;
using Dapper;

namespace Tarifa.API.Infrastructure.Repositories;

public class TarifacaoRepository : ITarifacaoRepository
{
    private readonly IDbConnectionFactory _factory;

    public TarifacaoRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<bool> ExistePorIdentificacao(string identificacaoTransferencia)
    {
        using var connection = _factory.CreateConnection();

        var sql = "SELECT COUNT(1) FROM Tarifacao WHERE IdentificacaoTransferencia = @Identificacao";

        var count = await connection.ExecuteScalarAsync<int>(sql, new { Identificacao = identificacaoTransferencia });

        return count > 0;
    }

    public async Task AdicionarAsync(Tarifacao tarifacao)
    {
        using var connection = _factory.CreateConnection();

        var sql = @"INSERT INTO Tarifacao 
                    (Id, ContaId, ValorTarifado, DataHoraTarifacao, IdentificacaoTransferencia)
                    VALUES (@Id, @ContaId, @ValorTarifado, @DataHoraTarifacao, @IdentificacaoTransferencia)";

        await connection.ExecuteAsync(sql, new
        {
            Id = tarifacao.Id.ToString(),
            ContaId = tarifacao.ContaId.ToString(),
            tarifacao.ValorTarifado,
            DataHoraTarifacao = tarifacao.DataHoraTarifacao.ToString("o"),
            tarifacao.IdentificacaoTransferencia
        });
    }
}
