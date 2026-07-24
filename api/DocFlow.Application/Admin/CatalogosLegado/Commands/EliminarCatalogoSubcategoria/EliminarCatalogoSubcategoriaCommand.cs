using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarCatalogoSubcategoria;

public record EliminarCatalogoSubcategoriaCommand(int CatCod, short IdSubcategoria) : IRequest;

public class EliminarCatalogoSubcategoriaCommandHandler : IRequestHandler<EliminarCatalogoSubcategoriaCommand>
{
    private readonly ICatalogoSubcategoriaRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EliminarCatalogoSubcategoriaCommandHandler> _logger;

    public EliminarCatalogoSubcategoriaCommandHandler(
        ICatalogoSubcategoriaRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<EliminarCatalogoSubcategoriaCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(EliminarCatalogoSubcategoriaCommand cmd, CancellationToken ct)
    {
        var subcategoria = await _repo.GetByIdAsync(cmd.CatCod, cmd.IdSubcategoria)
            ?? throw new KeyNotFoundException($"Subcategoría {cmd.CatCod}-{cmd.IdSubcategoria} no encontrada.");

        await _repo.DeleteAsync(subcategoria);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "EliminarCatalogoSubcategoria",
            "CatalogoSubcategoria",
            $"{subcategoria.CatCod}-{subcategoria.IdSubcategoria}",
            $"Subcategoría eliminada: {subcategoria.SubcatNombre}"));
    }
}
