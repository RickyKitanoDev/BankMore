using Account.API.Application.Commands;
using Account.API.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Account.API.Controllers;

[ApiController]
[Route("api/movimentacao")]
public class MovimentacaoController : ControllerBase
{
    private readonly IMediator _mediator;

    public MovimentacaoController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Account.API.Application.DTOs.ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Account.API.Application.DTOs.ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Account.API.Application.DTOs.ErrorDto), StatusCodes.Status403Forbidden)]
    /// <summary>
    /// Realiza uma movimentação (crédito ou débito) na conta autenticada.
    /// Requer token JWT no header Authorization.
    /// Para débitos: a conta é extraída do token JWT (não pode debitar de terceiros).
    /// Para créditos: pode usar contaId do payload (permitir transferências).
    /// </summary>
    /// <param name="command">RealizarMovimentacaoCommand com IdentificacaoRequisicao, Valor, Tipo e opcionalmente ContaId</param>
    /// <returns>204 No Content em caso de sucesso</returns>
    public async Task<IActionResult> Post(RealizarMovimentacaoCommand command)
    {
        // Extract ContaId from JWT token
        var contaIdClaim = User.FindFirst("ContaId")?.Value;
        var numeroContaClaim = User.FindFirst("NumeroConta")?.Value;

        if (string.IsNullOrEmpty(contaIdClaim) || !System.Guid.TryParse(contaIdClaim, out var contaFromToken))
            return StatusCode(403, new { message = "Token inválido ou expirado", type = "USER_UNAUTHORIZED" });

        if (string.IsNullOrEmpty(numeroContaClaim) || !int.TryParse(numeroContaClaim, out var numeroContaFromToken))
            return StatusCode(403, new { message = "Token inválido ou expirado", type = "USER_UNAUTHORIZED" });

        // Validate type
        if (command.Tipo != 'C' && command.Tipo != 'D')
            return BadRequest(new { message = "Tipo inválido. Use 'D' para débito ou 'C' para crédito", type = "INVALID_TYPE" });

        // Para DÉBITOS: sempre usar conta do token (segurança - não pode debitar de terceiros)
        // Para CRÉDITOS: usar numero de conta do payload se fornecido, senão do token
        int numeroConta;
        if (command.Tipo == 'D')
        {
            // Débito: SEMPRE da conta do token (segurança)
            numeroConta = numeroContaFromToken;
        }
        else // command.Tipo == 'C'
        {
            // Crédito: usar numero da conta do payload se fornecido (parametro ContaId no JSON), senão do token
            numeroConta = command.ContaId ?? numeroContaFromToken;
        }

        // Set ContaOrigemId (GUID do token) and NumeroConta
        var commandWithContaId = command with 
        { 
            ContaOrigemId = contaFromToken,
            NumeroConta = numeroConta
        };

        await _mediator.Send(commandWithContaId);
        return NoContent();
    }

    [Authorize]
    [HttpGet("saldo")]
    [ProducesResponseType(typeof(Account.API.Application.DTOs.SaldoResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Account.API.Application.DTOs.ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Account.API.Application.DTOs.ErrorDto), StatusCodes.Status403Forbidden)]
    /// <summary>
    /// Consulta o saldo da conta autenticada.
    /// Requer token JWT no header Authorization.
    /// Retorna número da conta, nome do titular, data/hora da consulta e saldo atual.
    /// </summary>
    /// <returns>200 OK com SaldoResult contendo dados da conta e saldo</returns>
    public async Task<IActionResult> GetSaldo()
    {
        // Extract ContaId from JWT token
        var contaIdClaim = User.FindFirst("ContaId")?.Value;

        if (string.IsNullOrEmpty(contaIdClaim) || !System.Guid.TryParse(contaIdClaim, out var contaId))
            return StatusCode(403, new { message = "Token inválido ou expirado", type = "USER_UNAUTHORIZED" });

        var query = new ObterSaldoQuery(contaId);
        var resultado = await _mediator.Send(query);

        return Ok(resultado);
    }
}
