namespace Transfer.API.Infrastructure.Kafka.Events;

public class TransferenciaRealizadaEvent
{
    public string IdentificacaoRequisicao { get; set; } = string.Empty;
    public Guid ContaOrigemId { get; set; }
    public Guid ContaDestinoId { get; set; }
    public decimal Valor { get; set; }
    public DateTime DataTransferencia { get; set; }
}
