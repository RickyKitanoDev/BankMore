namespace Tarifa.API.Application.Configuration;

public class TarifaConfiguration
{
    public decimal ValorPorTransferencia { get; }

    public TarifaConfiguration(IConfiguration configuration)
    {
        var valor = configuration["Tarifa:ValorPorTransferencia"];
        ValorPorTransferencia = string.IsNullOrEmpty(valor) 
            ? 2.00m 
            : decimal.Parse(valor);
    }
}
