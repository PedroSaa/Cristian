using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeremTipo;

public record ActualizarSeremTipoCommand(string RemTipo, string RemDesc) : IRequest;

public class ActualizarSeremTipoCommandValidator : AbstractValidator<ActualizarSeremTipoCommand>
{
    public ActualizarSeremTipoCommandValidator()
    {
        RuleFor(x => x.RemTipo)
            .NotEmpty().WithMessage("El tipo de remitente es obligatorio.")
            .MaximumLength(3).WithMessage("El tipo de remitente no puede superar los 3 caracteres.");

        RuleFor(x => x.RemDesc)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(30).WithMessage("La descripción no puede superar los 30 caracteres.");
    }
}

public class ActualizarSeremTipoCommandHandler : IRequestHandler<ActualizarSeremTipoCommand>
{
    private readonly ISeremTipoRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActualizarSeremTipoCommandHandler> _logger;

    public ActualizarSeremTipoCommandHandler(
        ISeremTipoRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<ActualizarSeremTipoCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(ActualizarSeremTipoCommand cmd, CancellationToken ct)
    {
        var tipo = await _repo.GetByIdAsync(cmd.RemTipo)
            ?? throw new KeyNotFoundException($"Tipo de remitente {cmd.RemTipo} no encontrado.");

        tipo.Actualizar(cmd.RemDesc);
        await _repo.UpdateAsync(tipo);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "ActualizarSeremTipo",
            "SeremTipo",
            tipo.RemTipo,
            $"Tipo de remitente actualizado: {tipo.RemDesc}"));
    }
}
