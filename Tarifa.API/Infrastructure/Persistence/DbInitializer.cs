using Tarifa.API.Domain.Interfaces;
using Dapper;

namespace Tarifa.API.Infrastructure.Persistence;

public class DbInitializer
{
    private readonly IDbConnectionFactory _factory;

    public DbInitializer(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        using var connection = _factory.CreateConnection();

        var path = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Infrastructure",
            "Persistence",
            "scripts.sql");

        var script = await File.ReadAllTextAsync(path);

        const int maxAttempts = 5;
        var attempt = 0;
        while (true)
        {
            try
            {
                await connection.ExecuteAsync(script);
                break;
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 5)
            {
                attempt++;
                if (attempt >= maxAttempts)
                    throw;

                var delayMs = 200 * attempt;
                Console.WriteLine($"Database is locked during initialization, retrying in {delayMs}ms (attempt {attempt})");
                await Task.Delay(delayMs);
            }
        }
    }
}
