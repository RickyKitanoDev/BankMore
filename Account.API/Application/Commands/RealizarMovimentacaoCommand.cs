namespace Account.API.Application.Commands;

using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public record RealizarMovimentacaoCommand(
    [Required(ErrorMessage = "Identificação da requisição é obrigatória")]
    string IdentificacaoRequisicao,

    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    decimal Valor,

    [Required(ErrorMessage = "Tipo de movimento é obrigatório")]
    char Tipo,

    // Número da conta (opcional - se não informado, usa a do token)
    int? ContaId = null
) : IRequest
{
    // Set from JWT token
    [JsonIgnore]
    public Guid? ContaOrigemId { get; init; }

    [JsonIgnore]
    public int? NumeroConta { get; init; }
}

