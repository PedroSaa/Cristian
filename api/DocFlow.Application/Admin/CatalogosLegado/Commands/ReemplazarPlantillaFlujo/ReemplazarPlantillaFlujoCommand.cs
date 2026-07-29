using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.ReemplazarPlantillaFlujo;

/// <summary>Input for a single workflow step. Enums travel as strings and are validated/parsed in the handler.</summary>
public record PlantillaFlujoPasoInput(
    int Orden,
    string TipoAccion,
    string ResponsableTipo,
    Guid ResponsableId,
    bool Obligatorio);

/// <summary>Replaces the entire workflow of a template with the given ordered steps.</summary>
public record ReemplazarPlantillaFlujoCommand(
    string CodForm,
    IReadOnlyList<PlantillaFlujoPasoInput> Pasos) : IRequest<IReadOnlyList<PlantillaFlujoPasoDto>>;

public class ReemplazarPlantillaFlujoCommandValidator : AbstractValidator<ReemplazarPlantillaFlujoCommand>
{
    public ReemplazarPlantillaFlujoCommandValidator()
    {
        RuleFor(x => x.CodForm)
            .NotEmpty().WithMessage("El código de plantilla es obligatorio.");

        RuleFor(x => x.Pasos)
            .NotNull().WithMessage("Los pasos del flujo son obligatorios.");

        RuleForEach(x => x.Pasos).ChildRules(paso =>
        {
            paso.RuleFor(p => p.Orden)
                .GreaterThanOrEqualTo(1).WithMessage("El orden de cada paso debe ser mayor o igual a 1.");

            paso.RuleFor(p => p.ResponsableId)
                .NotEmpty().WithMessage("El responsable de cada paso es obligatorio.");

            paso.RuleFor(p => p.TipoAccion)
                .Must(v => Enum.TryParse<TipoAccionFlujo>(v, ignoreCase: true, out _))
                .WithMessage(_ => $"El tipo de acción debe ser uno de: {string.Join(", ", Enum.GetNames<TipoAccionFlujo>())}.");

            paso.RuleFor(p => p.ResponsableTipo)
                .Must(v => Enum.TryParse<ResponsableFlujoTipo>(v, ignoreCase: true, out _))
                .WithMessage(_ => $"El tipo de responsable debe ser uno de: {string.Join(", ", Enum.GetNames<ResponsableFlujoTipo>())}.");
        });

        RuleFor(x => x.Pasos)
            .Must(pasos => pasos is null || pasos.Select(p => p.Orden).Distinct().Count() == pasos.Count)
            .WithMessage("El orden de los pasos no puede repetirse.");
    }
}

public class ReemplazarPlantillaFlujoCommandHandler
    : IRequestHandler<ReemplazarPlantillaFlujoCommand, IReadOnlyList<PlantillaFlujoPasoDto>>
{
    private readonly IPlantillaFlujoRepository _flujo;
    private readonly ISeForplaRepository _plantillas;
    private readonly IResponsableFlujoNombreResolver _nombres;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ReemplazarPlantillaFlujoCommandHandler> _logger;

    public ReemplazarPlantillaFlujoCommandHandler(
        IPlantillaFlujoRepository flujo,
        ISeForplaRepository plantillas,
        IResponsableFlujoNombreResolver nombres,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<ReemplazarPlantillaFlujoCommandHandler> logger)
    {
        _flujo = flujo;
        _plantillas = plantillas;
        _nombres = nombres;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PlantillaFlujoPasoDto>> Handle(
        ReemplazarPlantillaFlujoCommand cmd, CancellationToken ct)
    {
        if (!await _plantillas.ExistsAsync(cmd.CodForm))
            throw new KeyNotFoundException($"Plantilla {cmd.CodForm} no encontrada.");

        var pasos = cmd.Pasos
            .Select(p => PlantillaFlujoPaso.Crear(
                Guid.NewGuid(),
                cmd.CodForm,
                p.Orden,
                Enum.Parse<TipoAccionFlujo>(p.TipoAccion, ignoreCase: true),
                Enum.Parse<ResponsableFlujoTipo>(p.ResponsableTipo, ignoreCase: true),
                p.ResponsableId,
                p.Obligatorio))
            .ToList();

        await _flujo.ReemplazarAsync(cmd.CodForm, pasos, ct);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "PlantillaFlujoActualizado",
            "PlantillaFlujoPaso",
            cmd.CodForm,
            $"Flujo actualizado: {pasos.Count} paso(s)."));

        return await BuildDtosAsync(pasos, ct);
    }

    private async Task<IReadOnlyList<PlantillaFlujoPasoDto>> BuildDtosAsync(
        IReadOnlyList<PlantillaFlujoPaso> pasos, CancellationToken ct)
    {
        var nombresPorTipo = new Dictionary<ResponsableFlujoTipo, IReadOnlyDictionary<Guid, string>>();
        foreach (var grupo in pasos.GroupBy(p => p.ResponsableTipo))
        {
            var ids = grupo.Select(p => p.ResponsableId).Distinct().ToList();
            nombresPorTipo[grupo.Key] = await _nombres.ResolverNombresAsync(grupo.Key, ids, ct);
        }

        return pasos
            .OrderBy(p => p.Orden)
            .Select(p =>
            {
                string? nombre = null;
                if (nombresPorTipo.TryGetValue(p.ResponsableTipo, out var mapa)
                    && mapa.TryGetValue(p.ResponsableId, out var resuelto))
                {
                    nombre = resuelto;
                }

                return new PlantillaFlujoPasoDto(
                    p.Id, p.Orden, p.TipoAccion.ToString(), p.ResponsableTipo.ToString(),
                    p.ResponsableId, nombre, p.Obligatorio);
            })
            .ToList();
    }
}
