namespace DocFlow.Application.Admin.CatalogosLegado.DTOs;

public record SeFordocDto(
    short TipoCod,
    short TipoRec,
    short TipoInt,
    string TipoDesc,
    int CorrN,
    DateTime CorrFecha,
    int? TipoEnv,
    short SeFordocVistaI,
    short SeFordocVistaE,
    short SeFordocVistaR,
    string? SeFordocFormatoNum);
