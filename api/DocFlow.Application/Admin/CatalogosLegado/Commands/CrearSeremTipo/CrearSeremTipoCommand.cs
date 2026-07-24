using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeremTipo;

public record CrearSeremTipoCommand(string RemTipo, string RemDesc) : IRequest<SeremTipoDto>;

public class CrearSeremTipoCommandValidator : AbstractValidator<CrearSeremTipoCommand>
{
    public CrearSeremTipoCommandValidator()
    {
        RuleFor(x => x.RemTipo)
            .NotEmpty().WithMessage("El tipo de remitente es obligatorio.")
            .MaximumLength(3).WithMessage("El tipo de remitente no puede superar los 3 caracteres.");

        RuleFor(x => x.RemDesc)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(30).WithMessage("La descripción no puede superar los 30 caracteres.");
    }
}

public class CrearSeremTipoCommandHandler : IRequestHandler<CrearSeremTipoCommand, SeremTipoDto>
{
    private readonly ISeremTipoRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CrearSeremTipoCommandHandler> _logger;

    public CrearSeremTipoCommandHandler(
        ISeremTipoRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<CrearSeremTipoCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<SeremTipoDto> Handle(CrearSeremTipoCommand cmd, CancellationToken ct)
    {
        if (await _repo.ExistsAsync(cmd.RemTipo))
            throw new InvalidOperationException($"Ya existe un tipo de remitente con el código {cmd.RemTipo}.");

        var tipo = new SeremTipo(cmd.RemTipo, cmd.RemDesc);
        await _repo.CreateAsync(tipo);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "CrearSeremTipo",
            "SeremTipo",
            tipo.RemTipo,
            $"Tipo de remitente creado: {tipo.RemDesc}"));

        return new SeremTipoDto(tipo.RemTipo, tipo.RemDesc, 0);
    }
}
