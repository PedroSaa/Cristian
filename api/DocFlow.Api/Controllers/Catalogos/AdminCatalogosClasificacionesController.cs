using DocFlow.Api.Filters;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeClaseg;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeClaseg;
using DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeClaseg;
using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeClaseg;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeClaseg;
using DocFlow.Application.Common.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers.Catalogos;

[ApiController]
[Route("api/admin/catalogos/clasificaciones")]
[Authorize]
[RequireMfa]
public class AdminCatalogosClasificacionesController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminCatalogosClasificacionesController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<SeClasegDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListSeClasegQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{dfClasif:int}")]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(SeClasegDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int dfClasif, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetSeClasegQuery(checked((short)dfClasif)), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(typeof(SeClasegDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CrearSeClasegRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CrearSeClasegCommand(req.DFNCLASIF, req.DFDClasif), ct);
            return CreatedAtAction(nameof(GetById), new { dfClasif = result.DFClasif }, result);
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

    [HttpPut("{dfClasif:int}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int dfClasif, [FromBody] ActualizarSeClasegRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ActualizarSeClasegCommand(checked((short)dfClasif), req.DFNCLASIF, req.DFDClasif), ct);
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

    [HttpDelete("{dfClasif:int}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int dfClasif, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new EliminarSeClasegCommand(checked((short)dfClasif)), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}

public record CrearSeClasegRequest(string DFNCLASIF, string DFDClasif);
public record ActualizarSeClasegRequest(string DFNCLASIF, string DFDClasif);
