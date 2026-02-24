using Tarifa.API.Domain.Interfaces;
using Tarifa.API.Infrastructure.Persistence;
using Tarifa.API.Infrastructure.Repositories;
using Tarifa.API.Infrastructure.Kafka;
using Tarifa.API.Application.Services;
using Tarifa.API.Application.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ✅ JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(
            builder.Configuration["Jwt:Key"]!);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };

        // Customize authentication failure to return 403 Forbidden with { message, type }
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                // Skip default 401 behavior
                context.HandleResponse();

                // Return 403 Forbidden with error body
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var errorBody = new
                {
                    message = "Token inválido ou expirado",
                    type = "USER_UNAUTHORIZED"
                };

                return context.Response.WriteAsJsonAsync(errorBody);
            }
        };
    });

builder.Services.AddAuthorization();

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

// ✅ Swagger com JWT
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Tarifa API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Digite apenas o token JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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

// ✅ Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
