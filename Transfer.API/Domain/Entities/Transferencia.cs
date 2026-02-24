namespace Transfer.API.Domain.Entities;

public class Transferencia
{
    public Guid Id { get; set; }
    public Guid ContaOrigemId { get; set; }
    public Guid ContaDestinoId { get; set; }
    public decimal Valor { get; set; }
    public DateTime DataTransferencia { get; set; }
    public string IdentificacaoRequisicao { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // COMPLETED, FAILED, REVERSED
}
