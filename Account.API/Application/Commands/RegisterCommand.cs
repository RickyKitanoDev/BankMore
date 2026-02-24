using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Account.API.Application.Commands;

public class RegisterCommand : IRequest<int>
{
    [Range(1, int.MaxValue, ErrorMessage = "Número da conta inválido")]
    public int NumeroConta { get; set; }

    [Required(ErrorMessage = "CPF é obrigatório")]
    public string Cpf { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    public string Senha { get; set; } = string.Empty;
}
