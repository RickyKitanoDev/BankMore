using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Transfer.API.Application.Commands;

public record RealizarTransferenciaCommand(
    [Required(ErrorMessage = "Identificação da requisição é obrigatória")]
    string IdentificacaoRequisicao,
    
    [Range(1, int.MaxValue, ErrorMessage = "Número da conta de destino inválido")]
    int ContaDestinoNumero,
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    decimal Valor
) : IRequest
{
    // Internal property to hold the ContaOrigemId from JWT token
    public Guid? ContaOrigemId { get; init; }
    public int? ContaOrigemNumero { get; init; }
};
