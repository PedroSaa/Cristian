using DocFlow.Api.Filters;
using DocFlow.Api.Helpers;
using DocFlow.Application.Common.Authorization;
using DocFlow.Application.Admin.Usuarios.Commands.ActivarUsuario;
using DocFlow.Application.Admin.Usuarios.Commands.ActualizarUsuario;
using DocFlow.Application.Admin.Usuarios.Commands.BloquearUsuario;
using DocFlow.Application.Admin.Usuarios.Commands.CrearUsuario;
using DocFlow.Application.Admin.Usuarios.Commands.DesactivarUsuario;
using DocFlow.Application.Admin.Usuarios.Commands.DesbloquearUsuario;
using DocFlow.Application.Admin.Usuarios.Commands.ResetPasswordUsuario;
using DocFlow.Application.Admin.Usuarios.DTOs;
using DocFlow.Application.Common;
using DocFlow.Application.Admin.Usuarios.Queries.GetUsuario;
using DocFlow.Application.Admin.Usuarios.Queries.ListUsuarios;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers;

[ApiController]
[Route("api/admin/usuarios")]
[Authorize]
[RequireMfa]
public class AdminUsuariosController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminUsuariosController(ISender mediator) => _mediator = mediator;

    /// <summary>
    /// Returns a paginated list of users. Filterable by rol, departamento and activo.
    /// </summary>
    [HttpGet]
    [HasPermission("admin.usuarios.ver")]
    [ProducesResponseType(typeof(PagedResult<UsuarioAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? rol = null,
        [FromQuery] Guid? departamentoId = null,
        [FromQuery] bool? activo = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var paging = PaginationQuery.Normalize(page, pageSize);
        var result = await _mediator.Send(new ListUsuariosQuery(paging.Page, paging.PageSize, rol, departamentoId, activo, search), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single user by ID. Returns 404 if not found.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission("admin.usuarios.ver")]
    [ProducesResponseType(typeof(UsuarioAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetUsuarioQuery(id), ct);
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
    /// Creates a new user. Returns 409 if email already exists.
    /// </summary>
    [HttpPost]
    [HasPermission("admin.usuarios.crear")]
    [ProducesResponseType(typeof(UsuarioAdminDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CrearUsuarioRequest req, CancellationToken ct)
    {
        try
        {
            var cmd = new CrearUsuarioCommand(
                req.Nombres,
                req.ApellidoPaterno,
                req.ApellidoMaterno,
                req.Telefono,
                req.Direccion,
                req.Email,
                req.Rol,
                req.DepartamentoId,
                req.Password,
                Rut: req.Rut,
                Usucod: req.Usucod);
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
    /// Updates mutable fields (nombre, rol, departamentoId) of an existing user.
    /// </summary>
    [HttpPut("{id:guid}")]
    [HasPermission("admin.usuarios.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ActualizarUsuarioRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ActualizarUsuarioCommand(
                id,
                req.Nombres,
                req.ApellidoPaterno,
                req.ApellidoMaterno,
                req.Telefono,
                req.Direccion,
                req.Rol,
                req.DepartamentoId,
                req.Email,
                req.Rut), ct);
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
    /// Activates a user (sets Activo = true).
    /// </summary>
    [HttpPut("{id:guid}/activar")]
    [HasPermission("admin.usuarios.activar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ActivarUsuarioCommand(id), ct);
            return Ok();
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
    /// Deactivates a user (sets Activo = false) and logs audit entry.
    /// </summary>
    [HttpPut("{id:guid}/desactivar")]
    [HasPermission("admin.usuarios.desactivar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DesactivarUsuarioCommand(id), ct);
            return Ok();
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
    /// Resets the password for a user. Returns the new password in the response.
    /// </summary>
    [HttpPost("{id:guid}/reset-password")]
    [HasPermission("admin.usuarios.reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ResetPasswordUsuarioCommand(id, req.NuevaPassword), ct);
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

    /// <summary>
    /// Blocks a user (sets LockedUntil = UTC+30min) and invalidates their sessions.
    /// </summary>
    [HttpPut("{id:guid}/bloquear")]
    [HasPermission("admin.usuarios.bloquear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Bloquear(Guid id)
    {
        try
        {
            await _mediator.Send(new BloquearUsuarioCommand(id));
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

    /// <summary>
    /// Unblocks a user (clears lockout and failed login attempts).
    /// Uses the block permission because it reverses the same administrative lockout capability.
    /// </summary>
    [HttpPut("{id:guid}/desbloquear")]
    [HasPermission("admin.usuarios.bloquear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Desbloquear(Guid id)
    {
        try
        {
            await _mediator.Send(new DesbloquearUsuarioCommand(id));
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

// ── Request DTOs (controller-level) ──

public record CrearUsuarioRequest(
    string Nombres,
    string ApellidoPaterno,
    string ApellidoMaterno,
    string? Telefono,
    string? Direccion,
    string Email,
    string Rol,
    Guid? DepartamentoId,
    string Password,
    string? Rut = null,
    string? Usucod = null);

public record ActualizarUsuarioRequest(
    string Nombres,
    string ApellidoPaterno,
    string ApellidoMaterno,
    string? Telefono,
    string? Direccion,
    string Rol,
    Guid? DepartamentoId = null,
    string? Email = null,
    string? Rut = null);

public record ResetPasswordRequest(string NuevaPassword);
