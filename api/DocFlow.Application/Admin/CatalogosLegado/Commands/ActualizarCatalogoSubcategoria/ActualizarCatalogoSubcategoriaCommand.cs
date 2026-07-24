using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarCatalogoSubcategoria;

public record ActualizarCatalogoSubcategoriaCommand(
    int CatCod,
    short IdSubcategoria,
    string SubcatNombre,
    string? SubcatDescripcion) : IRequest;

public class ActualizarCatalogoSubcategoriaCommandValidator : AbstractValidator<ActualizarCatalogoSubcategoriaCommand>
{
    public ActualizarCatalogoSubcategoriaCommandValidator()
    {
        RuleFor(x => x.CatCod)
            .GreaterThan(0).WithMessage("El código de categoría es obligatorio.");

        RuleFor(x => x.IdSubcategoria)
            .GreaterThan((short)0).WithMessage("El identificador de subcategoría es obligatorio.");

        RuleFor(x => x.SubcatNombre)
            .NotEmpty().WithMessage("El nombre de la subcategoría es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.SubcatDescripcion)
            .MaximumLength(200).WithMessage("La descripción no puede superar los 200 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.SubcatDescripcion));
    }
}

public class ActualizarCatalogoSubcategoriaCommandHandler : IRequestHandler<ActualizarCatalogoSubcategoriaCommand>
{
    private readonly ICatalogoSubcategoriaRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActualizarCatalogoSubcategoriaCommandHandler> _logger;

    public ActualizarCatalogoSubcategoriaCommandHandler(
        ICatalogoSubcategoriaRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<ActualizarCatalogoSubcategoriaCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(ActualizarCatalogoSubcategoriaCommand cmd, CancellationToken ct)
    {
        var subcategoria = await _repo.GetByIdAsync(cmd.CatCod, cmd.IdSubcategoria)
            ?? throw new KeyNotFoundException($"Subcategoría {cmd.CatCod}-{cmd.IdSubcategoria} no encontrada.");

        subcategoria.Actualizar(cmd.SubcatNombre, cmd.SubcatDescripcion);
        await _repo.UpdateAsync(subcategoria);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "ActualizarCatalogoSubcategoria",
            "CatalogoSubcategoria",
            $"{subcategoria.CatCod}-{subcategoria.IdSubcategoria}",
            $"Subcategoría actualizada: {subcategoria.SubcatNombre}"));
    }
}
