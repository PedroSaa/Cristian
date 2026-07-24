using DocFlow.Api.Filters;
using DocFlow.Application.Admin.Configuracion.Commands.UpsertConfiguracion;
using DocFlow.Application.Admin.Configuracion.Commands.UploadBrandingLogo;
using DocFlow.Application.Admin.Configuracion.Commands.UploadLoginBackground;
using DocFlow.Application.Admin.Configuracion.DTOs;
using DocFlow.Application.Admin.Configuracion.Queries.GetConfiguracion;
using DocFlow.Application.Admin.Configuracion.Queries.ListConfiguracion;
using DocFlow.Application.Common.Authorization;
using DocFlow.Application.Common.Branding;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers;

[ApiController]
[Route("api/admin/configuracion")]
[Authorize]
[RequireMfa]
public class AdminConfiguracionController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminConfiguracionController(ISender mediator) => _mediator = mediator;

    /// <summary>
    /// Returns all system configuration entries.
    /// </summary>
    [HttpGet]
    [HasPermission("admin.config.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<ConfiguracionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListConfiguracionQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a configuration entry by its clave (key).
    /// </summary>
    [HttpGet("{clave}")]
    [HasPermission("admin.config.ver")]
    [ProducesResponseType(typeof(ConfiguracionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByClave(string clave, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetConfiguracionQuery(clave), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Creates or updates a configuration entry (upsert by Clave).
    /// </summary>
    [HttpPut]
    [HasPermission("admin.config.editar")]
    [ProducesResponseType(typeof(ConfiguracionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert([FromBody] UpsertConfiguracionRequest req, CancellationToken ct)
    {
        try
        {
            var cmd = new UpsertConfiguracionCommand(req.Clave, req.Valor, req.Descripcion);
            var result = await _mediator.Send(cmd, ct);
            return Ok(result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Uploads the institutional branding logo.
    /// </summary>
    [HttpPost("logo")]
    [Consumes("multipart/form-data")]
    [HasPermission("admin.config.editar")]
    [ProducesResponseType(typeof(ConfiguracionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadLogo([FromForm] IFormFile logo, CancellationToken ct)
    {
        if (logo is null || logo.Length == 0)
            return BadRequest(new { mensaje = "El archivo de logo es obligatorio." });
        if (logo.Length > BrandingImageUploadValidation.MaxImageBytes)
            return BadRequest(new { mensaje = $"El logo no puede superar {BrandingImageUploadValidation.MaxImageMegabytes} MB." });

        try
        {
            using var ms = new MemoryStream();
            await logo.CopyToAsync(ms, ct);

            var cmd = new UploadBrandingLogoCommand(ms.ToArray(), logo.FileName, logo.ContentType);
            var result = await _mediator.Send(cmd, ct);
            return Ok(result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Uploads the login background image.
    /// </summary>
    [HttpPost("login-background")]
    [Consumes("multipart/form-data")]
    [HasPermission("admin.config.editar")]
    [ProducesResponseType(typeof(ConfiguracionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadLoginBackground([FromForm] IFormFile loginBackground, CancellationToken ct)
    {
        if (loginBackground is null || loginBackground.Length == 0)
            return BadRequest(new { mensaje = "El archivo de fondo de login es obligatorio." });
        if (loginBackground.Length > BrandingImageUploadValidation.MaxImageBytes)
            return BadRequest(new { mensaje = $"El fondo de login no puede superar {BrandingImageUploadValidation.MaxImageMegabytes} MB." });

        try
        {
            using var ms = new MemoryStream();
            await loginBackground.CopyToAsync(ms, ct);

            var cmd = new UploadLoginBackgroundCommand(ms.ToArray(), loginBackground.FileName, loginBackground.ContentType);
            var result = await _mediator.Send(cmd, ct);
            return Ok(result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}

// ── Request DTOs ──

public record UpsertConfiguracionRequest(string Clave, string Valor, string? Descripcion = null);
