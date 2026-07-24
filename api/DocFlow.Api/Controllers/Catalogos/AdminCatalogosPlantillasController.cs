using DocFlow.Api.Filters;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeForpla;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeForplaMedidas;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeForpla;
using DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeForpla;
using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using System.Security.Cryptography;
using DocFlow.Application.Admin.CatalogosLegado.Commands.UpdateSeForplaContenido;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeForpla;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeForplaContenido;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeForplaMedidas;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeForplas;
using DocFlow.Application.Common.Authorization;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers.Catalogos;

[ApiController]
[Route("api/admin/catalogos/plantillas")]
[Authorize]
[RequireMfa]
public class AdminCatalogosPlantillasController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IOnlyOfficeJwtService _jwtService;
    private readonly IOnlyOfficeDocumentService _conversionService;
    private readonly IIntegracionConfigService _configService;
    private readonly IHttpClientFactory _httpClientFactory;

    public AdminCatalogosPlantillasController(
        ISender mediator,
        IOnlyOfficeJwtService jwtService,
        IOnlyOfficeDocumentService conversionService,
        IIntegracionConfigService configService,
        IHttpClientFactory httpClientFactory)
    {
        _mediator = mediator;
        _jwtService = jwtService;
        _conversionService = conversionService;
        _configService = configService;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<SeForplaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListSeForplaQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{codForm}")]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(SeForplaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string codForm, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetSeForplaQuery(codForm), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpGet("{codForm}/pdf")]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetPdf(string codForm, CancellationToken ct)
    {
        try
        {
            // 404 si no existe; la extensión define el filetype para OnlyOffice.
            var (_, extension, _) = await _mediator.Send(new GetSeForplaContenidoQuery(codForm), ct);
            var documentUrl = BuildContenidoUrl(codForm);
            var pdfBytes = await _conversionService.ConvertToPdfFromUrlAsync(documentUrl, extension, ct);
            return File(pdfBytes, "application/pdf");
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

    /// <summary>
    /// Sirve el archivo Word crudo de la plantilla para que OnlyOffice lo descargue
    /// (previsualización PDF y editor). Protegido por el JWT de OnlyOffice — solo el
    /// backend, que posee el Secret, genera tokens válidos.
    /// </summary>
    [HttpGet("{codForm}/contenido")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContenido(string codForm, [FromQuery] string token, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(token) || !_jwtService.ValidateToken(token))
            return Unauthorized();

        try
        {
            var (bytes, extension, _) = await _mediator.Send(new GetSeForplaContenidoQuery(codForm), ct);
            var ext = extension.TrimStart('.').ToLowerInvariant();
            var mime = ext switch
            {
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "doc" => "application/msword",
                _ => "application/octet-stream",
            };
            return File(bytes, mime);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Devuelve la configuración del editor OnlyOffice para editar la plantilla in-place.
    /// El frontend la pasa a DocsAPI.DocEditor. Las URLs apuntan al backend interno
    /// (host.docker.internal) para que el contenedor las alcance.
    /// </summary>
    [HttpGet("{codForm}/editor-config")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEditorConfig(string codForm, CancellationToken ct)
    {
        try
        {
            var (bytes, extension, nomForm) = await _mediator.Send(new GetSeForplaContenidoQuery(codForm), ct);
            var ext = extension.TrimStart('.').ToLowerInvariant();
            var backend = ResolveBackendInternalUrl();
            var urlToken = _jwtService.CreateToken(new Dictionary<string, object>());
            var escaped = Uri.EscapeDataString(codForm);

            var document = new Dictionary<string, object>
            {
                ["fileType"] = ext,
                // OnlyOffice solo acepta keys [0-9a-zA-Z.=_-] (max 128). codForm ahora es un JSON
                // con {}",: — se hashea a hex para que la key sea siempre válida.
                ["key"] = $"{DocumentKeyHash(codForm)}_{ShortHash(bytes)}",
                ["title"] = $"{nomForm}.{ext}",
                ["url"] = $"{backend}/api/admin/catalogos/plantillas/{escaped}/contenido?token={urlToken}",
            };
            var editorConfig = new Dictionary<string, object>
            {
                ["callbackUrl"] = $"{backend}/api/admin/catalogos/plantillas/{escaped}/editor-callback?token={urlToken}",
                ["mode"] = "edit",
                ["lang"] = "es",
            };
            var config = new Dictionary<string, object>
            {
                ["documentType"] = "word",
                ["document"] = document,
                ["editorConfig"] = editorConfig,
            };
            // Token JWT que firma el config; OnlyOffice lo valida contra el config recibido.
            config["token"] = _jwtService.CreateToken(new Dictionary<string, object>
            {
                ["documentType"] = "word",
                ["document"] = document,
                ["editorConfig"] = editorConfig,
            });

            return Ok(new
            {
                editorUrl = _configService.GetOnlyOfficeDocumentServerUrl(),
                config,
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Callback de guardado de OnlyOffice. Cuando el editor cierra/guarda (status 2 o 6),
    /// descarga el .docx editado y lo persiste en la plantilla. Protegido por JWT.
    /// </summary>
    [HttpPost("{codForm}/editor-callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> EditorCallback(
        string codForm,
        [FromQuery] string token,
        [FromBody] OnlyOfficeEditorCallbackRequest callback,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(token) || !_jwtService.ValidateToken(token))
            return Unauthorized(new { error = 1 });

        // 2 = listo para guardar (editor cerrado), 6 = force save (mientras se edita)
        if ((callback.Status == 2 || callback.Status == 6) && !string.IsNullOrEmpty(callback.Url))
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                var bytes = await client.GetByteArrayAsync(callback.Url, ct);
                await _mediator.Send(new UpdateSeForplaContenidoCommand(codForm, bytes), ct);
            }
            catch
            {
                return Ok(new { error = 1 });
            }
        }

        return Ok(new { error = 0 });
    }

    /// <summary>
    /// Fuerza el guardado inmediato del documento abierto en el editor (Command Service de
    /// OnlyOffice), para que al cerrar el editor el contenido editado esté disponible al instante
    /// y no haya que esperar el guardado diferido. <c>saved=false</c> = no había cambios.
    /// </summary>
    [HttpPost("{codForm}/forcesave")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ForceSave(string codForm, [FromQuery] string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest(new { mensaje = "Falta la key del documento." });

        try
        {
            var saved = await _conversionService.ForceSaveAsync(key, ct);
            return Ok(new { saved });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensaje = ex.Message });
        }
    }

    private string ResolveBackendInternalUrl()
    {
        var backendUrl = _configService.GetOnlyOfficeBackendInternalUrl().TrimEnd('/');
        return string.IsNullOrWhiteSpace(backendUrl)
            ? $"{Request.Scheme}://{Request.Host.Value}"
            : backendUrl;
    }

    private string BuildContenidoUrl(string codForm)
    {
        var token = _jwtService.CreateToken(new Dictionary<string, object>());
        return $"{ResolveBackendInternalUrl()}/api/admin/catalogos/plantillas/{Uri.EscapeDataString(codForm)}/contenido?token={token}";
    }

    private static string ShortHash(byte[] data)
        => Convert.ToHexString(SHA256.HashData(data), 0, 4).ToLowerInvariant();

    /// <summary>
    /// Identificador estable del documento para la key de OnlyOffice: SHA256 corto (hex)
    /// del codForm, porque el codForm JSON contiene caracteres inválidos para la key.
    /// </summary>
    private static string DocumentKeyHash(string codForm)
        => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(codForm)), 0, 8).ToLowerInvariant();

    [HttpPost]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(typeof(SeForplaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CrearSeForplaRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CrearSeForplaCommand(req.TipoSeleccion, req.TipoCod, req.CatCod, req.IdSubcategoria, req.FileName, req.BlobForm, req.ObsForm), ct);
            return CreatedAtAction(nameof(GetById), new { codForm = result.CodForm }, result);
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

    [HttpPut("{codForm}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string codForm, [FromBody] ActualizarSeForplaRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ActualizarSeForplaCommand(codForm, req.FileName, req.BlobForm, req.ObsForm), ct);
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
    /// Medidas de la plantilla: coordenadas en puntos PDF donde se estampa cada objeto
    /// (firma, autorización, QR, etc.) al firmar documentos. Réplica del legacy.
    /// </summary>
    [HttpGet("{codForm}/medidas")]
    [HasPermission("admin.catalogos.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<SeForplaMedidaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMedidas(string codForm, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetSeForplaMedidasQuery(codForm), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPut("{codForm}/medidas")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMedidas(string codForm, [FromBody] ActualizarSeForplaMedidasRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ActualizarSeForplaMedidasCommand(codForm, req.Items), ct);
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

    [HttpDelete("{codForm}")]
    [HasPermission("admin.catalogos.editar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string codForm, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new EliminarSeForplaCommand(codForm), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}

public record OnlyOfficeEditorCallbackRequest(int Status, string? Url = null, string? Key = null);

public record CrearSeForplaRequest(string TipoSeleccion, short? TipoCod, int? CatCod, short? IdSubcategoria, string FileName, string BlobForm, string? ObsForm = null);
public record ActualizarSeForplaRequest(string? FileName = null, string? BlobForm = null, string? ObsForm = null);
public record ActualizarSeForplaMedidasRequest(List<ActualizarSeForplaMedidaItem> Items);
