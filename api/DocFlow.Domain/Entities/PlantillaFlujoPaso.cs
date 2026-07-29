using DocFlow.Domain.Enums;

namespace DocFlow.Domain.Entities;

/// <summary>
/// One mandatory (or optional) step in the workflow attached to a document template (SeForpla).
/// A template's workflow is the ordered set of its steps. This module only configures and stores
/// the workflow; executing/validating it is done by another team.
/// </summary>
public class PlantillaFlujoPaso
{
    public Guid Id { get; private set; }

    /// <summary>Template primary key (SeForpla.CodForm) this step belongs to.</summary>
    public string CodForm { get; private set; } = string.Empty;

    /// <summary>1-based position of this step within the template's workflow.</summary>
    public int Orden { get; private set; }

    public TipoAccionFlujo TipoAccion { get; private set; }

    public ResponsableFlujoTipo ResponsableTipo { get; private set; }

    /// <summary>Id of the responsible party — a user, role or department depending on <see cref="ResponsableTipo"/>.</summary>
    public Guid ResponsableId { get; private set; }

    public bool Obligatorio { get; private set; }

    private PlantillaFlujoPaso() { }

    public static PlantillaFlujoPaso Crear(
        Guid id,
        string codForm,
        int orden,
        TipoAccionFlujo tipoAccion,
        ResponsableFlujoTipo responsableTipo,
        Guid responsableId,
        bool obligatorio = true)
    {
        if (string.IsNullOrWhiteSpace(codForm))
            throw new ArgumentException("El código de plantilla es obligatorio.", nameof(codForm));

        if (orden < 1)
            throw new ArgumentOutOfRangeException(nameof(orden), "El orden del paso debe ser mayor o igual a 1.");

        if (responsableId == Guid.Empty)
            throw new ArgumentException("El responsable del paso es obligatorio.", nameof(responsableId));

        return new PlantillaFlujoPaso
        {
            Id = id,
            CodForm = codForm.Trim(),
            Orden = orden,
            TipoAccion = tipoAccion,
            ResponsableTipo = responsableTipo,
            ResponsableId = responsableId,
            Obligatorio = obligatorio,
        };
    }
}
