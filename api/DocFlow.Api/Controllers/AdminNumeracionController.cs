using DocFlow.Application.Common;
using DocFlow.Api.Filters;
using DocFlow.Application.Common.Authorization;
using DocFlow.Application.Numeracion.Commands.CreateCounter;
using DocFlow.Application.Numeracion.Commands.DeactivateCounter;
using DocFlow.Application.Numeracion.Commands.IncrementCounter;
using DocFlow.Application.Numeracion.Commands.ReactivateCounter;
using DocFlow.Application.Numeracion.Commands.SetCounterValue;
using DocFlow.Application.Numeracion.DTOs;
using DocFlow.Application.Numeracion.Queries.GetCounter;
using DocFlow.Application.Numeracion.Queries.ListCounters;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers;

/// <summary>
/// Admin controller for counter lifecycle management.
/// All endpoints require authenticated users with MFA and granular permissions.
/// </summary>
[ApiController]
[Route("api/admin/numeracion/contadores")]
[Authorize]
[RequireMfa]
public class AdminNumeracionController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminNumeracionController(ISender mediator) => _mediator = mediator;

    /// <summary>
    /// Lists counters with optional filtering.
    /// </summary>
    [HttpGet]
    [HasPermission("admin.numeracion.ver")]
    [ProducesResponseType(typeof(PagedResult<CounterListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? activo = null,
        [FromQuery] string? codigoContador = null,
        [FromQuery] string? orgDepCod = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ListCountersQuery(page, pageSize, activo, codigoContador, orgDepCod), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single counter by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission("admin.numeracion.ver")]
    [ProducesResponseType(typeof(CounterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetCounterQuery(id), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new counter.
    /// </summary>
    [HttpPost]
    [HasPermission("admin.numeracion.editar")]
    [ProducesResponseType(typeof(CounterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCounterRequest req, CancellationToken ct)
    {
        try
        {
            var cmd = new CreateCounterCommand(
                req.CodigoContador,
                req.OrgDepCod,
                req.TipoCod,
                req.DfTipo ?? "",
                req.NivelCod ?? "",
                req.Periodicidad ?? "CONTINUO",
                req.ValorInicial);

            var result = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Override the current value of a counter.
    /// </summary>
    [HttpPut("{id:guid}/valor")]
    [HasPermission("admin.numeracion.editar")]
    [ProducesResponseType(typeof(CounterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetValue(Guid id, [FromBody] SetCounterValueRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new SetCounterValueCommand(id, req.Valor), ct);
            return Ok(result);
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

    /// <summary>
    /// Atomically increments the counter and returns the next value.
    /// </summary>
    [HttpPost("{id:guid}/incrementar")]
    [HasPermission("admin.numeracion.editar")]
    [ProducesResponseType(typeof(NextValueResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Increment(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new IncrementCounterCommand(id), ct);
            return Ok(result);
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

    /// <summary>
    /// Deactivates a counter (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [HasPermission("admin.numeracion.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeactivateCounterCommand(id), ct);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Reactivates a previously deactivated counter.
    /// </summary>
    [HttpPut("{id:guid}/reactivar")]
    [HasPermission("admin.numeracion.editar")]
    [ProducesResponseType(typeof(CounterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new ReactivateCounterCommand(id), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}

// ── Request DTOs (controller-level) ──

public record CreateCounterRequest(
    string CodigoContador,
    string OrgDepCod,
    int TipoCod = 0,
    string? DfTipo = null,
    string? NivelCod = null,
    string? Periodicidad = "CONTINUO",
    long ValorInicial = 0);

public record SetCounterValueRequest(long Valor);
