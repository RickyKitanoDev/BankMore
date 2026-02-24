namespace Account.API.Application.Commands;

using MediatR;
using System.ComponentModel.DataAnnotations;

public record RealizarMovimentacaoCommand(
    [Required(ErrorMessage = "Identificação da requisição é obrigatória")]
    string IdentificacaoRequisicao,

    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    decimal Valor,

    [Required(ErrorMessage = "Tipo de movimento é obrigatório")]
    char Tipo
) : IRequest
{
    // Internal properties set from JWT token or service calls
    public Guid? ContaOrigemId { get; init; }
    public int? NumeroConta { get; init; }
}

