namespace Account.API.Domain.Interfaces;

public interface IJwtService
{
    string GenerateToken(Guid contaId, int numeroConta);
}
