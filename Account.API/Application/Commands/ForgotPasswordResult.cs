namespace Account.API.Application.Commands;

public record ForgotPasswordResult(string CodeId, string Code);
