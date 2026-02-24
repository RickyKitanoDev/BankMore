using System.ComponentModel.DataAnnotations;

namespace Account.API.Application.DTOs;

public class InactivateRequest
{
    [Required(ErrorMessage = "Senha é obrigatória")]
    public string Senha { get; set; } = string.Empty;
}
