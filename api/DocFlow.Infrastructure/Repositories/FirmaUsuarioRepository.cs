using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using DocFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocFlow.Infrastructure.Repositories;

public class FirmaUsuarioRepository : IFirmaUsuarioRepository
{
    private readonly DocFlowDbContext _db;

    public FirmaUsuarioRepository(DocFlowDbContext db) => _db = db;

    public async Task<FirmaUsuario?> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
        => await _db.FirmasUsuario.FirstOrDefaultAsync(f => f.UsuarioId == usuarioId, ct);

    public async Task UpsertAsync(FirmaUsuario firma, CancellationToken ct = default)
    {
        // A signature loaded through GetByUsuarioAsync in this scope is tracked (replacement path):
        // only add it when it is a brand new, detached instance.
        if (_db.Entry(firma).State == EntityState.Detached)
            _db.FirmasUsuario.Add(firma);

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var firma = await _db.FirmasUsuario.FirstOrDefaultAsync(f => f.UsuarioId == usuarioId, ct);
        if (firma is null)
            return;

        _db.FirmasUsuario.Remove(firma);
        await _db.SaveChangesAsync(ct);
    }
}
