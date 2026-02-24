namespace Tarifa.API.Domain.Entities;

public class Tarifacao
{
    public Guid Id { get; set; }
    public Guid ContaId { get; set; }
    public decimal ValorTarifado { get; set; }
    public DateTime DataHoraTarifacao { get; set; }
    public string IdentificacaoTransferencia { get; set; } = string.Empty;
}
