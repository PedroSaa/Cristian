using DocFlow.Api.Filters;
using DocFlow.Application.Common.Authorization;
using DocFlow.Application.Numeracion.Commands.CreatePlantilla;
using DocFlow.Application.Numeracion.Commands.DeletePlantilla;
using DocFlow.Application.Numeracion.Commands.SetPlantillaActiva;
using DocFlow.Application.Numeracion.Commands.TogglePlantilla;
using DocFlow.Application.Numeracion.Commands.UpdatePlantilla;
using DocFlow.Application.Numeracion.DTOs;
using DocFlow.Application.Numeracion.Queries.ListPlantillas;
using DocFlow.Domain.Entities.NumeracionesDocumento;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers;

/// <summary>
/// Admin controller for plantillas de numeración CRUD.
/// All endpoints require authenticated users with MFA and granular permissions.
/// </summary>
[ApiController]
[Route("api/admin/numeracion/plantillas")]
[Authorize]
[RequireMfa]
public class AdminPlantillasNumeracionController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminPlantillasNumeracionController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission("admin.plantillasNumeracion.ver")]
    [ProducesResponseType(typeof(List<PlantillaNumeracionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] bool? soloActivos = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListPlantillasQuery(soloActivos), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission("admin.plantillasNumeracion.editar")]
    [ProducesResponseType(typeof(PlantillaNumeracionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlantillaRequest req,
        CancellationToken ct)
    {
        try
        {
            var cmd = new CreatePlantillaCommand(req.Id, req.Descripcion, req.Patron,
                req.PorOrganismo, req.PorTipoDocumento, req.PorFormatoDocumento,
                req.Periodicidad, req.MomentoGeneracion, req.RellenoCeros, req.ValorInicial);
            var result = await _mediator.Send(cmd, ct);
            return Created("", result);
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

    [HttpPut("{id:int}")]
    [HasPermission("admin.plantillasNumeracion.editar")]
    [ProducesResponseType(typeof(PlantillaNumeracionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdatePlantillaRequest req,
        CancellationToken ct)
    {
        try
        {
            var cmd = new UpdatePlantillaCommand(id, req.Descripcion, req.Patron,
                req.PorOrganismo, req.PorTipoDocumento, req.PorFormatoDocumento,
                req.Periodicidad, req.MomentoGeneracion, req.RellenoCeros, req.ValorInicial);
            var result = await _mediator.Send(cmd, ct);
            return Ok(result);
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

    [HttpPut("{id:int}/toggle")]
    [HasPermission("admin.plantillasNumeracion.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Toggle(int id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new TogglePlantillaCommand(id), ct);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>Define esta plantilla como la activa del sistema (única activa).</summary>
    [HttpPut("{id:int}/activar")]
    [HasPermission("admin.plantillasNumeracion.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activar(int id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new SetPlantillaActivaCommand(id), ct);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("admin.plantillasNumeracion.editar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeletePlantillaCommand(id), ct);
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

    /// <summary>Catálogo de tokens válidos para construir patrones (para el selector del front).</summary>
    [HttpGet("tokens")]
    [HasPermission("admin.plantillasNumeracion.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<TokenNumeracion>), StatusCodes.Status200OK)]
    public IActionResult Tokens() => Ok(PatronNumeracion.Tokens);
}

public record CreatePlantillaRequest(
    int Id,
    string Descripcion,
    string Patron,
    bool PorOrganismo = false,
    bool PorTipoDocumento = false,
    bool PorFormatoDocumento = false,
    string Periodicidad = "CONTINUO",
    string MomentoGeneracion = "AL_INGRESAR",
    int RellenoCeros = 0,
    int ValorInicial = 0);

public record UpdatePlantillaRequest(
    string Descripcion,
    string Patron,
    bool PorOrganismo = false,
    bool PorTipoDocumento = false,
    bool PorFormatoDocumento = false,
    string Periodicidad = "CONTINUO",
    string MomentoGeneracion = "AL_INGRESAR",
    int RellenoCeros = 0,
    int ValorInicial = 0);
