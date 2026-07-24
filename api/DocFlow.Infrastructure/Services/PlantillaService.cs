using DocFlow.Domain.Entities.NumeracionesDocumento;
using DocFlow.Domain.Interfaces;
using DocFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocFlow.Infrastructure.Services;

public class PlantillaService : IPlantillaService
{
    private readonly DocFlowDbContext _db;

    public PlantillaService(DocFlowDbContext db) => _db = db;

    public async Task<List<PlantillaNumeracion>> ListarAsync(bool? soloActivos = null, CancellationToken ct = default)
    {
        var query = _db.PlantillasNumeracion.AsQueryable();

        if (soloActivos == true)
            query = query.Where(p => p.Activo);

        return await query.OrderBy(p => p.Id).ToListAsync(ct);
    }

    public async Task<PlantillaNumeracion> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.PlantillasNumeracion.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe plantilla con Id {id}.");
    }

    public async Task<PlantillaNumeracion> CrearAsync(int id, string descripcion, string patron,
        bool porOrganismo = false, bool porTipoDocumento = false, bool porFormatoDocumento = false,
        string periodicidad = "CONTINUO", string momentoGeneracion = "AL_INGRESAR",
        int rellenoCeros = 0, int valorInicial = 0, CancellationToken ct = default)
    {
        // Id <= 0: autogenerar el siguiente (máx + 1). El formulario ya no pide el Id;
        // el cliente envía solo descripción y patrón. Se mantiene el camino explícito
        // (id > 0) por compatibilidad (seed/migraciones), validando unicidad.
        if (id <= 0)
            id = (await _db.PlantillasNumeracion.MaxAsync(p => (int?)p.Id, ct) ?? -1) + 1;
        else if (await _db.PlantillasNumeracion.AnyAsync(p => p.Id == id, ct))
            throw new InvalidOperationException($"Ya existe una plantilla con Id {id}.");

        var plantilla = new PlantillaNumeracion(id, descripcion, patron,
            porOrganismo, porTipoDocumento, porFormatoDocumento, periodicidad, momentoGeneracion, rellenoCeros, valorInicial);
        _db.PlantillasNumeracion.Add(plantilla);
        await _db.SaveChangesAsync(ct);
        return plantilla;
    }

    public async Task<PlantillaNumeracion> ActualizarAsync(int id, string descripcion, string patron,
        bool porOrganismo = false, bool porTipoDocumento = false, bool porFormatoDocumento = false,
        string periodicidad = "CONTINUO", string momentoGeneracion = "AL_INGRESAR",
        int rellenoCeros = 0, int valorInicial = 0, CancellationToken ct = default)
    {
        var plantilla = await _db.PlantillasNumeracion.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"No existe plantilla con Id {id}.");

        plantilla.Actualizar(descripcion, patron,
            porOrganismo, porTipoDocumento, porFormatoDocumento, periodicidad, momentoGeneracion, rellenoCeros, valorInicial);
        await _db.SaveChangesAsync(ct);
        return plantilla;
    }

    public async Task ToggleActivoAsync(int id, CancellationToken ct = default)
    {
        var plantilla = await _db.PlantillasNumeracion.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"No existe plantilla con Id {id}.");

        if (plantilla.Activo)
            plantilla.Desactivar();
        else
            plantilla.Activar();

        await _db.SaveChangesAsync(ct);
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var plantilla = await _db.PlantillasNumeracion.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"No existe plantilla con Id {id}.");

        _db.PlantillasNumeracion.Remove(plantilla);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetActivaAsync(int id, CancellationToken ct = default)
    {
        var todas = await _db.PlantillasNumeracion.ToListAsync(ct);
        var elegida = todas.FirstOrDefault(p => p.Id == id)
            ?? throw new KeyNotFoundException($"No existe plantilla con Id {id}.");

        // Una sola activa: la elegida queda activa y el resto inactivas.
        foreach (var p in todas)
        {
            if (p.Id == id) p.Activar();
            else p.Desactivar();
        }
        await _db.SaveChangesAsync(ct);
    }
}
