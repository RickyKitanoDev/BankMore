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
        if (!request.ContaOrigemId.HasValue || !request.ContaOrigemNumero.HasValue)
            throw new BusinessException("Conta de origem inválida", "INVALID_ACCOUNT");

        // 3. Validate destination account number
        if (request.ContaDestinoNumero <= 0)
            throw new BusinessException("Número da conta de destino inválido", "INVALID_ACCOUNT");

        // 4. Prevent self-transfer
        if (request.ContaDestinoNumero == request.ContaOrigemNumero.Value)
            throw new BusinessException("Não é possível transferir para a mesma conta", "INVALID_ACCOUNT");

        // 5. Idempotency check
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

        // 6. Generate unique IDs for debit and credit operations
        var debitoId = $"{request.IdentificacaoRequisicao}-DEBIT";
        var creditoId = $"{request.IdentificacaoRequisicao}-CREDIT";

        try
        {
            // 7. Perform DEBIT on origin account (logged user account - uses token)
            _logger.LogInformation("Iniciando débito na conta origem: {NumeroConta}, Valor: {Valor}", 
                request.ContaOrigemNumero, request.Valor);

            var debitoSuccess = await _accountApi.RealizarMovimentacaoAsync(
                token,
                debitoId,
                null, // null = usa a conta do token
                request.Valor,
                'D'); // Debit

            if (!debitoSuccess)
                throw new BusinessException("Falha ao realizar débito na conta de origem", "DEBIT_FAILED");

            // 8. Perform CREDIT on destination account (explicit account number)
            _logger.LogInformation("Iniciando crédito na conta destino: {NumeroConta}, Valor: {Valor}", 
                request.ContaDestinoNumero, request.Valor);

            var creditoSuccess = await _accountApi.RealizarMovimentacaoAsync(
                token,
                creditoId,
                request.ContaDestinoNumero, // Explicit destination account number
                request.Valor,
                'C'); // Credit

            if (!creditoSuccess)
            {
                // 9. ROLLBACK: Credit failed, reverse debit (credit back to origin)
                _logger.LogWarning("Falha no crédito, iniciando estorno para conta origem");

                var estornoId = $"{request.IdentificacaoRequisicao}-REVERSAL";
                var estornoSuccess = await _accountApi.RealizarMovimentacaoAsync(
                    token,
                    estornoId,
                    null, // null = usa a conta do token
                    request.Valor,
                    'C'); // Credit back (reversal)

                if (!estornoSuccess)
                    _logger.LogError("CRÍTICO: Falha no estorno da transferência {Id}", request.IdentificacaoRequisicao);

                throw new BusinessException("Falha ao realizar crédito na conta de destino", "CREDIT_FAILED");
            }

            // 10. Persist successful transfer (ainda usa GUIDs internamente para manter compatibilidade)
            var transferencia = new Transferencia
            {
                Id = Guid.NewGuid(),
                ContaOrigemId = request.ContaOrigemId.Value,
                ContaDestinoId = Guid.Empty, // Temporário - será atualizado quando tiver endpoint para buscar conta por número
                Valor = request.Valor,
                DataTransferencia = DateTime.UtcNow,
                IdentificacaoRequisicao = request.IdentificacaoRequisicao,
                Status = "COMPLETED"
            };

            await _repository.AdicionarAsync(transferencia);

            _logger.LogInformation("Transferência concluída com sucesso: {Id}", request.IdentificacaoRequisicao);

            // 11. Publish event to Kafka (after success) - Non-blocking with timeout
            try
            {
                var topic = _configuration["Kafka:Topics:TransferenciasRealizadas"] ?? "transferencias-realizadas";

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _kafkaProducer.PublishAsync(topic, new TransferenciaRealizadaEvent
                {
                    IdentificacaoRequisicao = request.IdentificacaoRequisicao,
                    ContaOrigemId = request.ContaOrigemId.Value,
                    ContaDestinoId = Guid.Empty, // Temporário
                    Valor = request.Valor,
                    DataTransferencia = transferencia.DataTransferencia
                });

                _logger.LogInformation("Evento de transferência publicado no Kafka: {Id}", request.IdentificacaoRequisicao);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Timeout ao publicar evento Kafka para transferência {Id} - transferência já foi concluída", 
                    request.IdentificacaoRequisicao);
            }
            catch (Exception kafkaEx)
            {
                _logger.LogError(kafkaEx, "Falha ao publicar evento Kafka para transferência {Id} - transferência já foi concluída", 
                    request.IdentificacaoRequisicao);
                // NÃO propaga o erro - transferência já foi concluída com sucesso
            }
        }
        catch (BusinessException)
        {
            throw; // Re-throw business exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar transferência {Id}. Exception: {ExType}, Message: {ExMsg}, Inner: {Inner}", 
                request.IdentificacaoRequisicao, ex.GetType().Name, ex.Message, ex.InnerException?.Message);
            throw new BusinessException("Erro ao processar transferência", "TRANSFER_ERROR");
        }
    }
}
