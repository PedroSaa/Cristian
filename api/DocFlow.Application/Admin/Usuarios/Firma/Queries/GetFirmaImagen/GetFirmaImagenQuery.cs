using DocFlow.Application.Admin.Usuarios.Firma.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Usuarios.Firma.Queries.GetFirmaImagen;

/// <summary>Returns the raw signature image bytes and content type. Throws KeyNotFoundException (404) if none.</summary>
public record GetFirmaImagenQuery(Guid UsuarioId) : IRequest<FirmaImagenDto>;

public class GetFirmaImagenHandler : IRequestHandler<GetFirmaImagenQuery, FirmaImagenDto>
{
    private readonly IFirmaUsuarioRepository _repo;

    public GetFirmaImagenHandler(IFirmaUsuarioRepository repo) => _repo = repo;

    public async Task<FirmaImagenDto> Handle(GetFirmaImagenQuery q, CancellationToken ct)
    {
        var firma = await _repo.GetByUsuarioAsync(q.UsuarioId, ct)
            ?? throw new KeyNotFoundException("El usuario no tiene una firma configurada.");

        return new FirmaImagenDto(firma.ImagenFirma, firma.ContentType);
    }
}
