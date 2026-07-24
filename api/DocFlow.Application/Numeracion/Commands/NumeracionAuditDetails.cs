using DocFlow.Domain.Entities.NumeracionesDocumento;

namespace DocFlow.Application.Numeracion.Commands;

internal static class NumeracionAuditDetails
{
    public static string CounterCreated(ContadorNumeracion counter) =>
        $"Contador creado: id={counter.Id}; codigo={counter.CodigoContador}; orgDepCod={counter.OrgDepCod}; nivelCod={counter.NivelCod}; tipoCod={counter.TipoCod}; dfTipo={counter.DfTipo}; periodicidad={counter.Periodicidad}; periodoRef={counter.PeriodoRef}; ultimoValor={counter.UltimoValor}; activo={counter.Activo}";

    public static string CounterValueChanged(ContadorNumeracion before, ContadorNumeracion after) =>
        $"Contador actualizado: id={after.Id}; codigo={after.CodigoContador}; orgDepCod={after.OrgDepCod}; ultimoValorAntes={before.UltimoValor}; ultimoValorDespues={after.UltimoValor}; periodoRefAntes={before.PeriodoRef}; periodoRefDespues={after.PeriodoRef}; activo={after.Activo}";

    public static string CounterActiveChanged(ContadorNumeracion before, bool activoDespues) =>
        $"Contador estado activo cambiado: id={before.Id}; codigo={before.CodigoContador}; orgDepCod={before.OrgDepCod}; activoAntes={before.Activo}; activoDespues={activoDespues}";

    public static string PlantillaCreated(PlantillaNumeracion plantilla) =>
        $"Plantilla numeracion creada: id={plantilla.Id}; descripcion={plantilla.Descripcion}; patron={plantilla.Patron}; activo={plantilla.Activo}";

    public static string PlantillaUpdated(PlantillaNumeracion before, PlantillaNumeracion after) =>
        $"Plantilla numeracion actualizada: id={after.Id}; descripcionAntes={before.Descripcion}; descripcionDespues={after.Descripcion}; patronAntes={before.Patron}; patronDespues={after.Patron}; activo={after.Activo}";

    public static string PlantillaActiveChanged(PlantillaNumeracion before, bool activoDespues) =>
        $"Plantilla numeracion estado activo cambiado: id={before.Id}; descripcion={before.Descripcion}; activoAntes={before.Activo}; activoDespues={activoDespues}";
}
