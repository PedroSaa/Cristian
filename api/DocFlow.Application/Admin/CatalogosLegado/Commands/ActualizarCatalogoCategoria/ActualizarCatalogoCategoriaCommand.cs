using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarCatalogoCategoria;

public record ActualizarCatalogoCategoriaCommand(int CatCod, string CatDesc) : IRequest;

public class ActualizarCatalogoCategoriaCommandValidator : AbstractValidator<ActualizarCatalogoCategoriaCommand>
{
    public ActualizarCatalogoCategoriaCommandValidator()
    {
        RuleFor(x => x.CatCod)
            .GreaterThan(0).WithMessage("El código de categoría es obligatorio.");

        RuleFor(x => x.CatDesc)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(60).WithMessage("La descripción no puede superar los 60 caracteres.");
    }
}

public class ActualizarCatalogoCategoriaCommandHandler : IRequestHandler<ActualizarCatalogoCategoriaCommand>
{
    private readonly ICatalogoCategoriaRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActualizarCatalogoCategoriaCommandHandler> _logger;

    public ActualizarCatalogoCategoriaCommandHandler(
        ICatalogoCategoriaRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<ActualizarCatalogoCategoriaCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(ActualizarCatalogoCategoriaCommand cmd, CancellationToken ct)
    {
        var categoria = await _repo.GetByIdAsync(cmd.CatCod)
            ?? throw new KeyNotFoundException($"Categoría {cmd.CatCod} no encontrada.");

        categoria.Actualizar(cmd.CatDesc);
        await _repo.UpdateAsync(categoria);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "ActualizarCatalogoCategoria",
            "CatalogoCategoria",
            categoria.CatCod.ToString(),
            $"Categoría actualizada: {categoria.CatDesc}"));
    }
}
