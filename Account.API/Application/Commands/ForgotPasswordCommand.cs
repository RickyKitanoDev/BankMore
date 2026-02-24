using MediatR;

namespace Account.API.Application.Commands;

public record ForgotPasswordCommand(string Cpf) : IRequest<ForgotPasswordResult>;