using DocFlow.Api.Filters;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarCatalogoCategoria;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearCatalogoCategoria;
using DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarCatalogoCategoria;
using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetCatalogoCategoria;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListCatalogoCategorias;
using DocFlow.Application.Common.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers.Catalogos;

[ApiController]
[Route("api/admin/catalogos/categorias")]
[Authorize]
[RequireMfa]
public class AdminCatalogosCategoriasController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminCatalogosCategoriasController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogoCategoriaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListCatalogoCategoriasQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{catCod:int}")]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(CatalogoCategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int catCod, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetCatalogoCategoriaQuery(catCod), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(typeof(CatalogoCategoriaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CrearCatalogoCategoriaRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CrearCatalogoCategoriaCommand(req.CatDesc), ct);
            return CreatedAtAction(nameof(GetById), new { catCod = result.CatCod }, result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    [HttpPut("{catCod:int}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int catCod, [FromBody] ActualizarCatalogoCategoriaRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ActualizarCatalogoCategoriaCommand(catCod, req.CatDesc), ct);
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
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpDelete("{catCod:int}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int catCod, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new EliminarCatalogoCategoriaCommand(catCod), ct);
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

public record CrearCatalogoCategoriaRequest(string CatDesc);
public record ActualizarCatalogoCategoriaRequest(string CatDesc);
