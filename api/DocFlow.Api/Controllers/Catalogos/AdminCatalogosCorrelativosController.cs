using DocFlow.Api.Filters;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeCorfor;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeCorfor;
using DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeCorfor;
using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeCorfor;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeCorfors;
using DocFlow.Application.Common.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers.Catalogos;

[ApiController]
[Route("api/admin/catalogos/correlativos")]
[Authorize]
[RequireMfa]
public class AdminCatalogosCorrelativosController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminCatalogosCorrelativosController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<SeCorforDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListSeCorforQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{corrTip}")]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(SeCorforDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string corrTip, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetSeCorforQuery(corrTip), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(typeof(SeCorforDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CrearSeCorforRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CrearSeCorforCommand(req.CorrTip, req.CorrNro, req.CorrDes, req.CorrFch), ct);
            return CreatedAtAction(nameof(GetById), new { corrTip = result.CorrTip }, result);
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

    [HttpPut("{corrTip}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string corrTip, [FromBody] ActualizarSeCorforRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ActualizarSeCorforCommand(corrTip, req.CorrNro, req.CorrDes, req.CorrFch), ct);
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

    [HttpDelete("{corrTip}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string corrTip, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new EliminarSeCorforCommand(corrTip), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}

public record CrearSeCorforRequest(string CorrTip, int CorrNro, string CorrDes, DateTime CorrFch);
public record ActualizarSeCorforRequest(int CorrNro, string CorrDes, DateTime CorrFch);
