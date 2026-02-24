namespace Account.API.Application.DTOs;

public record SaldoResult(
    int NumeroConta,
    string Nome,
    DateTime DataHoraResposta,
    decimal Saldo
);
