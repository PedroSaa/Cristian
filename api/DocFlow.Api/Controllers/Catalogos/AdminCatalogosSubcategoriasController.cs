using DocFlow.Api.Filters;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarCatalogoSubcategoria;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearCatalogoSubcategoria;
using DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarCatalogoSubcategoria;
using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetCatalogoSubcategoria;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListCatalogoSubcategorias;
using DocFlow.Application.Common.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers.Catalogos;

[ApiController]
[Route("api/admin/catalogos/subcategorias")]
[Authorize]
[RequireMfa]
public class AdminCatalogosSubcategoriasController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminCatalogosSubcategoriasController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogoSubcategoriaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int? catCod, CancellationToken ct)
    {
        var result = await _mediator.Send(new ListCatalogoSubcategoriasQuery(catCod), ct);
        return Ok(result);
    }

    [HttpGet("{catCod:int}/{idSubcategoria:int}")]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(CatalogoSubcategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int catCod, int idSubcategoria, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetCatalogoSubcategoriaQuery(catCod, checked((short)idSubcategoria)), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(typeof(CatalogoSubcategoriaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CrearCatalogoSubcategoriaRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CrearCatalogoSubcategoriaCommand(req.CatCod, req.SubcatNombre, req.SubcatDescripcion), ct);
            return CreatedAtAction(nameof(GetById), new { catCod = result.CatCod, idSubcategoria = result.IdSubcategoria }, result);
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

    [HttpPut("{catCod:int}/{idSubcategoria:int}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int catCod, int idSubcategoria, [FromBody] ActualizarCatalogoSubcategoriaRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ActualizarCatalogoSubcategoriaCommand(catCod, checked((short)idSubcategoria), req.SubcatNombre, req.SubcatDescripcion), ct);
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

    [HttpDelete("{catCod:int}/{idSubcategoria:int}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int catCod, int idSubcategoria, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new EliminarCatalogoSubcategoriaCommand(catCod, checked((short)idSubcategoria)), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}

public record CrearCatalogoSubcategoriaRequest(int CatCod, string SubcatNombre, string? SubcatDescripcion);
public record ActualizarCatalogoSubcategoriaRequest(string SubcatNombre, string? SubcatDescripcion);
