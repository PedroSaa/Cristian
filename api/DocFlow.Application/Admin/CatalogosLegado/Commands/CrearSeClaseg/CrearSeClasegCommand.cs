using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeClaseg;

public record CrearSeClasegCommand(string DFNCLASIF, string DFDClasif) : IRequest<SeClasegDto>;

public class CrearSeClasegCommandValidator : AbstractValidator<CrearSeClasegCommand>
{
    public CrearSeClasegCommandValidator()
    {
        RuleFor(x => x.DFNCLASIF)
            .NotEmpty().WithMessage("La sigla es obligatoria.")
            .MaximumLength(2).WithMessage("La sigla no puede superar los 2 caracteres.");

        RuleFor(x => x.DFDClasif)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(15).WithMessage("El nombre no puede superar los 15 caracteres.");
    }
}

public class CrearSeClasegCommandHandler : IRequestHandler<CrearSeClasegCommand, SeClasegDto>
{
    private readonly ISeClasegRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CrearSeClasegCommandHandler> _logger;

    public CrearSeClasegCommandHandler(
        ISeClasegRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<CrearSeClasegCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<SeClasegDto> Handle(CrearSeClasegCommand cmd, CancellationToken ct)
    {
        var next = await _repo.GetProximoIdAsync();
        var entity = new SeClaseg(next, cmd.DFNCLASIF, cmd.DFDClasif);
        await _repo.CreateAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "CrearSeClaseg",
            "SeClaseg",
            entity.DFClasif.ToString(),
            $"Clasificación creada: {entity.DFDClasif}"));

        return new SeClasegDto(entity.DFClasif, entity.DFNCLASIF, entity.DFDClasif);
    }
}
