using Account.API.Domain.Entities;
using Account.API.Domain.Interfaces;
using Dapper;

namespace Account.API.Infrastructure.Repositories;

public class ContaRepository : IContaRepository
{
    private readonly IDbConnectionFactory _factory;

    public ContaRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<ContaCorrente?> ObterPorIdAsync(Guid contaId)
    {
        using var connection = _factory.CreateConnection();

        var sql = "SELECT Id, NumeroConta, Nome, Cpf, SenhaHash, Ativo FROM ContaCorrente WHERE UPPER(Id) = UPPER(@Id)";

        var row = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = contaId.ToString() });
        if (row == null) return null;

        bool ativo;
        if (row.Ativo is bool b) ativo = b;
        else if (row.Ativo is long l) ativo = l != 0;
        else if (row.Ativo is int i) ativo = i != 0;
        else ativo = Convert.ToBoolean(row.Ativo);

        return new ContaCorrente(
            Guid.Parse((string)row.Id),
            (int)row.NumeroConta,
            (string)row.Cpf,
            (string)row.Nome,
            (string)row.SenhaHash,
            ativo
        );
    }

    public async Task<(string ContaId, string TokenHash, DateTime Expires, bool Used)?> ObterPasswordResetRawAsync(string tokenId)
    {
        using var connection = _factory.CreateConnection();

        var sql = "SELECT ContaId, TokenHash, Expires, Used FROM PasswordReset WHERE Id = @Id";

        var row = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = tokenId });
        if (row == null) return null;

        return (
            (string)row.ContaId,
            (string)row.TokenHash,
            DateTime.Parse((string)row.Expires, null, System.Globalization.DateTimeStyles.RoundtripKind),
            (long)row.Used != 0
        );
    }

    public async Task<ContaCorrente?> ObterPorNumeroAsync(int numeroConta)
    {
        using var connection = _factory.CreateConnection();

        var sql = """
            SELECT Id, NumeroConta, Nome, Cpf, SenhaHash, Ativo
            FROM ContaCorrente
            WHERE NumeroConta = @NumeroConta
        """;

        var row = await connection.QueryFirstOrDefaultAsync<dynamic>(
            sql, new { NumeroConta = numeroConta });

        if (row == null)
            return null;

        bool ativo;
        if (row.Ativo is bool b) ativo = b;
        else if (row.Ativo is long l) ativo = l != 0;
        else if (row.Ativo is int i) ativo = i != 0;
        else ativo = Convert.ToBoolean(row.Ativo);

        return new ContaCorrente(
            Guid.Parse((string)row.Id),
            (int)row.NumeroConta,
            (string)row.Cpf,
            (string)row.Nome,
            (string)row.SenhaHash,
            ativo
        );
    }

    public async Task<ContaCorrente?> ObterPorCpfAsync(string cpf)
    {
        using var connection = _factory.CreateConnection();

        var sql = """
            SELECT Id, NumeroConta, Nome, Cpf, SenhaHash, Ativo
            FROM ContaCorrente
            WHERE Cpf = @Cpf
        """;

        var row = await connection.QueryFirstOrDefaultAsync<dynamic>(
            sql, new { Cpf = cpf });

        if (row == null)
            return null;

        bool ativo;
        if (row.Ativo is bool b) ativo = b;
        else if (row.Ativo is long l) ativo = l != 0;
        else if (row.Ativo is int i) ativo = i != 0;
        else ativo = Convert.ToBoolean(row.Ativo);

        return new ContaCorrente(
            Guid.Parse((string)row.Id),
            (int)row.NumeroConta,
            (string)row.Cpf,
            (string)row.Nome,
            (string)row.SenhaHash,
            ativo
        );
    }

    public async Task<ContaCorrente?> ObterPorNumeroOuCpfAsync(string numeroOuCpf)
    {
        using var connection = _factory.CreateConnection();

        var sql = """
            SELECT Id, NumeroConta, Cpf, Nome, SenhaHash, Ativo
            FROM ContaCorrente
            WHERE NumeroConta = @Numero
               OR Cpf = @NumeroOuCpf
        """;

        int.TryParse(numeroOuCpf, out var numero);

        var row = await connection.QueryFirstOrDefaultAsync<dynamic>(
            sql,
            new { Numero = numero, NumeroOuCpf = numeroOuCpf });

        if (row == null)
            return null;

        bool ativo;
        if (row.Ativo is bool b) ativo = b;
        else if (row.Ativo is long l) ativo = l != 0;
        else if (row.Ativo is int i) ativo = i != 0;
        else ativo = Convert.ToBoolean(row.Ativo);

        return new ContaCorrente(
            Guid.Parse((string)row.Id),
            (int)row.NumeroConta,
            (string)row.Cpf,
            (string)row.Nome,
            (string)row.SenhaHash,
            ativo
        );
    }

    public async Task AdicionarAsync(ContaCorrente conta)
    {
        using var connection = _factory.CreateConnection();

        var sql = @"INSERT INTO ContaCorrente
                    (Id, NumeroConta, Cpf, Nome, SenhaHash, Ativo)
                    VALUES
                    (@Id, @NumeroConta, @Cpf, @Nome, @SenhaHash, @Ativo)";

        await connection.ExecuteAsync(sql, conta);
    }

    public async Task InativarAsync(Guid contaId)
    {
        using var connection = _factory.CreateConnection();

        var sql = """
            UPDATE ContaCorrente
            SET Ativo = 0
            WHERE UPPER(Id) = UPPER(@Id)
        """;

        await connection.ExecuteAsync(sql, new
        {
            Id = contaId.ToString()
        });
    }

    public async Task<bool> ExistePorCpfAsync(string cpf)
    {
        using var connection = _factory.CreateConnection();

        var sql = """
            SELECT COUNT(1)
            FROM ContaCorrente
            WHERE Cpf = @Cpf
        """;

        var count = await connection.ExecuteScalarAsync<int>(
            sql, new { Cpf = cpf });

        return count > 0;
    }

    public async Task AdicionarPasswordResetAsync(string id, string contaId, string tokenHash, DateTime expires)
    {
        using var connection = _factory.CreateConnection();

        var sql = @"INSERT INTO PasswordReset (Id, ContaId, TokenHash, Expires, Used)
                    VALUES (@Id, @ContaId, @TokenHash, @Expires, 0)";

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            ContaId = contaId,
            TokenHash = tokenHash,
            Expires = expires.ToString("o")
        });
    }

    public async Task<(string ContaId, string TokenHash, DateTime Expires, bool Used)?> ObterPasswordResetAsync(string tokenId)
    {
        using var connection = _factory.CreateConnection();

        var sql = "SELECT ContaId, TokenHash, Expires, Used FROM PasswordReset WHERE Id = @Id AND Used = 0";

        var row = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = tokenId });
        if (row == null) return null;

        var expires = DateTime.Parse((string)row.Expires, null, System.Globalization.DateTimeStyles.RoundtripKind);
        if (expires < DateTime.UtcNow)
            return null;

        return (
            (string)row.ContaId,
            (string)row.TokenHash,
            expires,
            (long)row.Used != 0
        );
    }

    public async Task MarcarTokenComoUsadoAsync(string tokenId)
    {
        using var connection = _factory.CreateConnection();

        var sql = "UPDATE PasswordReset SET Used = 1 WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new { Id = tokenId });
    }

    public async Task AtualizarSenhaAsync(Guid contaId, string senhaHash)
    {
        using var connection = _factory.CreateConnection();

        var sql = "UPDATE ContaCorrente SET SenhaHash = @SenhaHash WHERE UPPER(Id) = UPPER(@Id)";

        await connection.ExecuteAsync(sql, new { SenhaHash = senhaHash, Id = contaId.ToString() });
    }
}