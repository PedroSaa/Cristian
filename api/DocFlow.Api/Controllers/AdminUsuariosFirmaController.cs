using DocFlow.Api.Filters;
using DocFlow.Application.Admin.Usuarios.Firma.Commands.EliminarFirmaUsuario;
using DocFlow.Application.Admin.Usuarios.Firma.Commands.GuardarFirmaUsuario;
using DocFlow.Application.Admin.Usuarios.Firma.DTOs;
using DocFlow.Application.Admin.Usuarios.Firma.Queries.GetFirmaImagen;
using DocFlow.Application.Admin.Usuarios.Firma.Queries.GetFirmaUsuario;
using DocFlow.Application.Common.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers;

/// <summary>
/// Manages the per-user signature configuration (image + optional encrypted PIN + acronym).
/// One signature per user. Reads require admin.usuarios.ver; writes require admin.usuarios.editar.
/// </summary>
[ApiController]
[Route("api/admin/usuarios/{usuarioId:guid}/firma")]
[Authorize]
[RequireMfa]
public class AdminUsuariosFirmaController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminUsuariosFirmaController(ISender mediator) => _mediator = mediator;

    /// <summary>Returns the signature configuration metadata (no image bytes, no decrypted PIN).</summary>
    [HttpGet]
    [HasPermission("admin.usuarios.ver")]
    [ProducesResponseType(typeof(FirmaUsuarioMetadataDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetadata(Guid usuarioId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFirmaUsuarioQuery(usuarioId), ct);
        return Ok(result);
    }

    /// <summary>Downloads the signature image. Returns 404 if the user has no signature configured.</summary>
    [HttpGet("imagen")]
    [HasPermission("admin.usuarios.ver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImagen(Guid usuarioId, CancellationToken ct)
    {
        try
        {
            var imagen = await _mediator.Send(new GetFirmaImagenQuery(usuarioId), ct);
            return File(imagen.Contenido, imagen.ContentType);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Creates or partially updates the user signature (upsert). The image travels as base64 and is OPTIONAL:
    /// when omitted the stored image is kept (required only on creation). Clave is optional too — omitting it
    /// preserves the stored PIN.
    /// </summary>
    [HttpPut]
    [HasPermission("admin.usuarios.editar")]
    [ProducesResponseType(typeof(FirmaUsuarioMetadataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Guardar(Guid usuarioId, [FromBody] GuardarFirmaRequest req, CancellationToken ct)
    {
        byte[]? imagen = null;
        if (!string.IsNullOrEmpty(req.ImagenBase64))
        {
            try
            {
                imagen = Convert.FromBase64String(req.ImagenBase64);
            }
            catch (FormatException)
            {
                return BadRequest(new { mensaje = "La imagen de la firma no es un base64 válido." });
            }
        }

        try
        {
            var result = await _mediator.Send(
                new GuardarFirmaUsuarioCommand(usuarioId, imagen, req.ContentType, req.Clave, req.Sigla), ct);
            return Ok(result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>Deletes the user signature. Returns 404 if none exists.</summary>
    [HttpDelete]
    [HasPermission("admin.usuarios.editar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid usuarioId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new EliminarFirmaUsuarioCommand(usuarioId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}

// ── Request DTOs (controller-level) ──

public record GuardarFirmaRequest(
    string? ImagenBase64 = null,
    string? ContentType = null,
    string? Clave = null,
    string? Sigla = null);
