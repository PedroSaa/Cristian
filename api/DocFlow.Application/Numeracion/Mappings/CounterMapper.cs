using DocFlow.Application.Numeracion.DTOs;
using DocFlow.Domain.Entities.NumeracionesDocumento;

namespace DocFlow.Application.Numeracion.Mappings;

/// <summary>
/// Static mapper for ContadorNumeracion → DTO conversions used by CQRS handlers.
/// Follows the existing static mapper pattern (see UsuarioSplitMapper).
/// </summary>
public static class CounterMapper
{
    public static CounterDto ToDto(this ContadorNumeracion entity)
        => new(
            entity.Id,
            entity.CodigoContador,
            entity.OrgDepCod,
            entity.NivelCod,
            entity.TipoCod,
            entity.DfTipo,
            entity.Periodicidad,
            entity.PeriodoRef,
            entity.UltimoValor,
            entity.Activo,
            entity.CreatedAt,
            entity.UpdatedAt);

    public static CounterListDto ToListDto(this ContadorNumeracion entity)
        => new(
            entity.Id,
            entity.CodigoContador,
            entity.OrgDepCod,
            entity.NivelCod,
            entity.TipoCod,
            entity.DfTipo,
            entity.Periodicidad,
            entity.PeriodoRef,
            entity.UltimoValor,
            entity.Activo);
}
