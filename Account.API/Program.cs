using Account.API.Domain.Interfaces;
using Account.API.Infrastructure.Authentication;
using Account.API.Infrastructure.Persistence;
using Account.API.Infrastructure.Repositories;
using Account.API.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(
            builder.Configuration["Jwt:Key"]!);

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey =
                    new SymmetricSecurityKey(key)
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

// Memory Cache para otimização de consultas de saldo
builder.Services.AddMemoryCache();

// Add services to the container.
builder.Services.AddScoped<IDbConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddScoped<IContaRepository, ContaRepository>();

// MovimentoRepository com cache decorator pattern
builder.Services.AddScoped<MovimentoRepository>();
builder.Services.AddScoped<IMovimentoRepository, CachedMovimentoRepository>();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<DbInitializer>();

// Kafka Consumer Service (Background Service)
builder.Services.AddHostedService<Account.API.Infrastructure.Kafka.Services.TarifaConsumerService>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Account API",
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

builder.Services.AddControllers()
    .AddJsonOptions(opts => { });

// Convert FluentValidation failures to uniform error response
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var first = context.ModelState
            .Where(kv => kv.Value.Errors.Count > 0)
            .Select(kv => kv.Value.Errors.First().ErrorMessage)
            .FirstOrDefault() ?? "Invalid payload";

        // Map common messages to evaluator error types
        var type = first.Contains("CPF") ? "INVALID_DOCUMENT" : "INVALID_PAYLOAD";

        var problem = new { message = first, type };
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(problem);
    };
});

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));



var app = builder.Build();

// global exception handling for BusinessException -> HTTP responses
app.UseMiddleware<Account.API.Application.Middleware.ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();


// Aguarda o volume Docker estar pronto (importante no primeiro start)
await Task.Delay(2000);

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider
        .GetRequiredService<DbInitializer>();

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

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
