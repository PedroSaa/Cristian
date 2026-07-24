using DocFlow.Api.Filters;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeFordoc;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeFordoc;
using DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeFordoc;
using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeFordoc;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeFordocs;
using DocFlow.Application.Common.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers.Catalogos;

[ApiController]
[Route("api/admin/catalogos/formatos-documento")]
[Authorize]
[RequireMfa]
public class AdminCatalogosFormatosDocumentoController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminCatalogosFormatosDocumentoController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<SeFordocDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListSeFordocQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{tipoCod:int}")]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(SeFordocDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int tipoCod, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetSeFordocQuery(checked((short)tipoCod)), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(typeof(SeFordocDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CrearSeFordocRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CrearSeFordocCommand(req.TipoRec, req.TipoInt, req.TipoDesc, req.CorrN, req.TipoEnv, req.SeFordocVistaI, req.SeFordocVistaE, req.SeFordocVistaR, req.SeFordocFormatoNum), ct);
            return CreatedAtAction(nameof(GetById), new { tipoCod = result.TipoCod }, result);
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

    [HttpPut("{tipoCod:int}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int tipoCod, [FromBody] ActualizarSeFordocRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ActualizarSeFordocCommand(checked((short)tipoCod), req.TipoRec, req.TipoInt, req.TipoDesc, req.CorrN, req.TipoEnv, req.SeFordocVistaI, req.SeFordocVistaE, req.SeFordocVistaR, req.SeFordocFormatoNum), ct);
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

    [HttpDelete("{tipoCod:int}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int tipoCod, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new EliminarSeFordocCommand(checked((short)tipoCod)), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}

public record CrearSeFordocRequest(short TipoRec, short TipoInt, string TipoDesc, int CorrN, int? TipoEnv = null, short SeFordocVistaI = 0, short SeFordocVistaE = 0, short SeFordocVistaR = 0, string? SeFordocFormatoNum = null);
public record ActualizarSeFordocRequest(short TipoRec, short TipoInt, string TipoDesc, int CorrN, int? TipoEnv = null, short SeFordocVistaI = 0, short SeFordocVistaE = 0, short SeFordocVistaR = 0, string? SeFordocFormatoNum = null);
