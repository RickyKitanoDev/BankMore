using Confluent.Kafka;
using Account.API.Domain.Interfaces;
using Account.API.Infrastructure.Kafka.Events;
using System.Text.Json;

namespace Account.API.Infrastructure.Kafka.Services;

public class TarifaConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TarifaConsumerService> _logger;
    private IConsumer<string, string>? _consumer;

    public TarifaConsumerService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<TarifaConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Aguarda 10 segundos para o Kafka estar pronto
        await Task.Delay(10000, stoppingToken);

        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = _configuration["Kafka:GroupId"] ?? "account-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true,
            SessionTimeoutMs = 45000,
            HeartbeatIntervalMs = 15000
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();

        var topic = _configuration["Kafka:Topics:TarifasRealizadas"] ?? "tarifas-realizadas";
        _consumer.Subscribe(topic);

        _logger.LogInformation("Kafka Consumer iniciado - Topic: {Topic}", topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);

                    if (consumeResult?.Message?.Value != null)
                    {
                        _logger.LogInformation("Mensagem de tarifa recebida do Kafka - Offset: {Offset}", consumeResult.Offset.Value);

                        var tarifa = JsonSerializer.Deserialize<TarifaRealizadaEvent>(consumeResult.Message.Value);

                        if (tarifa != null)
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var movimentoRepository = scope.ServiceProvider.GetRequiredService<IMovimentoRepository>();

                            // Debitar tarifa da conta
                            await movimentoRepository.Adicionar(
                                $"tarifa-{tarifa.TarifacaoId}",
                                tarifa.ContaId,
                                tarifa.Valor,
                                'D' // Débito
                            );

                            _consumer.Commit(consumeResult);
                            _logger.LogInformation("Tarifa debitada com sucesso: Conta {ContaId}, Valor {Valor}", 
                                tarifa.ContaId, tarifa.Valor);
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning(ex, "Erro ao consumir mensagem do Kafka - Aguardando 5s antes de retry");
                    await Task.Delay(5000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar tarifa");
                }
            }
        }
        finally
        {
            _consumer?.Close();
            _consumer?.Dispose();
        }
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}
