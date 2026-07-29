using DocFlow.Application.Admin.Usuarios.Firma.Commands.EliminarFirmaUsuario;
using DocFlow.Application.Admin.Usuarios.Firma.Commands.GuardarFirmaUsuario;
using DocFlow.Application.Admin.Usuarios.Firma.DTOs;
using DocFlow.Application.Admin.Usuarios.Firma.Queries.GetFirmaImagen;
using DocFlow.Application.Admin.Usuarios.Firma.Queries.GetFirmaUsuario;
using DocFlow.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers;

/// <summary>
/// Self-service signature configuration from "Mi Perfil": the authenticated user manages
/// their OWN signature (image + optional encrypted PIN + acronym). Same feature as the admin
/// controller but scoped to the current user (id taken from the token, no admin permission).
/// </summary>
[ApiController]
[Route("api/perfil/firma")]
[Authorize]
public class PerfilFirmaController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ICurrentUser _currentUser;

    public PerfilFirmaController(ISender mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Metadata of the current user's signature (no image bytes, no decrypted PIN).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(FirmaUsuarioMetadataDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetadata(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFirmaUsuarioQuery(_currentUser.RequireAuthenticatedUserId()), ct);
        return Ok(result);
    }

    /// <summary>Downloads the current user's signature image. 404 if none configured.</summary>
    [HttpGet("imagen")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImagen(CancellationToken ct)
    {
        try
        {
            var imagen = await _mediator.Send(new GetFirmaImagenQuery(_currentUser.RequireAuthenticatedUserId()), ct);
            return File(imagen.Contenido, imagen.ContentType);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Creates or partially updates the current user's signature (upsert). Image (base64) is OPTIONAL:
    /// omitting it keeps the stored one (required only on creation). Omitting the clave preserves the stored PIN.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(FirmaUsuarioMetadataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Guardar([FromBody] GuardarFirmaRequest req, CancellationToken ct)
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
                new GuardarFirmaUsuarioCommand(_currentUser.RequireAuthenticatedUserId(), imagen, req.ContentType, req.Clave, req.Sigla), ct);
            return Ok(result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>Deletes the current user's signature. 404 if none exists.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new EliminarFirmaUsuarioCommand(_currentUser.RequireAuthenticatedUserId()), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}
