using MediatR;

namespace Tarifa.API.Application.Commands;

public record ProcessarTarifaCommand(
    string IdentificacaoTransferencia,
    Guid ContaOrigemId,
    decimal ValorTransferencia
) : IRequest;
