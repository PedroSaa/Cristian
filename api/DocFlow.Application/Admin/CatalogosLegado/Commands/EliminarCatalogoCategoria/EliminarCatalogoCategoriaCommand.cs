using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarCatalogoCategoria;

public record EliminarCatalogoCategoriaCommand(int CatCod) : IRequest;

public class EliminarCatalogoCategoriaCommandHandler : IRequestHandler<EliminarCatalogoCategoriaCommand>
{
    private readonly ICatalogoCategoriaRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EliminarCatalogoCategoriaCommandHandler> _logger;

    public EliminarCatalogoCategoriaCommandHandler(
        ICatalogoCategoriaRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<EliminarCatalogoCategoriaCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(EliminarCatalogoCategoriaCommand cmd, CancellationToken ct)
    {
        var categoria = await _repo.GetByIdAsync(cmd.CatCod)
            ?? throw new KeyNotFoundException($"Categoría {cmd.CatCod} no encontrada.");

        if (categoria.Subcategorias.Count != 0)
            throw new InvalidOperationException("No se puede eliminar una categoría con subcategorías asociadas.");

        await _repo.DeleteAsync(categoria);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "EliminarCatalogoCategoria",
            "CatalogoCategoria",
            categoria.CatCod.ToString(),
            $"Categoría eliminada: {categoria.CatDesc}"));
    }
}
