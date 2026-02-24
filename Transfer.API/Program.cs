using Transfer.API.Domain.Interfaces;
using Transfer.API.Infrastructure.Persistence;
using Transfer.API.Infrastructure.Repositories;
using Transfer.API.Infrastructure.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);

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

        // Return 403 on authentication failure
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
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

// Distributed Cache: Redis se disponível, caso contrário Memory Cache
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "BankMore:";
    });
}
else
{
    // Fallback para cache em memória em ambientes sem Redis
    builder.Services.AddDistributedMemoryCache();
}

// Add services to the container
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDbConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddScoped<ITransferenciaRepository, TransferenciaRepository>();
builder.Services.AddSingleton<Transfer.API.Infrastructure.Kafka.IKafkaProducer, Transfer.API.Infrastructure.Kafka.KafkaProducer>();
builder.Services.AddScoped<DbInitializer>();

// HttpClient for Account.API with Cache Decorator Pattern
builder.Services.AddHttpClient<AccountApiClient>(client =>
{
    var accountApiUrl = builder.Configuration["AccountApi:BaseUrl"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(accountApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register CachedAccountApiClient as IAccountApiClient with AccountApiClient as dependency
builder.Services.AddScoped<IAccountApiClient>(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(nameof(AccountApiClient));
    var logger = serviceProvider.GetRequiredService<ILogger<AccountApiClient>>();
    var cache = serviceProvider.GetRequiredService<IDistributedCache>();
    var cachedLogger = serviceProvider.GetRequiredService<ILogger<CachedAccountApiClient>>();

    var innerClient = new AccountApiClient(httpClient, logger);
    return new CachedAccountApiClient(innerClient, cache, cachedLogger);
});

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddControllers()
    .AddJsonOptions(opts => { });

// Convert model validation failures to uniform error response
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var first = context.ModelState
            .Where(kv => kv.Value.Errors.Count > 0)
            .Select(kv => kv.Value.Errors.First().ErrorMessage)
            .FirstOrDefault() ?? "Invalid payload";

        var problem = new { message = first, type = "INVALID_PAYLOAD" };
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(problem);
    };
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Transfer API",
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

// Global exception handling
app.UseMiddleware<Transfer.API.Application.Middleware.ExceptionHandlingMiddleware>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
