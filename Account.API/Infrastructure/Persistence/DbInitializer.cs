using Account.API.Domain.Interfaces;
using Dapper;
using System.Data;

namespace Account.API.Infrastructure.Persistence;

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

        Console.WriteLine("CurrentDirectory: " + Directory.GetCurrentDirectory());
        Console.WriteLine("Connection: " + connection.ConnectionString);

        var script = await File.ReadAllTextAsync(path);

        // Retry loop to avoid transient "database is locked" during initialization
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
