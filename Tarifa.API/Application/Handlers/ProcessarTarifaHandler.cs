using Tarifa.API.Application.Commands;
using Tarifa.API.Application.Configuration;
using Tarifa.API.Domain.Entities;
using Tarifa.API.Domain.Interfaces;
using Tarifa.API.Infrastructure.Kafka;
using Tarifa.API.Infrastructure.Kafka.Events;
using MediatR;

namespace Tarifa.API.Application.Handlers;

public class ProcessarTarifaHandler : IRequestHandler<ProcessarTarifaCommand>
{
    private readonly ITarifacaoRepository _repository;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly TarifaConfiguration _tarifaConfig;
    private readonly ILogger<ProcessarTarifaHandler> _logger;

    public ProcessarTarifaHandler(
        ITarifacaoRepository repository,
        IKafkaProducer kafkaProducer,
        TarifaConfiguration tarifaConfig,
        ILogger<ProcessarTarifaHandler> logger)
    {
        _repository = repository;
        _kafkaProducer = kafkaProducer;
        _tarifaConfig = tarifaConfig;
        _logger = logger;
    }

    public async Task Handle(ProcessarTarifaCommand request, CancellationToken cancellationToken)
    {
        // 1. Check idempotency
        if (await _repository.ExistePorIdentificacao(request.IdentificacaoTransferencia))
        {
            _logger.LogInformation("Tarifa já processada (idempotência): {Id}", request.IdentificacaoTransferencia);
            return;
        }

        // 2. Get tariff value from cached configuration (Singleton)
        var valorTarifa = _tarifaConfig.ValorPorTransferencia;

        // 3. Create tarifacao entity
        var tarifacao = new Tarifacao
        {
            Id = Guid.NewGuid(),
            ContaId = request.ContaOrigemId,
            ValorTarifado = valorTarifa,
            DataHoraTarifacao = DateTime.UtcNow,
            IdentificacaoTransferencia = request.IdentificacaoTransferencia
        };

        // 4. Persist tarifacao
        await _repository.AdicionarAsync(tarifacao);

        _logger.LogInformation("Tarifa processada: {Id}, Conta: {ContaId}, Valor: {Valor}", 
            request.IdentificacaoTransferencia, request.ContaOrigemId, valorTarifa);

        // 5. Publish event to Kafka (topic fixo configurado)
        var topic = "tarifas-realizadas";

        await _kafkaProducer.PublishAsync(topic, new TarifaRealizadaEvent
        {
            TarifacaoId = tarifacao.Id.ToString(),
            ContaId = tarifacao.ContaId,
            Valor = tarifacao.ValorTarifado,
            DataHoraTarifacao = tarifacao.DataHoraTarifacao
        });

        _logger.LogInformation("Evento de tarifa publicado no Kafka: {Id}", tarifacao.Id);
    }
}
