using DocFlow.Application.Admin.Configuracion.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Configuracion.Queries.ListConfiguracion;

public record ListConfiguracionQuery : IRequest<IReadOnlyList<ConfiguracionDto>>;

public class ListConfiguracionQueryHandler : IRequestHandler<ListConfiguracionQuery, IReadOnlyList<ConfiguracionDto>>
{
    private readonly IConfiguracionRepository _repo;

    public ListConfiguracionQueryHandler(IConfiguracionRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ConfiguracionDto>> Handle(ListConfiguracionQuery query, CancellationToken ct)
    {
        var items = await _repo.GetAllAsync();
        return items
            .Select(c => new ConfiguracionDto(
                c.Id, c.Clave, c.Valor, c.Descripcion, c.ActualizadoEn,
                SecurityKeyMetadata.GetGrupo(c.Clave),
                SecurityKeyMetadata.GetTipo(c.Clave),
                SecurityKeyMetadata.GetMinValue(c.Clave),
                SecurityKeyMetadata.GetMaxValue(c.Clave)))
            .ToList();
    }
}

/// <summary>
/// Shared metadata for the 7 security policy keys.
/// Single source of truth for Grupo, Tipo, MinValue, and MaxValue
/// that the list query and upsert validator both consume.
/// </summary>
internal static class SecurityKeyMetadata
{
    private sealed record KeyDef(string Grupo, string? Tipo, int? MinValue, int? MaxValue);

    private static readonly Dictionary<string, KeyDef> Keys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LockoutMaxIntentos"]     = new("seguridad", "int",  1,   10),
        ["LockoutDuracionMinutos"] = new("seguridad", "int",  1,   1440),
        ["JwtExpirationMinutos"]   = new("seguridad", "int",  15,  1440),
        ["TotpWindowSegundos"]     = new("seguridad", "int",  90,  300),
        ["PasswordMinLength"]      = new("seguridad", "int",  8,   32),
        ["PasswordRequireUpper"]   = new("seguridad", "bool", null, null),
        ["PasswordRequireSpecial"] = new("seguridad", "bool", null, null),
        ["RequireMfaAdministradores"] = new("seguridad", "bool", null, null),
        ["RequireMfaOtrosUsuarios"]   = new("seguridad", "bool", null, null),
    };

    public static string? GetGrupo(string clave)
        => Keys.TryGetValue(clave, out var def) ? def.Grupo : null;

    public static string? GetTipo(string clave)
        => Keys.TryGetValue(clave, out var def) ? def.Tipo : null;

    public static int? GetMinValue(string clave)
        => Keys.TryGetValue(clave, out var def) ? def.MinValue : null;

    public static int? GetMaxValue(string clave)
        => Keys.TryGetValue(clave, out var def) ? def.MaxValue : null;
}
