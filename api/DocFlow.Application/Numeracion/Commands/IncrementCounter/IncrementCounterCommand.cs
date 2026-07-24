using DocFlow.Application.Numeracion.DTOs;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Numeracion.Commands.IncrementCounter;

public record IncrementCounterCommand(
    Guid Id,
    string? Periodicidad = null
) : IRequest<NextValueResultDto>;

public class IncrementCounterValidator : AbstractValidator<IncrementCounterCommand>
{
    private static readonly string[] ValidPeriodicidades = ["CONTINUO", "ANUAL", "MENSUAL"];

    public IncrementCounterValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID del contador es obligatorio.");

        RuleFor(x => x.Periodicidad)
            .Must(p => p is null || ValidPeriodicidades.Contains(p))
            .WithMessage("Periodicidad debe ser CONTINUO, ANUAL o MENSUAL.");
    }
}

public class IncrementCounterHandler : IRequestHandler<IncrementCounterCommand, NextValueResultDto>
{
    private readonly ICounterService _counterService;

    public IncrementCounterHandler(ICounterService counterService)
        => _counterService = counterService;

    public async Task<NextValueResultDto> Handle(IncrementCounterCommand cmd, CancellationToken ct)
    {
        // Fetch the entity to get key dimensions
        var entity = await _counterService.GetByIdAsync(cmd.Id, ct);

        if (!entity.Activo)
            throw new InvalidOperationException(
                $"El contador con id {cmd.Id} está desactivado.");

        var key = new CounterKey(
            entity.CodigoContador,
            entity.OrgDepCod,
            entity.NivelCod,
            entity.TipoCod,
            entity.DfTipo);

        var periodicidad = cmd.Periodicidad ?? entity.Periodicidad;
        var valor = await _counterService.NextValueAsync(key, periodicidad, ct);

        return new NextValueResultDto(valor);
    }
}
