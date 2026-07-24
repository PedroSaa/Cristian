using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeCorfor;

public record CrearSeCorforCommand(string CorrTip, int CorrNro, string CorrDes, DateTime CorrFch) : IRequest<SeCorforDto>;

public class CrearSeCorforCommandValidator : AbstractValidator<CrearSeCorforCommand>
{
    public CrearSeCorforCommandValidator()
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

public class CrearSeCorforCommandHandler : IRequestHandler<CrearSeCorforCommand, SeCorforDto>
{
    private readonly ISeCorforRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CrearSeCorforCommandHandler> _logger;

    public CrearSeCorforCommandHandler(
        ISeCorforRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<CrearSeCorforCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<SeCorforDto> Handle(CrearSeCorforCommand cmd, CancellationToken ct)
    {
        if (await _repo.ExistsAsync(cmd.CorrTip))
            throw new InvalidOperationException($"Ya existe un correlativo con el tipo {cmd.CorrTip}.");

        var entity = new SeCorfor(cmd.CorrTip, cmd.CorrNro, cmd.CorrDes, cmd.CorrFch);
        await _repo.CreateAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "CrearSeCorfor",
            "SeCorfor",
            entity.CorrTip,
            $"Correlativo creado: {entity.CorrDes}"));

        return new SeCorforDto(entity.CorrTip, entity.CorrNro, entity.CorrDes, entity.CorrFch);
    }
}
