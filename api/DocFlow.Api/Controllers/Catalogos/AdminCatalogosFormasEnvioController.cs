using DocFlow.Api.Filters;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeFormaEnvio;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeFormaEnvio;
using DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeFormaEnvio;
using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeFormaEnvio;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeFormaEnvio;
using DocFlow.Application.Common.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers.Catalogos;

[ApiController]
[Route("api/admin/catalogos/formas-envio")]
[Authorize]
[RequireMfa]
public class AdminCatalogosFormasEnvioController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminCatalogosFormasEnvioController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<SeFormaEnvioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListSeFormaEnvioQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{idFormaEnvio:int}")]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(SeFormaEnvioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int idFormaEnvio, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetSeFormaEnvioQuery(checked((short)idFormaEnvio)), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(typeof(SeFormaEnvioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CrearSeFormaEnvioRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CrearSeFormaEnvioCommand(req.FormaEnvio), ct);
            return CreatedAtAction(nameof(GetById), new { idFormaEnvio = result.IdFormaEnvio }, result);
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

    [HttpPut("{idFormaEnvio:int}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int idFormaEnvio, [FromBody] ActualizarSeFormaEnvioRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ActualizarSeFormaEnvioCommand(checked((short)idFormaEnvio), req.FormaEnvio), ct);
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

    [HttpDelete("{idFormaEnvio:int}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int idFormaEnvio, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new EliminarSeFormaEnvioCommand(checked((short)idFormaEnvio)), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}

public record CrearSeFormaEnvioRequest(string FormaEnvio);
public record ActualizarSeFormaEnvioRequest(string FormaEnvio);
