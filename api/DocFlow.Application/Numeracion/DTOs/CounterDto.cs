namespace DocFlow.Application.Numeracion.DTOs;

/// <summary>
/// Full counter DTO for single-entity responses.
/// </summary>
public record CounterDto(
    Guid Id,
    string CodigoContador,
    string OrgDepCod,
    string NivelCod,
    int TipoCod,
    string DfTipo,
    string Periodicidad,
    string PeriodoRef,
    long UltimoValor,
    bool Activo,
    DateTime CreatedAt,
    DateTime UpdatedAt);
