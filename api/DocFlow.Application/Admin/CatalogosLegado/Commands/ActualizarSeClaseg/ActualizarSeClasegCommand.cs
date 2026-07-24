using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeClaseg;

public record ActualizarSeClasegCommand(short DFClasif, string DFNCLASIF, string DFDClasif) : IRequest;

public class ActualizarSeClasegCommandValidator : AbstractValidator<ActualizarSeClasegCommand>
{
    public ActualizarSeClasegCommandValidator()
    {
        RuleFor(x => x.DFClasif).GreaterThan((short)0).WithMessage("El código de clasificación es obligatorio.");

        RuleFor(x => x.DFNCLASIF)
            .NotEmpty().WithMessage("La sigla es obligatoria.")
            .MaximumLength(2).WithMessage("La sigla no puede superar los 2 caracteres.");

        RuleFor(x => x.DFDClasif)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(15).WithMessage("El nombre no puede superar los 15 caracteres.");
    }
}

public class ActualizarSeClasegCommandHandler : IRequestHandler<ActualizarSeClasegCommand>
{
    private readonly ISeClasegRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActualizarSeClasegCommandHandler> _logger;

    public ActualizarSeClasegCommandHandler(
        ISeClasegRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<ActualizarSeClasegCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(ActualizarSeClasegCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.DFClasif)
            ?? throw new KeyNotFoundException($"Clasificación {cmd.DFClasif} no encontrada.");

        entity.Actualizar(cmd.DFNCLASIF, cmd.DFDClasif);
        await _repo.UpdateAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "ActualizarSeClaseg",
            "SeClaseg",
            entity.DFClasif.ToString(),
            $"Clasificación actualizada: {entity.DFDClasif}"));
    }
}
