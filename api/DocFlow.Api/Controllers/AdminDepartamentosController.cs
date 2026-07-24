using DocFlow.Api.Filters;
using DocFlow.Application.Admin.Departamentos.Commands.ActualizarDepartamento;
using DocFlow.Application.Admin.Departamentos.Commands.ActivarDepartamento;
using DocFlow.Application.Admin.Departamentos.Commands.CrearDepartamento;
using DocFlow.Application.Admin.Departamentos.Commands.DesactivarDepartamento;
using DocFlow.Application.Admin.Departamentos.Commands.EliminarDepartamento;
using DocFlow.Application.Admin.Departamentos.DTOs;
using DocFlow.Application.Admin.Departamentos.Queries.GetDepartamento;
using DocFlow.Application.Admin.Departamentos.Queries.ListDepartamentos;
using DocFlow.Application.Common.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers;

[ApiController]
[Route("api/admin/departamentos")]
[Authorize]
[RequireMfa]
public class AdminDepartamentosController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminDepartamentosController(ISender mediator) => _mediator = mediator;

    /// <summary>
    /// Returns all departments. Optionally filter by activo.
    /// </summary>
    [HttpGet]
    [HasPermission("admin.departamentos.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<DepartamentoAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] bool? activo = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListDepartamentosQuery(activo), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single department by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission("admin.departamentos.ver")]
    [ProducesResponseType(typeof(DepartamentoAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetDepartamentoQuery(id), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new department. Returns 409 if Nombre or Codigo already exists.
    /// </summary>
    [HttpPost]
    [HasPermission("admin.departamentos.editar")]
    [ProducesResponseType(typeof(DepartamentoAdminDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CrearDepartamentoRequest req, CancellationToken ct)
    {
        try
        {
            var cmd = new CrearDepartamentoCommand(req.Nombre, req.Codigo);
            var result = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
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

    /// <summary>
    /// Updates nombre and codigo for an existing department.
    /// </summary>
    [HttpPut("{id:guid}")]
    [HasPermission("admin.departamentos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ActualizarDepartamentoRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ActualizarDepartamentoCommand(id, req.Nombre, req.Codigo), ct);
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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Activates a department.
    /// </summary>
    [HttpPut("{id:guid}/activar")]
    [HasPermission("admin.departamentos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activar(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ActivarDepartamentoCommand(id), ct);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Deactivates a department.
    /// </summary>
    [HttpPut("{id:guid}/desactivar")]
    [HasPermission("admin.departamentos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DesactivarDepartamentoCommand(id), ct);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a department. Only allowed when no users are assigned.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [HasPermission("admin.departamentos.editar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new EliminarDepartamentoCommand(id), ct);
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
}

// ── Request DTOs ──

public record CrearDepartamentoRequest(string Nombre, string Codigo);
public record ActualizarDepartamentoRequest(string Nombre, string Codigo);
