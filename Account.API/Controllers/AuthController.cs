using Account.API.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Account.API.Application.DTOs;

namespace Account.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // InactivateRequest moved to Account.API/Application/DTOs/InactivateRequest.cs

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    /// <summary>
    /// Efetua o login da conta por número ou CPF e senha.
    /// Retorna um token JWT que contém a identificação da conta.
    /// </summary>
    /// <param name="command">LoginCommand com NumeroOuCpf e Senha</param>
    /// <returns>Token JWT no corpo em caso de sucesso</returns>
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var token = await _mediator.Send(command);
        return Ok(new Account.API.Application.DTOs.TokenResult(token));
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    /// <summary>
    /// Cadastra uma nova conta corrente. Recebe CPF e senha.
    /// Valida CPF e persiste a conta. Retorna o número da conta criado.
    /// </summary>
    /// <param name="command">RegisterCommand com NumeroConta, Cpf, Nome e Senha</param>
    /// <returns>Numero da conta criado</returns>
    public async Task<IActionResult> Register(RegisterCommand command)
    {
        var numeroConta = await _mediator.Send(command);
        return Created("", new Account.API.Application.DTOs.RegisterResult(numeroConta));
    }

    [HttpPost("forgot")]
    [ProducesResponseType(typeof(Account.API.Application.Commands.ForgotPasswordResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    /// <summary>
    /// Gera um código de redefinição de senha (6 dígitos) e retorna codeId + code para teste.
    /// Em produção o código deveria ser enviado por canal seguro (e-mail/SMS).
    /// </summary>
    /// <param name="command">ForgotPasswordCommand com CPF</param>
    /// <returns>Objeto com codeId e code (6 dígitos)</returns>
    public async Task<IActionResult> Forgot(ForgotPasswordCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(new { codeId = result.CodeId, code = result.Code });
    }

    [HttpGet("forgot/{codeId}/debug")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    /// <summary>
    /// Endpoint de debug (desenvolvimento) para inspecionar status do código de redefinição.
    /// </summary>
    /// <param name="codeId">Identificador do código (codeId)</param>
    /// <returns>Informações de expiração e uso do código</returns>
    public async Task<IActionResult> DebugForgot(string codeId)
    {
        // only for local debugging: inspect stored password reset (expires/used)
        var repo = HttpContext.RequestServices.GetRequiredService<Account.API.Domain.Interfaces.IContaRepository>();
        var row = await repo.ObterPasswordResetRawAsync(codeId);
        if (row is null) return NotFound();

        return Ok(new { codeId, expires = row.Value.Expires, used = row.Value.Used });
    }

    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    /// <summary>
    /// Reseta a senha usando codeId + code e a nova senha.
    /// </summary>
    /// <param name="command">ResetPasswordCommand com CodeId, Code e NovaSenha</param>
    /// <returns>204 No Content em caso de sucesso</returns>
    public async Task<IActionResult> Reset(ResetPasswordCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    [Authorize]
    [HttpPost("inactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Inactivate([FromBody] InactivateRequest request)
    {
        /// <summary>
        /// Inativa a conta corrente do usuário autenticado. Requer token JWT no header e a senha no corpo.
        /// </summary>
        /// <param name="request">InactivateRequest com a senha da conta</param>
        /// <returns>204 No Content em caso de sucesso</returns>
        // User is authenticated by the authentication middleware
        // extract ContaId from claims
        var contaIdClaim = User.FindFirst("ContaId")?.Value;
        if (string.IsNullOrEmpty(contaIdClaim) || !System.Guid.TryParse(contaIdClaim, out var contaId))
            return StatusCode(403, new { message = "Token inválido ou expirado", type = "USER_UNAUTHORIZED" });

        await _mediator.Send(new InativarContaCommand(contaId, request.Senha));

        return NoContent();
    }
}
