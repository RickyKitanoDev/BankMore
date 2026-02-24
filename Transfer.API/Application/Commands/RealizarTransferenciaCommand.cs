using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Transfer.API.Application.Commands;

public record RealizarTransferenciaCommand(
    [Required(ErrorMessage = "Identificação da requisição é obrigatória")]
    string IdentificacaoRequisicao,

    [Required(ErrorMessage = "Número da conta de destino é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "Número da conta inválido")]
    int ContaDestinoNumero,

    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    decimal Valor
) : IRequest
{
    // Set from JWT token
    [JsonIgnore]
    public Guid? ContaOrigemId { get; init; }

    [JsonIgnore]
    public int? ContaOrigemNumero { get; init; }
};
