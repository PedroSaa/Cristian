using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeFormaEnvio;

public record ActualizarSeFormaEnvioCommand(short IdFormaEnvio, string FormaEnvio) : IRequest;

public class ActualizarSeFormaEnvioCommandValidator : AbstractValidator<ActualizarSeFormaEnvioCommand>
{
    public ActualizarSeFormaEnvioCommandValidator()
    {
        RuleFor(x => x.IdFormaEnvio).GreaterThan((short)0).WithMessage("El identificador de forma de envío es obligatorio.");

        RuleFor(x => x.FormaEnvio)
            .NotEmpty().WithMessage("La forma de envío es obligatoria.")
            .MaximumLength(50).WithMessage("La forma de envío no puede superar los 50 caracteres.");
    }
}

public class ActualizarSeFormaEnvioCommandHandler : IRequestHandler<ActualizarSeFormaEnvioCommand>
{
    private readonly ISeFormaEnvioRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActualizarSeFormaEnvioCommandHandler> _logger;

    public ActualizarSeFormaEnvioCommandHandler(
        ISeFormaEnvioRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<ActualizarSeFormaEnvioCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(ActualizarSeFormaEnvioCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.IdFormaEnvio)
            ?? throw new KeyNotFoundException($"Forma de envío {cmd.IdFormaEnvio} no encontrada.");

        entity.Actualizar(cmd.FormaEnvio);
        await _repo.UpdateAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "ActualizarSeFormaEnvio",
            "SeFormaEnvio",
            entity.IdFormaEnvio.ToString(),
            $"Forma de envío actualizada: {entity.FormaEnvio}"));
    }
}
