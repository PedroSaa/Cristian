using DocFlow.Api.Filters;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeTiptar;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeTiptar;
using DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeTiptar;
using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeTiptar;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeTiptar;
using DocFlow.Application.Common.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers.Catalogos;

[ApiController]
[Route("api/admin/catalogos/acciones-tarea")]
[Authorize]
[RequireMfa]
public class AdminCatalogosAccionesTareaController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminCatalogosAccionesTareaController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<SeTiptarDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListSeTiptarQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{dftaccion}")]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(SeTiptarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string dftaccion, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetSeTiptarQuery(dftaccion), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(typeof(SeTiptarDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CrearSeTiptarRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CrearSeTiptarCommand(req.DFTACCION, req.DFTACOBSV, req.DFTACDESC), ct);
            return CreatedAtAction(nameof(GetById), new { dftaccion = result.DFTACCION }, result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    [HttpPut("{dftaccion}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string dftaccion, [FromBody] ActualizarSeTiptarRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ActualizarSeTiptarCommand(dftaccion, req.DFTACOBSV, req.DFTACDESC), ct);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpDelete("{dftaccion}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string dftaccion, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new EliminarSeTiptarCommand(dftaccion), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}

public record CrearSeTiptarRequest(string DFTACCION, string? DFTACOBSV = null, string? DFTACDESC = null);
public record ActualizarSeTiptarRequest(string? DFTACOBSV = null, string? DFTACDESC = null);
