using MediatR;

namespace Account.API.Application.Commands;

public record InativarContaCommand(Guid ContaId, string Senha) : IRequest;
