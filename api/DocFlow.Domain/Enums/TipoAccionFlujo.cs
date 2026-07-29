namespace DocFlow.Domain.Enums;

/// <summary>
/// Fixed set of actions a workflow step can require over a document template.
/// </summary>
public enum TipoAccionFlujo
{
    Autorizar,
    Firmar,
    Revisar,
    Visar,
}
