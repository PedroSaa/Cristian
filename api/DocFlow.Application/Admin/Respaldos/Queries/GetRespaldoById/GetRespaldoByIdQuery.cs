using DocFlow.Application.Admin.Respaldos.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Respaldos.Queries.GetRespaldoById;

public record GetRespaldoByIdQuery(Guid Id) : IRequest<RespaldoDto>;

public class GetRespaldoByIdQueryHandler : IRequestHandler<GetRespaldoByIdQuery, RespaldoDto>
{
    private readonly IRespaldoRepository _repo;

    public GetRespaldoByIdQueryHandler(IRespaldoRepository repo) => _repo = repo;

    public async Task<RespaldoDto> Handle(GetRespaldoByIdQuery query, CancellationToken ct)
    {
        var respaldo = await _repo.GetByIdAsync(query.Id);

        if (respaldo is null)
            throw new KeyNotFoundException($"Respaldo {query.Id} no encontrado.");

        return new RespaldoDto(
            respaldo.Id,
            respaldo.Nombre,
            respaldo.FechaCreacion,
            respaldo.TamanioBytes,
            respaldo.Estado,
            respaldo.Ruta);
    }
}
