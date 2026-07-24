using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeTiptar;

public record CrearSeTiptarCommand(string DFTACCION, string? DFTACOBSV, string? DFTACDESC) : IRequest<SeTiptarDto>;

public class CrearSeTiptarCommandValidator : AbstractValidator<CrearSeTiptarCommand>
{
    public CrearSeTiptarCommandValidator()
    {
        RuleFor(x => x.DFTACCION)
            .NotEmpty().WithMessage("La acción de tarea es obligatoria.")
            .MaximumLength(30).WithMessage("La acción de tarea no puede superar los 30 caracteres.");

        RuleFor(x => x.DFTACDESC)
            .MaximumLength(60).WithMessage("La descripción no puede superar los 60 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.DFTACDESC));
    }
}

public class CrearSeTiptarCommandHandler : IRequestHandler<CrearSeTiptarCommand, SeTiptarDto>
{
    private readonly ISeTiptarRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CrearSeTiptarCommandHandler> _logger;

    public CrearSeTiptarCommandHandler(
        ISeTiptarRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<CrearSeTiptarCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<SeTiptarDto> Handle(CrearSeTiptarCommand cmd, CancellationToken ct)
    {
        if (await _repo.ExistsAsync(cmd.DFTACCION))
            throw new InvalidOperationException($"Ya existe una acción de tarea con el código {cmd.DFTACCION}.");

        var entity = new SeTiptar(cmd.DFTACCION, cmd.DFTACOBSV, cmd.DFTACDESC);
        await _repo.CreateAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "CrearSeTiptar",
            "SeTiptar",
            entity.DFTACCION,
            $"Acción de tarea creada: {entity.DFTACCION}"));

        return new SeTiptarDto(entity.DFTACCION, entity.DFTACOBSV, entity.DFTACDESC);
    }
}
