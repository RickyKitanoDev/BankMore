using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Account.API.Application.Commands;

public record LoginCommand(
    [Required(ErrorMessage = "Número da conta ou CPF é obrigatório")]
    string NumeroOuCpf,
    [Required(ErrorMessage = "Senha é obrigatória")]
    string Senha
) : IRequest<string>;
