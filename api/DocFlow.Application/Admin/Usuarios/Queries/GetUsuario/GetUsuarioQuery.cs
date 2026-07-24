using DocFlow.Application.Admin.Usuarios.DTOs;
using DocFlow.Application.Common.Mappings;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Usuarios.Queries.GetUsuario;

public record GetUsuarioQuery(Guid Id) : IRequest<UsuarioAdminDto>;

public class GetUsuarioQueryHandler : IRequestHandler<GetUsuarioQuery, UsuarioAdminDto>
{
    private readonly IUsuarioAdminRepository _repo;

    public GetUsuarioQueryHandler(IUsuarioAdminRepository repo) => _repo = repo;

    public async Task<UsuarioAdminDto> Handle(GetUsuarioQuery q, CancellationToken ct)
    {
        var usuario = await _repo.GetByIdAsync(q.Id, ct)
            ?? throw new KeyNotFoundException($"No se encontró el usuario con id {q.Id}.");

        return usuario.ToAdminDto();
    }
}
