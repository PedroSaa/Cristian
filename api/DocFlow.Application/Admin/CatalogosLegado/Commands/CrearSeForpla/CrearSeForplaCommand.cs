using System.Text.Json;
using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeForpla;

/// <summary>
/// Crea una plantilla SEFORPLA replicando el flujo legacy: el usuario solo elige la
/// asociación (Formato "T", Categoría "C" o Subcategoría "S"), sube el archivo y agrega
/// una observación. El resto (codForm, tipoCod, usucod, nomForm, extForm, sisForm) se deriva.
/// </summary>
public record CrearSeForplaCommand(
    string TipoSeleccion,
    short? TipoCod,
    int? CatCod,
    short? IdSubcategoria,
    string FileName,
    string BlobForm,
    string? ObsForm) : IRequest<SeForplaDto>;

public class CrearSeForplaCommandValidator : AbstractValidator<CrearSeForplaCommand>
{
    private static readonly string[] TiposValidos = ["T", "C", "S"];

    public CrearSeForplaCommandValidator()
    {
        RuleFor(x => x.TipoSeleccion)
            .Must(t => TiposValidos.Contains(t))
            .WithMessage("El tipo de asociación debe ser T (formato), C (categoría) o S (subcategoría).");

        RuleFor(x => x.TipoCod)
            .NotNull().When(x => x.TipoSeleccion == "T", ApplyConditionTo.CurrentValidator)
            .WithMessage("El formato de documento es obligatorio cuando la asociación es por formato.")
            .GreaterThan((short)0).When(x => x.TipoSeleccion == "T" && x.TipoCod.HasValue, ApplyConditionTo.CurrentValidator)
            .WithMessage("El formato de documento debe ser mayor que cero.");

        RuleFor(x => x.CatCod)
            .NotNull().When(x => x.TipoSeleccion is "C" or "S", ApplyConditionTo.CurrentValidator)
            .WithMessage("La categoría es obligatoria cuando la asociación es por categoría o subcategoría.")
            .GreaterThan(0).When(x => x.TipoSeleccion is "C" or "S" && x.CatCod.HasValue, ApplyConditionTo.CurrentValidator)
            .WithMessage("La categoría debe ser mayor que cero.");

        RuleFor(x => x.IdSubcategoria)
            .NotNull().When(x => x.TipoSeleccion == "S", ApplyConditionTo.CurrentValidator)
            .WithMessage("La subcategoría es obligatoria cuando la asociación es por subcategoría.")
            .GreaterThan((short)0).When(x => x.TipoSeleccion == "S" && x.IdSubcategoria.HasValue, ApplyConditionTo.CurrentValidator)
            .WithMessage("La subcategoría debe ser mayor que cero.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("El nombre del archivo es obligatorio.");

        RuleFor(x => x.BlobForm)
            .NotEmpty().WithMessage("El contenido de la plantilla es obligatorio.");

        RuleFor(x => x.ObsForm)
            .MaximumLength(255).WithMessage("La observación no puede superar los 255 caracteres.");
    }
}

public class CrearSeForplaCommandHandler : IRequestHandler<CrearSeForplaCommand, SeForplaDto>
{
    private readonly ISeForplaRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CrearSeForplaCommandHandler> _logger;
    private readonly ISeFordocRepository _formatos;
    private readonly ICatalogoCategoriaRepository _categorias;
    private readonly ICatalogoSubcategoriaRepository _subcategorias;
    private readonly ISeUsuariRepository _usuarios;
    private readonly ISeForplaMedidaRepository _medidas;

    public CrearSeForplaCommandHandler(
        ISeForplaRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<CrearSeForplaCommandHandler> logger,
        ISeFordocRepository formatos,
        ICatalogoCategoriaRepository categorias,
        ICatalogoSubcategoriaRepository subcategorias,
        ISeUsuariRepository usuarios,
        ISeForplaMedidaRepository medidas)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
        _formatos = formatos;
        _categorias = categorias;
        _subcategorias = subcategorias;
        _usuarios = usuarios;
        _medidas = medidas;
    }

    public async Task<SeForplaDto> Handle(CrearSeForplaCommand cmd, CancellationToken ct)
    {
        // El target de la asociación debe existir (no hay FKs en la BD legacy); evita
        // plantillas huérfanas apuntando a formatos/categorías/subcategorías inexistentes.
        await ValidarTargetAsync(cmd);

        var codForm = SeForplaCodForm.Build(cmd.TipoSeleccion, cmd.TipoCod, cmd.CatCod, cmd.IdSubcategoria);
        var tipoCod = ResolverTipoCodColumna(cmd);
        var usucod = await ResolverUsucodAsync(ct);
        var (nomForm, extForm) = DerivarNombreYExtension(cmd.FileName);
        var blob = DecodificarBlob(cmd.BlobForm);

        if (await _repo.ExistsAsync(codForm))
            throw new InvalidOperationException($"Ya existe una plantilla asociada a esa selección ({codForm}).");

        var entity = new SeForpla(codForm, usucod, tipoCod, nomForm, blob, "1", cmd.ObsForm, extForm, alto: null, ancho: null);
        await _repo.CreateAsync(entity);

        // Legacy pingresamedida: toda plantilla nace con sus 7 medidas por defecto.
        await _medidas.CreateRangeAsync(SeForplaMedida.CrearDefaults(entity.CodForm));

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "CrearSeForpla",
            "SeForpla",
            entity.CodForm,
            $"Plantilla creada: {entity.NomForm}"));

        return new SeForplaDto(entity.CodForm, entity.Usucod, entity.TipoCod, entity.NomForm, entity.BlobForm, entity.SisForm, entity.ObsForm, entity.ExtForm, entity.Alto, entity.Ancho);
    }

    private async Task ValidarTargetAsync(CrearSeForplaCommand cmd)
    {
        switch (cmd.TipoSeleccion)
        {
            case "T":
                if (!await _formatos.ExistsAsync(cmd.TipoCod!.Value))
                    throw new ValidationException($"No existe un formato de documento con el código {cmd.TipoCod.Value}.");
                break;
            case "C":
                if (await _categorias.GetByIdAsync(cmd.CatCod!.Value) is null)
                    throw new ValidationException($"No existe una categoría con el código {cmd.CatCod.Value}.");
                break;
            case "S":
                if (!await _subcategorias.ExistsAsync(cmd.CatCod!.Value, cmd.IdSubcategoria!.Value))
                    throw new ValidationException($"No existe la subcategoría {cmd.IdSubcategoria.Value} en la categoría {cmd.CatCod.Value}.");
                break;
        }
    }

    /// <summary>
    /// Legacy: la columna tipoCod guarda el código seleccionado, sea formato, categoría
    /// o subcategoría. La columna es short, así que un CatCod fuera de rango es inválido.
    /// </summary>
    private static short ResolverTipoCodColumna(CrearSeForplaCommand cmd)
    {
        return cmd.TipoSeleccion switch
        {
            "T" => cmd.TipoCod!.Value,
            "C" => ToShort(cmd.CatCod!.Value),
            _ => cmd.IdSubcategoria!.Value,
        };

        static short ToShort(int value) => value is > short.MaxValue or < short.MinValue
            ? throw new ValidationException($"El código de categoría {value} está fuera del rango soportado.")
            : (short)value;
    }

    private async Task<string> ResolverUsucodAsync(CancellationToken ct)
    {
        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        var usuario = await _usuarios.GetByIdAsync(usuarioId, ct)
            ?? throw new ValidationException("No se encontró el usuario autenticado.");
        return usuario.Usucod;
    }

    internal static (string NomForm, string ExtForm) DerivarNombreYExtension(string fileName)
    {
        var nomForm = Path.GetFileNameWithoutExtension(fileName).Trim();
        if (nomForm.Length == 0)
            throw new ValidationException("El nombre del archivo no es válido.");
        if (nomForm.Length > 30)
            nomForm = nomForm[..30];

        var extForm = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        if (extForm.Length == 0)
            throw new ValidationException("El archivo debe tener extensión (.doc o .docx).");
        if (extForm.Length > 10)
            extForm = extForm[..10];

        return (nomForm, extForm);
    }

    internal static byte[] DecodificarBlob(string base64)
    {
        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            throw new ValidationException("El contenido del archivo no es un base64 válido.");
        }
    }
}

/// <summary>
/// Construye el codForm JSON byte-compatible con producción:
/// {"tipo":"T","nt":1,"nc":0,"ns":0} — orden de propiedades exacto y sin espacios.
/// System.Text.Json serializa las propiedades del record en orden de declaración.
/// </summary>
public static class SeForplaCodForm
{
    private sealed record Payload(string tipo, int nt, int nc, int ns);

    public static string Build(string tipoSeleccion, short? tipoCod, int? catCod, short? idSubcategoria)
    {
        var payload = tipoSeleccion switch
        {
            "T" => new Payload("T", tipoCod!.Value, 0, 0),
            "C" => new Payload("C", 0, catCod!.Value, 0),
            "S" => new Payload("S", 0, catCod!.Value, idSubcategoria!.Value),
            _ => throw new ValidationException($"Tipo de asociación inválido: {tipoSeleccion}."),
        };

        return JsonSerializer.Serialize(payload);
    }
}
