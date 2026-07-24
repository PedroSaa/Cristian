using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.Numeracion.Commands;
using DocFlow.Application.Numeracion.DTOs;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Entities.NumeracionesDocumento;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Numeracion.Commands.CreatePlantilla;

public record CreatePlantillaCommand(
    int Id,
    string Descripcion,
    string Patron,
    bool PorOrganismo = false,
    bool PorTipoDocumento = false,
    bool PorFormatoDocumento = false,
    string Periodicidad = "CONTINUO",
    string MomentoGeneracion = "AL_INGRESAR",
    int RellenoCeros = 0,
    int ValorInicial = 0
) : IRequest<PlantillaNumeracionDto>;

public class CreatePlantillaHandler : IRequestHandler<CreatePlantillaCommand, PlantillaNumeracionDto>
{
    private readonly IPlantillaService _service;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public CreatePlantillaHandler(IPlantillaService service, IAuditoriaRepository auditoria, ICurrentUser currentUser)
    {
        _service = service;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<PlantillaNumeracionDto> Handle(CreatePlantillaCommand cmd, CancellationToken ct)
    {
        var actorId = _currentUser.RequireAuthenticatedUserId();
        var plantilla = await _service.CrearAsync(cmd.Id, cmd.Descripcion, cmd.Patron,
            cmd.PorOrganismo, cmd.PorTipoDocumento, cmd.PorFormatoDocumento,
            cmd.Periodicidad, cmd.MomentoGeneracion, cmd.RellenoCeros, cmd.ValorInicial, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            actorId,
            "CrearPlantillaNumeracion",
            "PlantillaNumeracion",
            plantilla.Id.ToString(),
            NumeracionAuditDetails.PlantillaCreated(plantilla)));

        return PlantillaNumeracionDto.From(plantilla);
    }
}

public class CreatePlantillaValidator : AbstractValidator<CreatePlantillaCommand>
{
    public CreatePlantillaValidator()
    {
        // El Id se autogenera en el servicio (máx + 1) cuando llega 0; no se valida > 0.
        RuleFor(x => x.Descripcion).NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(255);
        RuleFor(x => x.Patron).NotEmpty().WithMessage("El patrón es obligatorio.")
            .MaximumLength(200)
            .Must(p => PatronNumeracion.EsValido(p, out _))
            .WithMessage((_, p) => { PatronNumeracion.EsValido(p, out var e); return e ?? "El patrón no es válido."; });
        RuleFor(x => x.Periodicidad)
            .Must(p => PlantillaNumeracion.PeriodicidadesValidas.Contains((p ?? "").Trim().ToUpperInvariant()))
            .WithMessage("Periodicidad debe ser CONTINUO, ANUAL o MENSUAL.");
        RuleFor(x => x.MomentoGeneracion)
            .Must(m => PlantillaNumeracion.MomentosValidos.Contains((m ?? "").Trim().ToUpperInvariant()))
            .WithMessage("El momento debe ser AL_INGRESAR, AL_FIRMAR, AMBOS o MANUAL.");
        RuleFor(x => x.RellenoCeros).InclusiveBetween(0, 20).WithMessage("El relleno de ceros debe estar entre 0 y 20.");
        RuleFor(x => x.ValorInicial).GreaterThanOrEqualTo(0).WithMessage("El valor inicial no puede ser negativo.");
    }
}
