using Tarifa.API.Domain.Interfaces;
using Tarifa.API.Infrastructure.Persistence;
using Tarifa.API.Infrastructure.Repositories;
using Tarifa.API.Infrastructure.Kafka;
using Tarifa.API.Application.Services;
using Tarifa.API.Application.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddScoped<IDbConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddScoped<ITarifacaoRepository, TarifacaoRepository>();
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddScoped<DbInitializer>();

// Configuration Cache (Singleton - carregado uma vez na memória)
builder.Services.AddSingleton<TarifaConfiguration>();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Background Service (Kafka Consumer)
builder.Services.AddHostedService<TarifaConsumerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Aguarda o volume Docker estar pronto (importante no primeiro start)
await Task.Delay(2000);

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();

    // Retry logic para aguardar volume estar acessível
    var maxRetries = 5;
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await initializer.InitializeAsync();
            Console.WriteLine("Database initialized successfully");
            break;
        }
        catch (Exception ex) when (i < maxRetries - 1)
        {
            Console.WriteLine($"Failed to initialize database (attempt {i + 1}/{maxRetries}): {ex.Message}");
            await Task.Delay(3000);
        }
    }
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
