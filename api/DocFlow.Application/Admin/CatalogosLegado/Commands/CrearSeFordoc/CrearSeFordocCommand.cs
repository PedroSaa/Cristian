using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.Common.Time;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeFordoc;

public record CrearSeFordocCommand(
    short TipoRec,
    short TipoInt,
    string TipoDesc,
    int CorrN,
    int? TipoEnv,
    short SeFordocVistaI,
    short SeFordocVistaE,
    short SeFordocVistaR,
    string? SeFordocFormatoNum) : IRequest<SeFordocDto>;

public class CrearSeFordocCommandValidator : AbstractValidator<CrearSeFordocCommand>
{
    public CrearSeFordocCommandValidator()
    {
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

public class CrearSeFordocCommandHandler : IRequestHandler<CrearSeFordocCommand, SeFordocDto>
{
    private readonly ISeFordocRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CrearSeFordocCommandHandler> _logger;
    private readonly TimeProvider _timeProvider;

    public CrearSeFordocCommandHandler(
        ISeFordocRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<CrearSeFordocCommandHandler> logger,
        TimeProvider timeProvider)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<SeFordocDto> Handle(CrearSeFordocCommand cmd, CancellationToken ct)
    {
        if (cmd.TipoRec < 0)
            throw new ValidationException("El tipo de recepción no puede ser negativo.");

        if (cmd.TipoInt < 0)
            throw new ValidationException("El tipo interno no puede ser negativo.");

        if (string.IsNullOrWhiteSpace(cmd.TipoDesc))
            throw new ValidationException("La descripción del formato es obligatoria.");

        var next = await _repo.GetProximoIdAsync();
        var corrFecha = ChileClock.Today(_timeProvider);
        var entity = new SeFordoc(next, cmd.TipoRec, cmd.TipoInt, cmd.TipoDesc, cmd.CorrN, corrFecha, cmd.TipoEnv, cmd.SeFordocVistaI, cmd.SeFordocVistaE, cmd.SeFordocVistaR, cmd.SeFordocFormatoNum);
        await _repo.CreateAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "CrearSeFordoc",
            "SeFordoc",
            entity.TipoCod.ToString(),
            $"Formato de documento creado: {entity.TipoDesc}"));

        return new SeFordocDto(entity.TipoCod, entity.TipoRec, entity.TipoInt, entity.TipoDesc, entity.CorrN, entity.CorrFecha, entity.TipoEnv, entity.SeFordocVistaI, entity.SeFordocVistaE, entity.SeFordocVistaR, entity.SeFordocFormatoNum);
    }
}
