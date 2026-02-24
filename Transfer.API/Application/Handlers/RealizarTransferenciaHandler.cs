using Transfer.API.Application.Commands;
using Transfer.API.Domain.Entities;
using Transfer.API.Domain.Exceptions;
using Transfer.API.Domain.Interfaces;
using Transfer.API.Infrastructure.Http;
using Transfer.API.Infrastructure.Kafka;
using Transfer.API.Infrastructure.Kafka.Events;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Transfer.API.Application.Handlers;

public class RealizarTransferenciaHandler : IRequestHandler<RealizarTransferenciaCommand>
{
    private readonly ITransferenciaRepository _repository;
    private readonly IAccountApiClient _accountApi;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<RealizarTransferenciaHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public RealizarTransferenciaHandler(
        ITransferenciaRepository repository,
        IAccountApiClient accountApi,
        IKafkaProducer kafkaProducer,
        ILogger<RealizarTransferenciaHandler> logger,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _repository = repository;
        _accountApi = accountApi;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public async Task Handle(RealizarTransferenciaCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate value is positive
        if (request.Valor <= 0)
            throw new BusinessException("Valor inválido", "INVALID_VALUE");

        // 2. Validate required data from token
        if (!request.ContaOrigemId.HasValue)
            throw new BusinessException("Conta de origem inválida", "INVALID_ACCOUNT");

        // 3. Idempotency check
        if (await _repository.ExistePorIdentificacao(request.IdentificacaoRequisicao))
        {
            _logger.LogInformation("Transferência já processada (idempotência): {Id}", request.IdentificacaoRequisicao);
            return;
        }

        // Get token from request header
        var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"]
            .ToString().Replace("Bearer ", "") ?? string.Empty;

        if (string.IsNullOrEmpty(token))
            throw new BusinessException("Token não encontrado", "USER_UNAUTHORIZED");

        // 4. Generate unique IDs for debit and credit operations
        var debitoId = $"{request.IdentificacaoRequisicao}-DEBIT";
        var creditoId = $"{request.IdentificacaoRequisicao}-CREDIT";

        try
        {
            // 5. Perform DEBIT on origin account (logged user account)
            _logger.LogInformation("Iniciando débito na conta origem: {ContaId}, Valor: {Valor}", 
                request.ContaOrigemId, request.Valor);

            var debitoSuccess = await _accountApi.RealizarMovimentacaoAsync(
                token,
                debitoId,
                null, // Use account from token
                request.Valor,
                'D'); // Debit

            if (!debitoSuccess)
                throw new BusinessException("Falha ao realizar débito na conta de origem", "DEBIT_FAILED");

            // 6. Perform CREDIT on destination account
            _logger.LogInformation("Iniciando crédito na conta destino: {ContaDestino}, Valor: {Valor}", 
                request.ContaDestinoNumero, request.Valor);

            var creditoSuccess = await _accountApi.RealizarMovimentacaoAsync(
                token,
                creditoId,
                request.ContaDestinoNumero,
                request.Valor,
                'C'); // Credit

            if (!creditoSuccess)
            {
                // 7. ROLLBACK: Credit failed, reverse debit (credit back to origin)
                _logger.LogWarning("Falha no crédito, iniciando estorno para conta origem");

                var estornoId = $"{request.IdentificacaoRequisicao}-REVERSAL";
                var estornoSuccess = await _accountApi.RealizarMovimentacaoAsync(
                    token,
                    estornoId,
                    null, // Origin account from token
                    request.Valor,
                    'C'); // Credit to reverse

                if (!estornoSuccess)
                    _logger.LogError("CRÍTICO: Falha no estorno da transferência {Id}", request.IdentificacaoRequisicao);

                throw new BusinessException("Falha ao realizar crédito na conta de destino", "CREDIT_FAILED");
            }

            // 8. Persist successful transfer
            var transferencia = new Transferencia
            {
                Id = Guid.NewGuid(),
                ContaOrigemId = request.ContaOrigemId.Value,
                ContaDestinoNumero = request.ContaDestinoNumero,
                Valor = request.Valor,
                DataTransferencia = DateTime.UtcNow,
                IdentificacaoRequisicao = request.IdentificacaoRequisicao,
                Status = "COMPLETED"
            };

            await _repository.AdicionarAsync(transferencia);

            _logger.LogInformation("Transferência concluída com sucesso: {Id}", request.IdentificacaoRequisicao);

            // 9. Publish event to Kafka (after success)
            var topic = _configuration["Kafka:Topics:TransferenciasRealizadas"] ?? "transferencias-realizadas";

            await _kafkaProducer.PublishAsync(topic, new TransferenciaRealizadaEvent
            {
                IdentificacaoRequisicao = request.IdentificacaoRequisicao,
                ContaOrigemId = request.ContaOrigemId.Value,
                ContaDestinoNumero = request.ContaDestinoNumero,
                Valor = request.Valor,
                DataTransferencia = transferencia.DataTransferencia
            });

            _logger.LogInformation("Evento de transferência publicado no Kafka: {Id}", request.IdentificacaoRequisicao);
        }
        catch (BusinessException)
        {
            throw; // Re-throw business exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar transferência {Id}", request.IdentificacaoRequisicao);
            throw new BusinessException("Erro ao processar transferência", "TRANSFER_ERROR");
        }
    }
}
