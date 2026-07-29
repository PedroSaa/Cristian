namespace DocFlow.Domain.Enums;

/// <summary>
/// Kind of party responsible for a workflow step. Determines how
/// <see cref="Entities.PlantillaFlujoPaso.ResponsableId"/> is resolved to a name.
/// </summary>
public enum ResponsableFlujoTipo
{
    Usuario,
    Rol,
    Departamento,
}
