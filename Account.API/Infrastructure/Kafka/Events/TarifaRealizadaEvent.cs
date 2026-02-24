namespace Account.API.Infrastructure.Kafka.Events;

public class TarifaRealizadaEvent
{
    public string TarifacaoId { get; set; } = string.Empty;
    public Guid ContaId { get; set; }
    public decimal Valor { get; set; }
    public DateTime DataHoraTarifacao { get; set; }
}
