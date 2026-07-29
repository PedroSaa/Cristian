using DocFlow.Application.Admin.Usuarios.Firma.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Usuarios.Firma.Queries.GetFirmaUsuario;

/// <summary>Returns the signature configuration metadata for a user (never the image bytes nor the decrypted PIN).</summary>
public record GetFirmaUsuarioQuery(Guid UsuarioId) : IRequest<FirmaUsuarioMetadataDto>;

public class GetFirmaUsuarioHandler : IRequestHandler<GetFirmaUsuarioQuery, FirmaUsuarioMetadataDto>
{
    private readonly IFirmaUsuarioRepository _repo;

    public GetFirmaUsuarioHandler(IFirmaUsuarioRepository repo) => _repo = repo;

    public async Task<FirmaUsuarioMetadataDto> Handle(GetFirmaUsuarioQuery q, CancellationToken ct)
    {
        var firma = await _repo.GetByUsuarioAsync(q.UsuarioId, ct);

        if (firma is null)
            return new FirmaUsuarioMetadataDto(q.UsuarioId, false, false, null, null, 0, null, null);

        return new FirmaUsuarioMetadataDto(
            firma.UsuarioId,
            TieneFirma: true,
            TieneClave: firma.ClaveCifrada is not null,
            firma.Sigla,
            firma.ContentType,
            firma.ImagenFirma.LongLength,
            firma.CreadoEn,
            firma.ActualizadoEn);
    }
}
