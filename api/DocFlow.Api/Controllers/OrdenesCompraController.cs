using DocFlow.Application.Common.Authorization;
using DocFlow.Application.OrdenesCompra.Commands.ActualizarOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.DesvincularMercadoPublicoOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.VincularMercadoPublicoOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.AgregarAdjuntoOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.AnularOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.AprobarOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.CrearOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.EliminarAdjuntoOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.EnviarAprobacionOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.MarcarEnviadaOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.RechazarOrdenCompra;
using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Application.OrdenesCompra.Interfaces;
using DocFlow.Application.OrdenesCompra.Queries.BuscarOrdenMercadoPublico;
using DocFlow.Application.OrdenesCompra.Queries.GetAdjuntoContenido;
using DocFlow.Application.OrdenesCompra.Queries.GetOrdenCompra;
using DocFlow.Application.OrdenesCompra.Queries.GetOrdenCompraPdf;
using DocFlow.Application.OrdenesCompra.Queries.ListOrdenesCompra;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers;

[ApiController]
[Route("api/ordenes-compra")]
[Authorize]
public class OrdenesCompraController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdenesCompraController(IMediator mediator) => _mediator = mediator;

    // ── Queries ──

    /// <summary>Paginated list with optional filters (estado, proveedorId, search on numero/observaciones).</summary>
    [HttpGet]
    [HasPermission("ordenescompra.ver")]
    [ProducesResponseType(typeof(PaginatedOrdenesCompraResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] string? estado = null,
        [FromQuery] Guid? proveedorId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(
                new ListOrdenesCompraQuery(estado, proveedorId, search, page, pageSize), ct);
            return Ok(result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>Order detail with items, provider name/RUT and attachment metadata (no bytes).</summary>
    [HttpGet("{id:guid}")]
    [HasPermission("ordenescompra.ver")]
    [ProducesResponseType(typeof(OrdenCompraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _mediator.Send(new GetOrdenCompraQuery(id), ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>Purchase order rendered as PDF.</summary>
    [HttpGet("{id:guid}/pdf")]
    [HasPermission("ordenescompra.ver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken ct)
    {
        try
        {
            var pdf = await _mediator.Send(new GetOrdenCompraPdfQuery(id), ct);
            return File(pdf.Contenido, "application/pdf", pdf.NombreArchivo);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>Downloads an attachment's binary content.</summary>
    [HttpGet("{id:guid}/adjuntos/{adjuntoId:guid}/download")]
    [HasPermission("ordenescompra.ver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAdjunto(Guid id, Guid adjuntoId, CancellationToken ct)
    {
        try
        {
            var adjunto = await _mediator.Send(new GetAdjuntoContenidoQuery(id, adjuntoId), ct);
            return File(adjunto.Contenido, adjunto.ContentType, adjunto.NombreArchivo);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    // ── Commands ──

    /// <summary>Creates a purchase order draft (estado Borrador, no number assigned).</summary>
    [HttpPost]
    [HasPermission("ordenescompra.crear")]
    [ProducesResponseType(typeof(OrdenCompraDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CrearOrdenCompraRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CrearOrdenCompraCommand(
                req.ProveedorId,
                req.Fecha,
                req.Moneda,
                req.FormaPago,
                req.PlazoEntrega,
                req.LugarEntrega,
                req.Observaciones,
                MapItems(req.Items)), ct);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = MensajeValidacion(ex) });
        }
    }

    /// <summary>Updates header data and items. Only drafts and rejected orders are editable.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission("ordenescompra.crear")]
    [ProducesResponseType(typeof(OrdenCompraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ActualizarOrdenCompraRequest req, CancellationToken ct)
    {
        return await Ejecutar(async () => Ok(await _mediator.Send(new ActualizarOrdenCompraCommand(
            id,
            req.ProveedorId,
            req.Fecha,
            req.Moneda,
            req.FormaPago,
            req.PlazoEntrega,
            req.LugarEntrega,
            req.Observaciones,
            MapItems(req.Items)), ct)));
    }

    /// <summary>Submits the order for approval; assigns the business number on first submission.</summary>
    [HttpPost("{id:guid}/enviar-aprobacion")]
    [HasPermission("ordenescompra.crear")]
    [ProducesResponseType(typeof(OrdenCompraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EnviarAprobacion(Guid id, CancellationToken ct)
    {
        return await Ejecutar(async () =>
            Ok(await _mediator.Send(new EnviarAprobacionOrdenCompraCommand(id), ct)));
    }

    /// <summary>Approves a pending order. The approver is the authenticated user.</summary>
    [HttpPost("{id:guid}/aprobar")]
    [HasPermission("ordenescompra.aprobar")]
    [ProducesResponseType(typeof(OrdenCompraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Aprobar(Guid id, [FromBody] AprobarOrdenCompraRequest? req, CancellationToken ct)
    {
        return await Ejecutar(async () =>
            Ok(await _mediator.Send(new AprobarOrdenCompraCommand(id, req?.Comentario), ct)));
    }

    /// <summary>Rejects a pending order. A comment is mandatory.</summary>
    [HttpPost("{id:guid}/rechazar")]
    [HasPermission("ordenescompra.aprobar")]
    [ProducesResponseType(typeof(OrdenCompraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Rechazar(Guid id, [FromBody] RechazarOrdenCompraRequest req, CancellationToken ct)
    {
        return await Ejecutar(async () =>
            Ok(await _mediator.Send(new RechazarOrdenCompraCommand(id, req.Comentario), ct)));
    }

    /// <summary>Marks an approved order as sent to the provider.</summary>
    [HttpPost("{id:guid}/marcar-enviada")]
    [HasPermission("ordenescompra.crear")]
    [ProducesResponseType(typeof(OrdenCompraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarcarEnviada(Guid id, CancellationToken ct)
    {
        return await Ejecutar(async () =>
            Ok(await _mediator.Send(new MarcarEnviadaOrdenCompraCommand(id), ct)));
    }

    /// <summary>Cancels the order from any non-terminal state. A reason is mandatory.</summary>
    [HttpPost("{id:guid}/anular")]
    [HasPermission("ordenescompra.anular")]
    [ProducesResponseType(typeof(OrdenCompraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Anular(Guid id, [FromBody] AnularOrdenCompraRequest req, CancellationToken ct)
    {
        return await Ejecutar(async () =>
            Ok(await _mediator.Send(new AnularOrdenCompraCommand(id, req.Motivo), ct)));
    }

    /// <summary>Adds an attachment (base64 payload, max 10 MB).</summary>
    [HttpPost("{id:guid}/adjuntos")]
    [HasPermission("ordenescompra.crear")]
    [ProducesResponseType(typeof(OrdenCompraAdjuntoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AgregarAdjunto(Guid id, [FromBody] AgregarAdjuntoOrdenCompraRequest req, CancellationToken ct)
    {
        return await Ejecutar(async () =>
        {
            var result = await _mediator.Send(new AgregarAdjuntoOrdenCompraCommand(
                id, req.NombreArchivo, req.ContentType, req.ContenidoBase64), ct);
            return CreatedAtAction(nameof(GetById), new { id }, result);
        });
    }

    /// <summary>Deletes an attachment.</summary>
    [HttpDelete("{id:guid}/adjuntos/{adjuntoId:guid}")]
    [HasPermission("ordenescompra.crear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EliminarAdjunto(Guid id, Guid adjuntoId, CancellationToken ct)
    {
        return await Ejecutar(async () =>
        {
            await _mediator.Send(new EliminarAdjuntoOrdenCompraCommand(id, adjuntoId), ct);
            return NoContent();
        });
    }

    // ── Mercado Público (ChileCompra) ──

    /// <summary>Looks up a purchase order in the Mercado Público public API by its portal code.</summary>
    [HttpGet("mercado-publico/{codigo}")]
    [HasPermission("ordenescompra.ver")]
    [ProducesResponseType(typeof(MercadoPublicoOrdenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> BuscarMercadoPublico(string codigo, CancellationToken ct)
    {
        return await EjecutarMercadoPublico(async () =>
            Ok(await _mediator.Send(new BuscarOrdenMercadoPublicoQuery(codigo), ct)));
    }

    /// <summary>Links the order to a Mercado Público purchase order code (validated against the portal).</summary>
    [HttpPost("{id:guid}/vincular-mercado-publico")]
    [HasPermission("ordenescompra.crear")]
    [ProducesResponseType(typeof(OrdenCompraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> VincularMercadoPublico(
        Guid id, [FromBody] VincularMercadoPublicoOrdenCompraRequest req, CancellationToken ct)
    {
        return await EjecutarMercadoPublico(async () =>
            Ok(await _mediator.Send(new VincularMercadoPublicoOrdenCompraCommand(id, req.Codigo), ct)));
    }

    /// <summary>Removes the Mercado Público link from the order.</summary>
    [HttpDelete("{id:guid}/vincular-mercado-publico")]
    [HasPermission("ordenescompra.crear")]
    [ProducesResponseType(typeof(OrdenCompraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DesvincularMercadoPublico(Guid id, CancellationToken ct)
    {
        return await EjecutarMercadoPublico(async () =>
            Ok(await _mediator.Send(new DesvincularMercadoPublicoOrdenCompraCommand(id), ct)));
    }

    // ── Helpers ──

    private static IReadOnlyList<OrdenCompraItemInput> MapItems(IReadOnlyList<OrdenCompraItemRequest>? items)
        => (items ?? []).Select(i => new OrdenCompraItemInput(i.Descripcion, i.Cantidad, i.PrecioUnitario)).ToList();

    /// <summary>
    /// User-facing validation message: joins the per-field errors, skipping the technical
    /// "Validation failed: -- Prop: ..." wrapper that ValidationException.Message carries.
    /// </summary>
    private static string MensajeValidacion(FluentValidation.ValidationException ex)
    {
        var mensajes = ex.Errors?.Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
        return mensajes is { Count: > 0 } ? string.Join(" ", mensajes) : ex.Message;
    }

    /// <summary>Shared error mapping: 400 validation, 404 not found, 409 state conflicts.</summary>
    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try
        {
            return await accion();
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = MensajeValidacion(ex) });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            // Optimistic concurrency (xmin): another transition won the race.
            return Conflict(new
            {
                mensaje = "La orden de compra fue modificada por otro usuario. Actualice y vuelva a intentarlo.",
            });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            // Unique index violated — in practice the business number, e.g. after an admin
            // manually reset the numbering counter. Actionable 409 instead of a raw 500.
            return Conflict(new
            {
                mensaje = "El número asignado ya existe. Verifique el contador de numeración en Administración → Numeración.",
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Error mapping for Mercado Público endpoints: 400 validation, 404 not found,
    /// 503 when the ticket is missing or the portal is unreachable (InvalidOperationException).
    /// </summary>
    private async Task<IActionResult> EjecutarMercadoPublico(Func<Task<IActionResult>> accion)
    {
        try
        {
            return await accion();
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = MensajeValidacion(ex) });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensaje = ex.Message });
        }
    }
}

// ── Request DTOs (controller-level) ──

public record OrdenCompraItemRequest(
    string Descripcion,
    decimal Cantidad,
    decimal PrecioUnitario);

public record CrearOrdenCompraRequest(
    Guid ProveedorId,
    DateTime Fecha,
    string? Moneda = null,
    string? FormaPago = null,
    string? PlazoEntrega = null,
    string? LugarEntrega = null,
    string? Observaciones = null,
    IReadOnlyList<OrdenCompraItemRequest>? Items = null);

public record ActualizarOrdenCompraRequest(
    Guid ProveedorId,
    DateTime Fecha,
    string? Moneda = null,
    string? FormaPago = null,
    string? PlazoEntrega = null,
    string? LugarEntrega = null,
    string? Observaciones = null,
    IReadOnlyList<OrdenCompraItemRequest>? Items = null);

public record AprobarOrdenCompraRequest(string? Comentario = null);

public record RechazarOrdenCompraRequest(string Comentario);

public record AnularOrdenCompraRequest(string Motivo);

public record AgregarAdjuntoOrdenCompraRequest(
    string NombreArchivo,
    string ContentType,
    string ContenidoBase64);

public record VincularMercadoPublicoOrdenCompraRequest(string Codigo);
