using Transfer.API.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Transfer.API.Application.DTOs;

namespace Transfer.API.Controllers;

[ApiController]
[Route("api/transferencia")]
public class TransferenciaController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransferenciaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    /// <summary>
    /// Realiza transferência entre contas da mesma instituição.
    /// Requer token JWT no header Authorization.
    /// </summary>
    /// <param name="command">RealizarTransferenciaCommand com IdentificacaoRequisicao, ContaDestinoNumero e Valor</param>
    /// <returns>204 No Content em caso de sucesso</returns>
    public async Task<IActionResult> Post(RealizarTransferenciaCommand command)
    {
        // Extract ContaId and NumeroConta from JWT token
        var contaIdClaim = User.FindFirst("ContaId")?.Value;
        var numeroContaClaim = User.FindFirst("NumeroConta")?.Value;

        if (string.IsNullOrEmpty(contaIdClaim) || !Guid.TryParse(contaIdClaim, out var contaOrigemId))
            return StatusCode(403, new { message = "Token inválido ou expirado", type = "USER_UNAUTHORIZED" });

        if (string.IsNullOrEmpty(numeroContaClaim) || !int.TryParse(numeroContaClaim, out var contaOrigemNumero))
            return StatusCode(403, new { message = "Token inválido ou expirado", type = "USER_UNAUTHORIZED" });

        // Create new command with ContaOrigemId and ContaOrigemNumero from token
        var commandWithOrigin = command with 
        { 
            ContaOrigemId = contaOrigemId,
            ContaOrigemNumero = contaOrigemNumero
        };

        await _mediator.Send(commandWithOrigin);
        return NoContent();
    }
}
