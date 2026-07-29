using DocFlow.Domain.Entities;

namespace DocFlow.Domain.Interfaces;

public interface IPlantillaFlujoRepository
{
    /// <summary>Returns the template's workflow steps ordered by <see cref="PlantillaFlujoPaso.Orden"/>.</summary>
    Task<IReadOnlyList<PlantillaFlujoPaso>> GetByCodFormAsync(string codForm, CancellationToken ct = default);

    /// <summary>
    /// Atomically replaces the whole workflow of a template: deletes the existing steps for
    /// <paramref name="codForm"/> and inserts <paramref name="pasos"/> in a single transaction.
    /// </summary>
    Task ReemplazarAsync(string codForm, IEnumerable<PlantillaFlujoPaso> pasos, CancellationToken ct = default);
}
