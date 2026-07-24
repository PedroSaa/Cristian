using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.Numeracion.Commands;
using DocFlow.Application.Numeracion.DTOs;
using DocFlow.Application.Numeracion.Mappings;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Numeracion.Commands.SetCounterValue;

public record SetCounterValueCommand(Guid Id, long Valor) : IRequest<CounterDto>;

public class SetCounterValueValidator : AbstractValidator<SetCounterValueCommand>
{
    public SetCounterValueValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID del contador es obligatorio.");

        RuleFor(x => x.Valor)
            .GreaterThanOrEqualTo(0).WithMessage("El valor no puede ser negativo.");
    }
}

public class SetCounterValueHandler : IRequestHandler<SetCounterValueCommand, CounterDto>
{
    private readonly ICounterService _counterService;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public SetCounterValueHandler(ICounterService counterService, IAuditoriaRepository auditoria, ICurrentUser currentUser)
    {
        _counterService = counterService;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<CounterDto> Handle(SetCounterValueCommand cmd, CancellationToken ct)
    {
        var actorId = _currentUser.RequireAuthenticatedUserId();
        var before = await _counterService.GetByIdAsync(cmd.Id, ct);
        var entity = await _counterService.SetCounterValueAsync(cmd.Id, cmd.Valor, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            actorId,
            "ActualizarValorContadorNumeracion",
            "ContadorNumeracion",
            entity.Id.ToString(),
            NumeracionAuditDetails.CounterValueChanged(before, entity)));

        return entity.ToDto();
    }
}
