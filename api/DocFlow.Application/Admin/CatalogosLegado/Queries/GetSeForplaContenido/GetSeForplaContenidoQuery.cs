using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeForplaContenido;

/// <summary>
/// Devuelve los bytes crudos del archivo Word de una plantilla SEFORPLA.
/// Lo usa el endpoint que OnlyOffice descarga para previsualizar/editar.
/// </summary>
public record GetSeForplaContenidoQuery(string CodForm)
    : IRequest<(byte[] Bytes, string Extension, string NomForm)>;

public class GetSeForplaContenidoQueryHandler
    : IRequestHandler<GetSeForplaContenidoQuery, (byte[] Bytes, string Extension, string NomForm)>
{
    private readonly ISeForplaRepository _repo;

    public GetSeForplaContenidoQueryHandler(ISeForplaRepository repo) => _repo = repo;

    public async Task<(byte[] Bytes, string Extension, string NomForm)> Handle(
        GetSeForplaContenidoQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.CodForm)
            ?? throw new KeyNotFoundException($"Plantilla {request.CodForm} no encontrada.");

        return (entity.BlobForm, entity.ExtForm, entity.NomForm);
    }
}
