using DocFlow.Api.Filters;
using DocFlow.Application.Admin.Respaldos.Commands.RestoreRespaldo;
using DocFlow.Application.Admin.Respaldos.Commands.TriggerRespaldo;
using DocFlow.Application.Admin.Respaldos.Commands.UpsertRespaldoConfig;
using DocFlow.Application.Admin.Respaldos.DTOs;
using DocFlow.Application.Admin.Respaldos.Queries.GetRespaldoById;
using DocFlow.Application.Admin.Respaldos.Queries.GetRespaldoConfig;
using DocFlow.Application.Admin.Respaldos.Queries.GetRestoreLogs;
using DocFlow.Application.Admin.Respaldos.Queries.ListRespaldos;
using DocFlow.Application.Common.Authorization;
using DocFlow.Domain.Enums;
using DocFlow.Infrastructure.Configuration;
using DocFlow.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers;

[ApiController]
[Route("api/admin/respaldos")]
[Authorize]
[RequireMfa]
public class AdminRespaldosController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly BackupSettings _settings;

    public AdminRespaldosController(ISender mediator, BackupSettings settings)
    {
        _mediator = mediator;
        _settings = settings;
    }

    /// <summary>
    /// Returns all backups ordered by creation date descending.
    /// </summary>
    [HttpGet]
    [HasPermission("admin.respaldos.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<RespaldoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListRespaldosQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Triggers an async backup (alias for POST trigger).
    /// Returns 202 Accepted — the backup runs in the background.
    /// </summary>
    [HttpPost]
    [HasPermission("admin.respaldos.crear")]
    [ProducesResponseType(typeof(RespaldoDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Post(CancellationToken ct)
        => await Trigger(ct);

    /// <summary>
    /// Triggers an async backup. Returns 202 Accepted — the backup runs in
    /// the background via <see cref="RespaldoBackgroundService"/>.
    /// </summary>
    [HttpPost("trigger")]
    [HasPermission("admin.respaldos.crear")]
    [ProducesResponseType(typeof(RespaldoDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Trigger(CancellationToken ct)
    {
        var result = await _mediator.Send(new TriggerRespaldoCommand(), ct);
        return StatusCode(StatusCodes.Status202Accepted, result);
    }

    /// <summary>
    /// Downloads a completed backup file. Returns 400 if the backup is not
    /// completed, 404 if it does not exist.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [HasPermission("admin.respaldos.descargar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        RespaldoDto respaldo;
        try
        {
            respaldo = await _mediator.Send(new GetRespaldoByIdQuery(id), ct);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { mensaje = $"Respaldo {id} no encontrado." });
        }

        if (respaldo.Estado != EstadoRespaldo.Completado)
        {
            return BadRequest(new { mensaje = "El respaldo debe estar en estado Completado para descargarse." });
        }

        if (!BackupPathValidator.TryResolveExistingBackupFile(
            _settings.OutputPath,
            respaldo.Ruta,
            out var resolvedPath,
            out var errorMessage))
        {
            return BadRequest(new { mensaje = errorMessage });
        }

        var fileName = Path.GetFileName(respaldo.Ruta);
        return PhysicalFile(resolvedPath, "application/octet-stream", fileName);
    }

    /// <summary>
    /// Restores a completed backup. Requires <c>X-Confirm-Restore</c> header
    /// matching the backup name. Returns 202 Accepted — the restore runs in
    /// the background via <see cref="RestoreBackgroundService"/>.
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    [HasPermission("admin.respaldos.restaurar")]
    [ProducesResponseType(typeof(RestoreLogDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(
        Guid id,
        [FromHeader(Name = "X-Confirm-Restore")] string confirmName,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(confirmName))
        {
            return BadRequest(new { mensaje = "Header X-Confirm-Restore es requerido." });
        }

        RespaldoDto respaldo;
        try
        {
            respaldo = await _mediator.Send(new GetRespaldoByIdQuery(id), ct);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { mensaje = $"Respaldo {id} no encontrado." });
        }

        if (!string.Equals(respaldo.Nombre, confirmName, StringComparison.Ordinal))
        {
            return BadRequest(new { mensaje = "El nombre en X-Confirm-Restore no coincide con el respaldo." });
        }

        if (!BackupPathValidator.TryResolveExistingBackupFile(
            _settings.OutputPath,
            respaldo.Ruta,
            out _,
            out var errorMessage))
        {
            return BadRequest(new { mensaje = errorMessage });
        }

        var result = await _mediator.Send(new RestoreRespaldoCommand(id), ct);
        return StatusCode(StatusCodes.Status202Accepted, result);
    }

    /// <summary>
    /// Returns restore logs for a specific backup.
    /// </summary>
    [HttpGet("{id:guid}/restore-logs")]
    [HasPermission("admin.respaldos.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<RestoreLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRestoreLogs(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRestoreLogsQuery(id), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns the current RespaldoConfig.
    /// </summary>
    [HttpGet("config")]
    [HasPermission("admin.respaldos.ver")]
    [ProducesResponseType(typeof(RespaldoConfigDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRespaldoConfigQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Creates or updates the RespaldoConfig. Reading config remains admin.respaldos.ver;
    /// changing output path, retention, schedule or timeout requires configuration permission.
    /// </summary>
    [HttpPut("config")]
    [HasPermission("admin.respaldos.configurar")]
    [ProducesResponseType(typeof(RespaldoConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateConfig(
        UpsertRespaldoConfigCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}
