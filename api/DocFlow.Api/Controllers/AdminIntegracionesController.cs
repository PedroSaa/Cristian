using DocFlow.Api.Filters;
using DocFlow.Application.Admin.Integraciones.Commands.ActualizarIntegracion;
using DocFlow.Application.Admin.Integraciones.Commands.ProbarConexion;
using DocFlow.Application.Admin.Integraciones.DTOs;
using DocFlow.Application.Admin.Integraciones.Queries.GetIntegracion;
using DocFlow.Application.Admin.Integraciones.Queries.GetIntegracionIdByNombre;
using DocFlow.Application.Admin.Integraciones.Queries.ListIntegraciones;
using DocFlow.Application.Common.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers;

[ApiController]
[Route("api/admin/integraciones")]
[Authorize]
[RequireMfa]
public class AdminIntegracionesController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminIntegracionesController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns all external integration configurations. ApiKey is masked.
    /// </summary>
    [HttpGet]
    [HasPermission("admin.integraciones.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<IntegracionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListIntegracionesQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single integration by ID. ApiKey is masked.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission("admin.integraciones.ver")]
    [ProducesResponseType(typeof(IntegracionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetIntegracionQuery(id), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Updates an integration by service name or GUID. If <paramref name="servicio"/> is a valid GUID,
    /// resolves by ID; otherwise resolves by service name via <see cref="IIntegracionRepository.GetByNombreAsync"/>.
    /// </summary>
    [HttpPut("{servicio}")]
    [HasPermission("admin.integraciones.editar")]
    [ProducesResponseType(typeof(IntegracionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string servicio, [FromBody] ActualizarIntegracionRequest req, CancellationToken ct)
    {
        try
        {
            Guid id;
            if (Guid.TryParse(servicio, out var parsedGuid))
            {
                id = parsedGuid;
            }
            else
            {
                id = await _mediator.Send(new GetIntegracionIdByNombreQuery(servicio), ct);
            }

            var cmd = new ActualizarIntegracionCommand(id, req.BaseUrl, req.ApiKey, req.Activo, req.Settings);
            var result = await _mediator.Send(cmd, ct);
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
    /// Tests connectivity to an external integration by service name or GUID.
    /// A failed connection returns HTTP 200 with Success=false — connectivity failure is data, not an API error.
    /// </summary>
    [HttpPost("{servicio}/test")]
    [HasPermission("admin.integraciones.editar")]
    [ProducesResponseType(typeof(IntegracionTestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProbarConexion(string servicio, CancellationToken ct)
    {
        try
        {
            Guid id;
            if (Guid.TryParse(servicio, out var parsedGuid))
            {
                id = parsedGuid;
            }
            else
            {
                id = await _mediator.Send(new GetIntegracionIdByNombreQuery(servicio), ct);
            }

            var result = await _mediator.Send(new ProbarConexionIntegracionCommand(id), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}

// ── Request DTOs ──

public record ActualizarIntegracionRequest(
    string BaseUrl,
    string? ApiKey,
    bool Activo,
    IReadOnlyDictionary<string, string>? Settings = null);
