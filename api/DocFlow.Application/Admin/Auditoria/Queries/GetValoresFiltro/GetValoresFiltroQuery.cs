using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Auditoria.Queries.GetValoresFiltro;

public record GetValoresFiltroQuery() : IRequest<ValoresFiltro>;

public class GetValoresFiltroQueryHandler : IRequestHandler<GetValoresFiltroQuery, ValoresFiltro>
{
    private readonly IAuditoriaRepository _repo;

    public GetValoresFiltroQueryHandler(IAuditoriaRepository repo) => _repo = repo;

    public async Task<ValoresFiltro> Handle(GetValoresFiltroQuery query, CancellationToken ct)
    {
        return await _repo.GetValoresFiltroAsync();
    }
}
