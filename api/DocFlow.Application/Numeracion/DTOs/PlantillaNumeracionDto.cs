using DocFlow.Domain.Entities.NumeracionesDocumento;

namespace DocFlow.Application.Numeracion.DTOs;

public record PlantillaNumeracionDto(
    int Id,
    string Descripcion,
    string? Patron,
    bool Activo,
    bool PorOrganismo,
    bool PorTipoDocumento,
    bool PorFormatoDocumento,
    string Periodicidad,
    string MomentoGeneracion,
    int RellenoCeros,
    int ValorInicial)
{
    public static PlantillaNumeracionDto From(PlantillaNumeracion p) => new(
        p.Id, p.Descripcion, p.Patron, p.Activo,
        p.PorOrganismo, p.PorTipoDocumento, p.PorFormatoDocumento,
        p.Periodicidad, p.MomentoGeneracion, p.RellenoCeros, p.ValorInicial);
}
