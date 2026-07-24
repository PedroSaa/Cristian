using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using DocFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocFlow.Infrastructure.Repositories.OrdenesCompra;

public class OrdenCompraRepository : IOrdenCompraRepository
{
    private readonly DocFlowDbContext _db;

    public OrdenCompraRepository(DocFlowDbContext db) => _db = db;

    public async Task AddAsync(OrdenCompra ordenCompra, CancellationToken ct = default)
    {
        _db.OrdenesCompra.Add(ordenCompra);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<OrdenCompra?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // Items are part of the aggregate; attachments are loaded separately
        // (metadata projection) so their binary content never travels with the order.
        return await _db.OrdenesCompra
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task UpdateAsync(OrdenCompra ordenCompra, CancellationToken ct = default)
    {
        // The aggregate is normally tracked (loaded through GetByIdAsync in the same scope);
        // calling Update on a tracked graph would wrongly mark new items as Modified.
        if (_db.Entry(ordenCompra).State == EntityState.Detached)
            _db.OrdenesCompra.Update(ordenCompra);

        // Items created by ReemplazarItems carry client-generated Guid keys; DetectChanges
        // assumes a set key means the row already exists and marks them Modified, which
        // fails on save. Resolve new-vs-existing against the store and fix the state.
        var idsPersistidos = await _db.OrdenesCompraItems.AsNoTracking()
            .Where(i => i.OrdenCompraId == ordenCompra.Id)
            .Select(i => i.Id)
            .ToListAsync(ct);

        foreach (var item in ordenCompra.Items)
        {
            if (!idsPersistidos.Contains(item.Id))
                _db.Entry(item).State = EntityState.Added;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<OrdenCompraListRow> Items, int TotalCount)> GetListAsync(
        EstadoOrdenCompra? estado = null,
        Guid? proveedorId = null,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = _db.OrdenesCompra.AsNoTracking().AsQueryable();

        if (estado.HasValue)
            query = query.Where(o => o.Estado == estado.Value);

        if (proveedorId.HasValue)
            query = query.Where(o => o.ProveedorId == proveedorId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(o =>
                (o.Numero != null && o.Numero.ToLower().Contains(s)) ||
                (o.Observaciones != null && o.Observaciones.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(ct);

        // Read-only join with the Proveedores module to expose the provider name.
        var rows = await query
            .OrderByDescending(o => o.CreadoEn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(
                _db.Proveedores.AsNoTracking(),
                o => o.ProveedorId,
                p => p.Id,
                (o, p) => new OrdenCompraListRow(
                    o.Id,
                    o.Numero,
                    o.ProveedorId,
                    p.Nombre,
                    o.Fecha,
                    o.Moneda,
                    o.Neto,
                    o.Iva,
                    o.Total,
                    o.Estado,
                    o.CreadoEn,
                    o.CodigoMercadoPublico))
            .ToListAsync(ct);

        return (rows, total);
    }

    public async Task<IReadOnlyList<OrdenCompraAdjuntoMetadata>> GetAdjuntosMetadataAsync(
        Guid ordenCompraId, CancellationToken ct = default)
    {
        return await _db.OrdenesCompraAdjuntos
            .AsNoTracking()
            .Where(a => a.OrdenCompraId == ordenCompraId)
            .OrderBy(a => a.CreadoEn)
            .Select(a => new OrdenCompraAdjuntoMetadata(
                a.Id, a.NombreArchivo, a.ContentType, a.Tamano, a.SubidoPor, a.CreadoEn))
            .ToListAsync(ct);
    }

    public async Task<OrdenCompraAdjunto?> GetAdjuntoAsync(
        Guid ordenCompraId, Guid adjuntoId, CancellationToken ct = default)
    {
        return await _db.OrdenesCompraAdjuntos
            .FirstOrDefaultAsync(a => a.OrdenCompraId == ordenCompraId && a.Id == adjuntoId, ct);
    }

    public async Task AddAdjuntoAsync(OrdenCompraAdjunto adjunto, CancellationToken ct = default)
    {
        _db.OrdenesCompraAdjuntos.Add(adjunto);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAdjuntoAsync(OrdenCompraAdjunto adjunto, CancellationToken ct = default)
    {
        _db.OrdenesCompraAdjuntos.Remove(adjunto);
        await _db.SaveChangesAsync(ct);
    }
}
