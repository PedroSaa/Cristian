using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using DocFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocFlow.Infrastructure.Repositories;

public class PlantillaFlujoRepository : IPlantillaFlujoRepository
{
    private readonly DocFlowDbContext _db;

    public PlantillaFlujoRepository(DocFlowDbContext db) => _db = db;

    public async Task<IReadOnlyList<PlantillaFlujoPaso>> GetByCodFormAsync(
        string codForm, CancellationToken ct = default)
    {
        return await _db.PlantillaFlujoPasos
            .AsNoTracking()
            .Where(p => p.CodForm == codForm)
            .OrderBy(p => p.Orden)
            .ToListAsync(ct);
    }

    public async Task ReemplazarAsync(
        string codForm, IEnumerable<PlantillaFlujoPaso> pasos, CancellationToken ct = default)
    {
        var existentes = await _db.PlantillaFlujoPasos
            .Where(p => p.CodForm == codForm)
            .ToListAsync(ct);

        _db.PlantillaFlujoPasos.RemoveRange(existentes);
        await _db.PlantillaFlujoPasos.AddRangeAsync(pasos, ct);

        // A single SaveChanges runs in one implicit transaction: deletes are ordered
        // before inserts, so the whole workflow replacement is atomic.
        await _db.SaveChangesAsync(ct);
    }
}
