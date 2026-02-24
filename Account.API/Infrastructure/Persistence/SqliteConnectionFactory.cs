using Account.API.Domain.Interfaces;
using Microsoft.Data.Sqlite;
using System.Data;

namespace Account.API.Infrastructure.Persistence;

public class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SqliteConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection()
    {
        var cs = _configuration.GetConnectionString("Default") ?? "Data Source=./data/bankmore.db";

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

        // Ensure shared cache to allow multiple connections in same process and docker
        if (!cs.Contains("Cache=", StringComparison.OrdinalIgnoreCase))
            cs = cs.TrimEnd(';') + ";Cache=Shared";

        var connection = new SqliteConnection(cs);

        // Open and configure pragmas to reduce locking in concurrent scenarios
        connection.Open();

        using (var cmd = connection.CreateCommand())
        {
            // Use WAL mode for better concurrency
            cmd.CommandText = "PRAGMA journal_mode = WAL;";
            cmd.ExecuteNonQuery();

            // Set a longer busy timeout (15 seconds) to handle concurrent access
            cmd.CommandText = "PRAGMA busy_timeout = 15000;";
            cmd.ExecuteNonQuery();

            // Enable foreign keys
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();

            // Optimize for concurrent access
            cmd.CommandText = "PRAGMA synchronous = NORMAL;";
            cmd.ExecuteNonQuery();
        }

        return connection;
    }
}

