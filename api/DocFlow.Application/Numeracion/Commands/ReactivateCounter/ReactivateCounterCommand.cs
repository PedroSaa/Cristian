using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.Numeracion.Commands;
using DocFlow.Application.Numeracion.DTOs;
using DocFlow.Application.Numeracion.Mappings;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Numeracion.Commands.ReactivateCounter;

public record ReactivateCounterCommand(Guid Id) : IRequest<CounterDto>;

public class ReactivateCounterValidator : AbstractValidator<ReactivateCounterCommand>
{
    public ReactivateCounterValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID del contador es obligatorio.");
    }
}

public class ReactivateCounterHandler : IRequestHandler<ReactivateCounterCommand, CounterDto>
{
    private readonly ICounterService _counterService;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public ReactivateCounterHandler(ICounterService counterService, IAuditoriaRepository auditoria, ICurrentUser currentUser)
    {
        _counterService = counterService;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<CounterDto> Handle(ReactivateCounterCommand cmd, CancellationToken ct)
    {
        var actorId = _currentUser.RequireAuthenticatedUserId();
        var before = await _counterService.GetByIdAsync(cmd.Id, ct);
        var entity = await _counterService.ReactivateCounterAsync(cmd.Id, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            actorId,
            "ReactivarContadorNumeracion",
            "ContadorNumeracion",
            entity.Id.ToString(),
            NumeracionAuditDetails.CounterActiveChanged(before, entity.Activo)));

        return entity.ToDto();
    }
}
