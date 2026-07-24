using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeTiptar;

public record ActualizarSeTiptarCommand(string DFTACCION, string? DFTACOBSV, string? DFTACDESC) : IRequest;

public class ActualizarSeTiptarCommandValidator : AbstractValidator<ActualizarSeTiptarCommand>
{
    public ActualizarSeTiptarCommandValidator()
    {
        RuleFor(x => x.DFTACCION)
            .NotEmpty().WithMessage("La acción de tarea es obligatoria.")
            .MaximumLength(30).WithMessage("La acción de tarea no puede superar los 30 caracteres.");

        RuleFor(x => x.DFTACDESC)
            .MaximumLength(60).WithMessage("La descripción no puede superar los 60 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.DFTACDESC));
    }
}

public class ActualizarSeTiptarCommandHandler : IRequestHandler<ActualizarSeTiptarCommand>
{
    private readonly ISeTiptarRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActualizarSeTiptarCommandHandler> _logger;

    public ActualizarSeTiptarCommandHandler(
        ISeTiptarRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<ActualizarSeTiptarCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(ActualizarSeTiptarCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.DFTACCION)
            ?? throw new KeyNotFoundException($"Acción de tarea {cmd.DFTACCION} no encontrada.");

        entity.Actualizar(cmd.DFTACOBSV, cmd.DFTACDESC);
        await _repo.UpdateAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "ActualizarSeTiptar",
            "SeTiptar",
            entity.DFTACCION,
            $"Acción de tarea actualizada: {entity.DFTACCION}"));
    }
}
