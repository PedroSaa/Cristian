using DocFlow.Api.Filters;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSerem;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeremTipo;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSerem;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeremTipo;
using DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSerem;
using DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeremTipo;
using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSerem;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeremTipo;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListSerems;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeremTipos;
using DocFlow.Application.Common.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers;

[ApiController]
[Route("api/admin/remitentes-legado")]
[Authorize]
[RequireMfa]
public class AdminRemitentesLegadoController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminRemitentesLegadoController(ISender mediator) => _mediator = mediator;

    [HttpGet("tipos")]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<SeremTipoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTipos(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListSeremTiposQuery(), ct);
        return Ok(result);
    }

    [HttpGet("tipos/{remTipo}")]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(SeremTipoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTipo(string remTipo, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetSeremTipoQuery(remTipo), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPost("tipos")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(typeof(SeremTipoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTipo([FromBody] CrearSeremTipoRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CrearSeremTipoCommand(req.RemTipo, req.RemDesc), ct);
            return CreatedAtAction(nameof(GetTipo), new { remTipo = result.RemTipo }, result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = string.Join(" ", ex.Errors.Select(e => e.ErrorMessage)) });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    [HttpPut("tipos/{remTipo}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTipo(string remTipo, [FromBody] ActualizarSeremTipoRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ActualizarSeremTipoCommand(remTipo, req.RemDesc), ct);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = string.Join(" ", ex.Errors.Select(e => e.ErrorMessage)) });
        }
    }

    [HttpDelete("tipos/{remTipo}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTipo(string remTipo, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new EliminarSeremTipoCommand(remTipo), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    [HttpGet]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<SeremDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRemitentes([FromQuery] string? remTipo = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListSeremsQuery(remTipo), ct);
        return Ok(result);
    }

    [HttpGet("{remCod}")]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(SeremDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRemitente(string remCod, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetSeremQuery(remCod), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(typeof(SeremDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRemitente([FromBody] CrearSeremRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CrearSeremCommand(
                req.RemCod,
                req.RemTipo,
                req.RemNomb,
                req.RemRutValid,
                req.RemSector,
                req.RemComuna,
                req.RemNro,
                req.RemEmail,
                req.RemFax,
                req.RemRut,
                req.RemDirec,
                req.RemTelef,
                req.RemZip,
                req.RemRegion,
                req.RemBlock,
                req.RemCalle,
                req.RemCodDocDigital), ct);

            return CreatedAtAction(nameof(GetRemitente), new { remCod = result.RemCod }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = string.Join(" ", ex.Errors.Select(e => e.ErrorMessage)) });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    [HttpPut("{remCod}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRemitente(string remCod, [FromBody] ActualizarSeremRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ActualizarSeremCommand(
                remCod,
                req.RemTipo,
                req.RemNomb,
                req.RemRutValid,
                req.RemSector,
                req.RemComuna,
                req.RemNro,
                req.RemEmail,
                req.RemFax,
                req.RemRut,
                req.RemDirec,
                req.RemTelef,
                req.RemZip,
                req.RemRegion,
                req.RemBlock,
                req.RemCalle,
                req.RemCodDocDigital), ct);

            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = string.Join(" ", ex.Errors.Select(e => e.ErrorMessage)) });
        }
    }

    [HttpDelete("{remCod}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRemitente(string remCod, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new EliminarSeremCommand(remCod), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}

public record CrearSeremTipoRequest(string RemTipo, string RemDesc);
public record ActualizarSeremTipoRequest(string RemDesc);
public record CrearSeremRequest(
    string RemCod,
    string RemTipo,
    string RemNomb,
    short? RemRutValid = null,
    string? RemSector = null,
    string? RemComuna = null,
    int? RemNro = null,
    string? RemEmail = null,
    string? RemFax = null,
    string? RemRut = null,
    string? RemDirec = null,
    string? RemTelef = null,
    string? RemZip = null,
    string? RemRegion = null,
    string? RemBlock = null,
    string? RemCalle = null,
    decimal? RemCodDocDigital = null);
public record ActualizarSeremRequest(
    string RemTipo,
    string RemNomb,
    short? RemRutValid = null,
    string? RemSector = null,
    string? RemComuna = null,
    int? RemNro = null,
    string? RemEmail = null,
    string? RemFax = null,
    string? RemRut = null,
    string? RemDirec = null,
    string? RemTelef = null,
    string? RemZip = null,
    string? RemRegion = null,
    string? RemBlock = null,
    string? RemCalle = null,
    decimal? RemCodDocDigital = null);
