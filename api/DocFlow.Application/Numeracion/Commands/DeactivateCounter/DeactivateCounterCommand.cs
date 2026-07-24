using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.Numeracion.Commands;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Numeracion.Commands.DeactivateCounter;

public record DeactivateCounterCommand(Guid Id) : IRequest;

public class DeactivateCounterValidator : AbstractValidator<DeactivateCounterCommand>
{
    public DeactivateCounterValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID del contador es obligatorio.");
    }
}

public class DeactivateCounterHandler : IRequestHandler<DeactivateCounterCommand>
{
    private readonly ICounterService _counterService;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public DeactivateCounterHandler(ICounterService counterService, IAuditoriaRepository auditoria, ICurrentUser currentUser)
    {
        _counterService = counterService;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task Handle(DeactivateCounterCommand cmd, CancellationToken ct)
    {
        var actorId = _currentUser.RequireAuthenticatedUserId();
        var before = await _counterService.GetByIdAsync(cmd.Id, ct);
        await _counterService.DeactivateCounterAsync(cmd.Id, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            actorId,
            "DesactivarContadorNumeracion",
            "ContadorNumeracion",
            before.Id.ToString(),
            NumeracionAuditDetails.CounterActiveChanged(before, activoDespues: false)));
    }
}
