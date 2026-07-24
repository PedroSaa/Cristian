using DocFlow.Api.Filters;
using DocFlow.Api.Helpers;
using DocFlow.Application.Admin.Auditoria.DTOs;
using DocFlow.Application.Admin.Auditoria.Queries.ExportAuditoria;
using DocFlow.Application.Admin.Auditoria.Queries.GetAuditoria;
using DocFlow.Application.Admin.Auditoria.Queries.GetValoresFiltro;
using DocFlow.Application.Admin.Auditoria.Queries.ListAuditoria;
using DocFlow.Application.Common;
using DocFlow.Application.Common.Authorization;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers;

[ApiController]
[Route("api/admin/auditoria")]
[Authorize]
[RequireMfa]
public class AdminAuditoriaController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminAuditoriaController(ISender mediator) => _mediator = mediator;

    /// <summary>
    /// Returns a paginated list of audit log entries. Filterable by accion.
    /// </summary>
    [HttpGet]
    [HasPermission("admin.auditoria.ver")]
    [ProducesResponseType(typeof(PagedResult<RegistroAuditoriaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? usuarioId = null,
        [FromQuery] string? entidad = null,
        [FromQuery] string? accion = null,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] string? usuarioNombre = null,
        CancellationToken ct = default)
    {
        var paging = PaginationQuery.Normalize(page, pageSize);
        var result = await _mediator.Send(
            new ListAuditoriaQuery(paging.Page, paging.PageSize, usuarioId, entidad, accion, desde, hasta, usuarioNombre), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single audit log entry by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission("admin.auditoria.ver")]
    [ProducesResponseType(typeof(RegistroAuditoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetAuditoriaQuery(id), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Returns distinct Accion and Entidad values for filter dropdowns.
    /// </summary>
    [HttpGet("valores-filtro")]
    [HasPermission("admin.auditoria.ver")]
    [ProducesResponseType(typeof(ValoresFiltro), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetValoresFiltro(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetValoresFiltroQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Exports audit logs as CSV. Supports the same filters as the list endpoint.
    /// Limit: 10,000 rows maximum.
    /// </summary>
    [HttpGet("exportar")]
    [HasPermission("admin.auditoria.ver")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Exportar(
        [FromQuery] Guid? usuarioId = null,
        [FromQuery] string? entidad = null,
        [FromQuery] string? accion = null,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        CancellationToken ct = default)
    {
        try
        {
            var bytes = await _mediator.Send(
                new ExportAuditoriaQuery(usuarioId, entidad, accion, desde, hasta), ct);
            return File(bytes, "text/csv", $"auditoria-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}
