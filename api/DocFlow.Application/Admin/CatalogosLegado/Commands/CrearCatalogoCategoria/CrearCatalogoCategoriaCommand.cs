using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.CrearCatalogoCategoria;

public record CrearCatalogoCategoriaCommand(string CatDesc) : IRequest<CatalogoCategoriaDto>;

public class CrearCatalogoCategoriaCommandValidator : AbstractValidator<CrearCatalogoCategoriaCommand>
{
    public CrearCatalogoCategoriaCommandValidator()
    {
        RuleFor(x => x.CatDesc)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(60).WithMessage("La descripción no puede superar los 60 caracteres.");
    }
}

public class CrearCatalogoCategoriaCommandHandler : IRequestHandler<CrearCatalogoCategoriaCommand, CatalogoCategoriaDto>
{
    private readonly ICatalogoCategoriaRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CrearCatalogoCategoriaCommandHandler> _logger;

    public CrearCatalogoCategoriaCommandHandler(
        ICatalogoCategoriaRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<CrearCatalogoCategoriaCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<CatalogoCategoriaDto> Handle(CrearCatalogoCategoriaCommand cmd, CancellationToken ct)
    {
        var next = await _repo.GetProximoIdAsync();
        var categoria = new CatalogoCategoria(next, cmd.CatDesc);
        await _repo.CreateAsync(categoria);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "CrearCatalogoCategoria",
            "CatalogoCategoria",
            categoria.CatCod.ToString(),
            $"Categoría creada: {categoria.CatDesc}"));

        return new CatalogoCategoriaDto(categoria.CatCod, categoria.CatDesc, 0);
    }
}
