using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tarifa.API.Domain.Interfaces;
using Tarifa.API.Infrastructure.Persistence;

namespace Tarifa.Tests.Integration.Persistence;

public class DbInitializerIntegrationTests : IClassFixture<TarifaWebApplicationFactory>
{
    private readonly TarifaWebApplicationFactory _factory;

    public DbInitializerIntegrationTests(TarifaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InitializeAsync_DeveCriarTabelaTarifacao()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

        // Act
        await initializer.InitializeAsync();

        // Assert
        using var connection = connectionFactory.CreateConnection();
        var sql = @"SELECT name FROM sqlite_master 
                    WHERE type='table' AND name='Tarifacao'";
        
        var tableName = await Dapper.SqlMapper.QuerySingleOrDefaultAsync<string>(connection, sql);
        tableName.Should().Be("Tarifacao");
    }

    [Fact]
    public async Task InitializeAsync_DeveCriarIndiceIdentificacaoTransferencia()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

        // Act
        await initializer.InitializeAsync();

        // Assert
        using var connection = connectionFactory.CreateConnection();
        var sql = @"SELECT name FROM sqlite_master 
                    WHERE type='index' AND tbl_name='Tarifacao' 
                    AND name='UX_Tarifacao_Identificacao'";

        var indexName = await Dapper.SqlMapper.QuerySingleOrDefaultAsync<string>(connection, sql);
        indexName.Should().Be("UX_Tarifacao_Identificacao");
    }

    [Fact]
    public async Task InitializeAsync_DeveSerIdempotente()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();

        // Act - Executar múltiplas vezes
        await initializer.InitializeAsync();
        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        // Assert - Não deve lançar exceção
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using var connection = connectionFactory.CreateConnection();
        var sql = @"SELECT name FROM sqlite_master 
                    WHERE type='table' AND name='Tarifacao'";
        
        var tableName = await Dapper.SqlMapper.QuerySingleOrDefaultAsync<string>(connection, sql);
        tableName.Should().Be("Tarifacao");
    }

    [Fact]
    public async Task InitializeAsync_DeveCriarTabelaComColunaCorreta()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

        // Act
        await initializer.InitializeAsync();

        // Assert
        using var connection = connectionFactory.CreateConnection();
        var sql = "PRAGMA table_info(Tarifacao)";
        
        var columns = await Dapper.SqlMapper.QueryAsync<dynamic>(connection, sql);
        var columnNames = columns.Select(c => (string)c.name).ToList();

        columnNames.Should().Contain("Id");
        columnNames.Should().Contain("ContaId");
        columnNames.Should().Contain("ValorTarifado");
        columnNames.Should().Contain("DataHoraTarifacao");
        columnNames.Should().Contain("IdentificacaoTransferencia");
    }
}
