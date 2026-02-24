using Confluent.Kafka;
using Tarifa.API.Application.Commands;
using Tarifa.API.Infrastructure.Kafka.Events;
using MediatR;
using System.Text.Json;

namespace Tarifa.API.Application.Services;

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
            GroupId = _configuration["Kafka:GroupId"] ?? "tarifa-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true,
            SessionTimeoutMs = 45000,
            HeartbeatIntervalMs = 15000
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();

        var topic = _configuration["Kafka:Topics:TransferenciasRealizadas"] ?? "transferencias-realizadas";
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
                        _logger.LogInformation("Mensagem recebida do Kafka - Offset: {Offset}", consumeResult.Offset.Value);

                        var transferencia = JsonSerializer.Deserialize<TransferenciaRealizadaEvent>(consumeResult.Message.Value);

                        if (transferencia != null)
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                            await mediator.Send(new ProcessarTarifaCommand(
                                transferencia.IdentificacaoRequisicao,
                                transferencia.ContaOrigemId,
                                transferencia.Valor
                            ), stoppingToken);

                            _consumer.Commit(consumeResult);
                            _logger.LogInformation("Mensagem processada e commitada");
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
                    _logger.LogError(ex, "Erro ao processar mensagem");
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
