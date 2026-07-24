using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.Common.Time;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeFordoc;

public record ActualizarSeFordocCommand(
    short TipoCod,
    short TipoRec,
    short TipoInt,
    string TipoDesc,
    int CorrN,
    int? TipoEnv,
    short SeFordocVistaI,
    short SeFordocVistaE,
    short SeFordocVistaR,
    string? SeFordocFormatoNum) : IRequest;

public class ActualizarSeFordocCommandValidator : AbstractValidator<ActualizarSeFordocCommand>
{
    public ActualizarSeFordocCommandValidator()
    {
        RuleFor(x => x.TipoCod).GreaterThan((short)0).WithMessage("El código de formato de documento es obligatorio.");
        RuleFor(x => x.TipoRec).InclusiveBetween((short)0, (short)32767).WithMessage("El tipo de recepción debe estar entre 0 y 32767.");
        RuleFor(x => x.TipoInt).InclusiveBetween((short)0, (short)32767).WithMessage("El tipo interno debe estar entre 0 y 32767.");
        RuleFor(x => x.TipoDesc)
            .NotEmpty().WithMessage("La descripción del formato es obligatoria.")
            .MaximumLength(100).WithMessage("La descripción del formato no puede superar los 100 caracteres.");
        RuleFor(x => x.CorrN)
            .InclusiveBetween(0, 2147483647).WithMessage("El correlativo debe estar entre 0 y 2147483647.");
        RuleFor(x => x.SeFordocFormatoNum)
            .MaximumLength(40).WithMessage("El formato numérico no puede superar los 40 caracteres.");
    }
}

public class ActualizarSeFordocCommandHandler : IRequestHandler<ActualizarSeFordocCommand>
{
    private readonly ISeFordocRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActualizarSeFordocCommandHandler> _logger;
    private readonly TimeProvider _timeProvider;

    public ActualizarSeFordocCommandHandler(
        ISeFordocRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<ActualizarSeFordocCommandHandler> logger,
        TimeProvider timeProvider)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task Handle(ActualizarSeFordocCommand cmd, CancellationToken ct)
    {
        if (cmd.TipoRec < 0)
            throw new ValidationException("El tipo de recepción no puede ser negativo.");

        if (cmd.TipoInt < 0)
            throw new ValidationException("El tipo interno no puede ser negativo.");

        if (string.IsNullOrWhiteSpace(cmd.TipoDesc))
            throw new ValidationException("La descripción del formato es obligatoria.");

        var entity = await _repo.GetByIdAsync(cmd.TipoCod)
            ?? throw new KeyNotFoundException($"Formato de documento {cmd.TipoCod} no encontrado.");

        var corrFecha = ChileClock.Today(_timeProvider);
        entity.Actualizar(cmd.TipoRec, cmd.TipoInt, cmd.TipoDesc, cmd.CorrN, corrFecha, cmd.TipoEnv, cmd.SeFordocVistaI, cmd.SeFordocVistaE, cmd.SeFordocVistaR, cmd.SeFordocFormatoNum);
        await _repo.UpdateAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "ActualizarSeFordoc",
            "SeFordoc",
            entity.TipoCod.ToString(),
            $"Formato de documento actualizado: {entity.TipoDesc}"));
    }
}
