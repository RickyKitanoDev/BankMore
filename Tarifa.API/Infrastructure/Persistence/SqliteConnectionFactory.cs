using Tarifa.API.Domain.Interfaces;
using Microsoft.Data.Sqlite;
using System.Data;

namespace Tarifa.API.Infrastructure.Persistence;

public class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SqliteConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection()
    {
        var cs = _configuration.GetConnectionString("Default") ?? "Data Source=./data/tarifacao.db";

        // Extract database path
        var match = System.Text.RegularExpressions.Regex.Match(cs, @"Data Source=([^;]+)");
        if (match.Success)
        {
            var dbPath = match.Groups[1].Value;
            var dataDir = Path.GetDirectoryName(dbPath);

            // Ensure data directory exists with proper permissions
            if (!string.IsNullOrEmpty(dataDir) && !Directory.Exists(dataDir))
            {
                try
                {
                    Directory.CreateDirectory(dataDir);
                    Console.WriteLine($"Created directory: {dataDir}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create directory {dataDir}: {ex.Message}");
                    throw;
                }
            }
        }

        if (!cs.Contains("Cache=", StringComparison.OrdinalIgnoreCase))
            cs = cs.TrimEnd(';') + ";Cache=Shared";

        var connection = new SqliteConnection(cs);
        connection.Open();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA journal_mode = WAL;";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "PRAGMA busy_timeout = 15000;";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "PRAGMA synchronous = NORMAL;";
            cmd.ExecuteNonQuery();
        }

        return connection;
    }
}
