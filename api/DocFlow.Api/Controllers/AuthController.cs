using DocFlow.Api.Helpers;
using DocFlow.Application.Auth.Commands.ChangePassword;
using DocFlow.Application.Auth.Commands.Login;
using DocFlow.Application.Auth.Commands.Logout;
using DocFlow.Application.Auth.Commands.Mfa;
using DocFlow.Application.Auth.Commands.RefreshToken;
using DocFlow.Application.Auth.Commands.UpdateProfile;
using DocFlow.Application.Auth.DTOs;
using DocFlow.Application.Auth.Queries.GetCurrentUser;
using DocFlow.Application.Auth.Queries.GetPasswordPolicy;
using DocFlow.Application.Auth.Queries.GetProfile;
using DocFlow.Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DocFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Política de contraseña efectiva, para que el cliente valide en sintonía con el backend.</summary>
    [HttpGet("password-policy")]
    [Authorize]
    [ProducesResponseType(typeof(PasswordPolicyDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPasswordPolicy(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPasswordPolicyQuery(), ct);
        return Ok(result);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        try
        {
            var command = new LoginCommand(request.Identifier, request.Password, request.MfaCode);
            var result = await _mediator.Send(command, ct);

            if (result.AuthState != AuthState.MfaSetupRequired)
            {
                // Set HttpOnly cookie on successful full authentication (non-MFA)
                Response.SetAccessTokenCookie(result.AccessToken);
                Response.SetRefreshTokenCookie(result.RefreshToken);
            }

            return Ok(result);
        }
        catch (MfaRequiredException ex)
        {
            // MFA challenge — do NOT set cookie (partial auth only)
            return Ok(new { requiresMfa = true, mfaToken = ex.MfaToken });
        }
        catch (LoginFailedException ex)
        {
            return Unauthorized(new { mensaje = ex.Message, intentosRestantes = ex.IntentosRestantes });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { mensaje = ex.Message, intentosRestantes = 5 });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest? request, CancellationToken ct)
    {
        try
        {
            var refreshToken = !string.IsNullOrWhiteSpace(request?.RefreshToken)
                ? request.RefreshToken
                : Request.Cookies["refresh_token"];
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Unauthorized(new { message = "Falta la información para renovar la sesión." });

            var command = new RefreshTokenCommand(refreshToken);
            var result = await _mediator.Send(command, ct);

            if (result.AuthState != AuthState.MfaSetupRequired)
            {
                // Replace access_token cookie with new token
                Response.SetAccessTokenCookie(result.AccessToken);
                Response.SetRefreshTokenCookie(result.RefreshToken);
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request, CancellationToken ct)
    {
        var refreshToken = !string.IsNullOrWhiteSpace(request?.RefreshToken)
            ? request.RefreshToken
            : Request.Cookies["refresh_token"];

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var command = new LogoutCommand(refreshToken);
            await _mediator.Send(command, ct);
        }

        // Clear the access_token cookie
        Response.ClearAccessTokenCookie();
        Response.ClearRefreshTokenCookie();

            return Ok(new { message = "Sesión cerrada correctamente." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetCurrentUserQuery(), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetProfileQuery(), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var command = new UpdateProfileCommand(request.Nombre, request.Email);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("mfa/enable")]
    [Authorize]
    public async Task<ActionResult<EnableMfaResult>> EnableMfa(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new EnableMfaCommand(), ct));
    }

    [HttpPost("mfa/verify")]
    [Authorize]
    public async Task<ActionResult<MfaVerificationResult>> VerifyMfa([FromBody] VerifyMfaRequest request, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new VerifyMfaCommand(request.Code), ct));
    }

    [HttpPost("mfa/disable")]
    [Authorize]
    public async Task<ActionResult> DisableMfa([FromBody] DisableMfaRequest request, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DisableMfaCommand(request.CurrentPassword), ct);
            return Ok(new { message = "Autenticación en dos pasos desactivada." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login/mfa")]
    [EnableRateLimiting("auth-login")]
    public async Task<ActionResult<LoginResultDto>> LoginMfa([FromBody] LoginMfaRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new LoginMfaCommand(request.MfaToken, request.Code), ct);

            // Set HttpOnly cookie on successful MFA authentication
            Response.SetAccessTokenCookie(result.AccessToken);
            Response.SetRefreshTokenCookie(result.RefreshToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { mensaje = ex.Message });
        }
    }

    [HttpPut("profile/password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        try
        {
            var command = new ChangePasswordCommand(request.CurrentPassword, request.NewPassword);
            await _mediator.Send(command, ct);
            return Ok(new { message = "Contraseña actualizada correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public record RefreshTokenRequest(string? RefreshToken);
public record UpdateProfileRequest(string? Nombre, string? Email);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record DisableMfaRequest(string CurrentPassword);
public record VerifyMfaRequest(string Code);
public record LoginMfaRequest(string MfaToken, string Code);
