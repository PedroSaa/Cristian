using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeFormaEnvio;

public record CrearSeFormaEnvioCommand(string FormaEnvio) : IRequest<SeFormaEnvioDto>;

public class CrearSeFormaEnvioCommandValidator : AbstractValidator<CrearSeFormaEnvioCommand>
{
    public CrearSeFormaEnvioCommandValidator()
    {
        RuleFor(x => x.FormaEnvio)
            .NotEmpty().WithMessage("La forma de envío es obligatoria.")
            .MaximumLength(50).WithMessage("La forma de envío no puede superar los 50 caracteres.");
    }
}

public class CrearSeFormaEnvioCommandHandler : IRequestHandler<CrearSeFormaEnvioCommand, SeFormaEnvioDto>
{
    private readonly ISeFormaEnvioRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CrearSeFormaEnvioCommandHandler> _logger;

    public CrearSeFormaEnvioCommandHandler(
        ISeFormaEnvioRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<CrearSeFormaEnvioCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<SeFormaEnvioDto> Handle(CrearSeFormaEnvioCommand cmd, CancellationToken ct)
    {
        var next = await _repo.GetProximoIdAsync();
        var entity = new SeFormaEnvio(next, cmd.FormaEnvio);
        await _repo.CreateAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "CrearSeFormaEnvio",
            "SeFormaEnvio",
            entity.IdFormaEnvio.ToString(),
            $"Forma de envío creada: {entity.FormaEnvio}"));

        return new SeFormaEnvioDto(entity.IdFormaEnvio, entity.FormaEnvio);
    }
}
