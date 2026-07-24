namespace DocFlow.Application.Numeracion.DTOs;

/// <summary>
/// Summary counter DTO for list-view responses.
/// </summary>
public record CounterListDto(
    Guid Id,
    string CodigoContador,
    string OrgDepCod,
    string NivelCod,
    int TipoCod,
    string DfTipo,
    string Periodicidad,
    string PeriodoRef,
    long UltimoValor,
    bool Activo);
