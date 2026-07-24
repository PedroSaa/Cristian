using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeCorfor;

public record ActualizarSeCorforCommand(string CorrTip, int CorrNro, string CorrDes, DateTime CorrFch) : IRequest;

public class ActualizarSeCorforCommandValidator : AbstractValidator<ActualizarSeCorforCommand>
{
    public ActualizarSeCorforCommandValidator()
    {
        RuleFor(x => x.CorrTip)
            .NotEmpty().WithMessage("El tipo de correlativo es obligatorio.")
            .MaximumLength(8).WithMessage("El tipo de correlativo no puede superar los 8 caracteres.");

        RuleFor(x => x.CorrNro).GreaterThanOrEqualTo(0).WithMessage("El número de correlativo no puede ser negativo.");

        RuleFor(x => x.CorrDes)
            .NotEmpty().WithMessage("La descripción del correlativo es obligatoria.")
            .MaximumLength(60).WithMessage("La descripción del correlativo no puede superar los 60 caracteres.");
    }
}

public class ActualizarSeCorforCommandHandler : IRequestHandler<ActualizarSeCorforCommand>
{
    private readonly ISeCorforRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActualizarSeCorforCommandHandler> _logger;

    public ActualizarSeCorforCommandHandler(
        ISeCorforRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<ActualizarSeCorforCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(ActualizarSeCorforCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.CorrTip)
            ?? throw new KeyNotFoundException($"Correlativo {cmd.CorrTip} no encontrado.");

        entity.Actualizar(cmd.CorrNro, cmd.CorrDes, cmd.CorrFch);
        await _repo.UpdateAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "ActualizarSeCorfor",
            "SeCorfor",
            entity.CorrTip,
            $"Correlativo actualizado: {entity.CorrDes}"));
    }
}
