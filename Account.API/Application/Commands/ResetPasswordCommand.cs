using MediatR;

namespace Account.API.Application.Commands;

public record ResetPasswordCommand(string CodeId, string Code, string NovaSenha) : IRequest;
