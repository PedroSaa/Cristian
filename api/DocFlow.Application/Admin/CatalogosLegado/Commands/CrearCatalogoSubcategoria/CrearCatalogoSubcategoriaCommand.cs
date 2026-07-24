using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.CrearCatalogoSubcategoria;

public record CrearCatalogoSubcategoriaCommand(
    int CatCod,
    string SubcatNombre,
    string? SubcatDescripcion) : IRequest<CatalogoSubcategoriaDto>;

public class CrearCatalogoSubcategoriaCommandValidator : AbstractValidator<CrearCatalogoSubcategoriaCommand>
{
    public CrearCatalogoSubcategoriaCommandValidator()
    {
        RuleFor(x => x.CatCod)
            .GreaterThan(0).WithMessage("El código de categoría es obligatorio.");

        RuleFor(x => x.SubcatNombre)
            .NotEmpty().WithMessage("El nombre de la subcategoría es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.SubcatDescripcion)
            .MaximumLength(200).WithMessage("La descripción no puede superar los 200 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.SubcatDescripcion));
    }
}

public class CrearCatalogoSubcategoriaCommandHandler : IRequestHandler<CrearCatalogoSubcategoriaCommand, CatalogoSubcategoriaDto>
{
    private readonly ICatalogoCategoriaRepository _categoriaRepo;
    private readonly ICatalogoSubcategoriaRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CrearCatalogoSubcategoriaCommandHandler> _logger;

    public CrearCatalogoSubcategoriaCommandHandler(
        ICatalogoCategoriaRepository categoriaRepo,
        ICatalogoSubcategoriaRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<CrearCatalogoSubcategoriaCommandHandler> logger)
    {
        _categoriaRepo = categoriaRepo;
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<CatalogoSubcategoriaDto> Handle(CrearCatalogoSubcategoriaCommand cmd, CancellationToken ct)
    {
        var categoria = await _categoriaRepo.GetByIdAsync(cmd.CatCod)
            ?? throw new KeyNotFoundException($"Categoría {cmd.CatCod} no encontrada.");

        var next = await _repo.GetProximoIdSubcategoriaAsync(cmd.CatCod);
        var subcategoria = new CatalogoSubcategoria(cmd.CatCod, next, cmd.SubcatNombre, cmd.SubcatDescripcion);

        await _repo.CreateAsync(subcategoria);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "CrearCatalogoSubcategoria",
            "CatalogoSubcategoria",
            $"{subcategoria.CatCod}-{subcategoria.IdSubcategoria}",
            $"Subcategoría creada: {subcategoria.SubcatNombre}"));

        return new CatalogoSubcategoriaDto(categoria.CatCod, categoria.CatDesc, subcategoria.IdSubcategoria, subcategoria.SubcatNombre, subcategoria.SubcatDescripcion);
    }
}
