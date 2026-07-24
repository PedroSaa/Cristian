using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.Numeracion.Commands;
using DocFlow.Application.Numeracion.DTOs;
using DocFlow.Application.Numeracion.Mappings;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Numeracion.Commands.CreateCounter;

public record CreateCounterCommand(
    string CodigoContador,
    string OrgDepCod,
    int TipoCod = 0,
    string DfTipo = "",
    string NivelCod = "",
    string Periodicidad = "CONTINUO",
    long ValorInicial = 0
) : IRequest<CounterDto>;

public class CreateCounterValidator : AbstractValidator<CreateCounterCommand>
{
    public CreateCounterValidator()
    {
        RuleFor(x => x.CodigoContador)
            .NotEmpty().WithMessage("El código de contador es obligatorio.")
            .MaximumLength(50).WithMessage("El código de contador no puede superar los 50 caracteres.");

        RuleFor(x => x.OrgDepCod)
            .NotEmpty().WithMessage("El código de organización es obligatorio.")
            .MaximumLength(20).WithMessage("El código de organización no puede superar los 20 caracteres.");

        RuleFor(x => x.Periodicidad)
            .NotEmpty().WithMessage("La periodicidad es obligatoria.")
            // Case-insensitive: el handler la normaliza a mayúsculas antes de guardar.
            .Must(p => !string.IsNullOrWhiteSpace(p) && p.Trim().ToUpperInvariant() is "CONTINUO" or "ANUAL" or "MENSUAL")
            .WithMessage("Periodicidad debe ser CONTINUO, ANUAL o MENSUAL.");

        RuleFor(x => x.ValorInicial)
            .GreaterThanOrEqualTo(0).WithMessage("El valor inicial no puede ser negativo.");

        RuleFor(x => x.NivelCod)
            .MaximumLength(20).WithMessage("El nivel no puede superar los 20 caracteres.");

        RuleFor(x => x.DfTipo)
            .MaximumLength(1).WithMessage("El tipo DF no puede superar 1 carácter.");
    }
}

public class CreateCounterHandler : IRequestHandler<CreateCounterCommand, CounterDto>
{
    private readonly ICounterService _counterService;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ISeFordocRepository _formatos;

    public CreateCounterHandler(ICounterService counterService, IAuditoriaRepository auditoria, ICurrentUser currentUser, ISeFordocRepository formatos)
    {
        _counterService = counterService;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _formatos = formatos;
    }

    public async Task<CounterDto> Handle(CreateCounterCommand cmd, CancellationToken ct)
    {
        var actorId = _currentUser.RequireAuthenticatedUserId();

        // tipoCod 0 = "sin tipo" (válido). Si se especifica, debe existir en el catálogo
        // de formatos de documento (SEFORDOC). No hay FK en la BD, así que sin este chequeo
        // se podrían crear contadores huérfanos apuntando a un tipo inexistente.
        if (cmd.TipoCod != 0 &&
            (cmd.TipoCod < 1 || cmd.TipoCod > short.MaxValue || !await _formatos.ExistsAsync((short)cmd.TipoCod)))
        {
            throw new ValidationException($"No existe un formato de documento con el código {cmd.TipoCod}.");
        }

        var key = new Domain.ValueObjects.CounterKey(
            cmd.CodigoContador,
            cmd.OrgDepCod,
            cmd.NivelCod,
            cmd.TipoCod,
            cmd.DfTipo);

        var entity = await _counterService.CreateCounterAsync(
            key, cmd.ValorInicial, cmd.Periodicidad.Trim().ToUpperInvariant(), ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            actorId,
            "CrearContadorNumeracion",
            "ContadorNumeracion",
            entity.Id.ToString(),
            NumeracionAuditDetails.CounterCreated(entity)));

        return entity.ToDto();
    }
}
