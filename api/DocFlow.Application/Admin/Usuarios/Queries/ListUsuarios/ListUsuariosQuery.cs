using DocFlow.Application.Admin.Usuarios.DTOs;
using DocFlow.Application.Common;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.Common.Mappings;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Usuarios.Queries.ListUsuarios;

public record ListUsuariosQuery(
    int Page = 1,
    int PageSize = 20,
    string? Rol = null,
    Guid? DepartamentoId = null,
    bool? Activo = null,
    string? Search = null
) : IRequest<PagedResult<UsuarioAdminDto>>;

public class ListUsuariosQueryHandler : IRequestHandler<ListUsuariosQuery, PagedResult<UsuarioAdminDto>>
{
    private readonly IUsuarioAdminRepository _repo;
    private readonly ICurrentUser _currentUser;

    public ListUsuariosQueryHandler(IUsuarioAdminRepository repo, ICurrentUser currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<UsuarioAdminDto>> Handle(ListUsuariosQuery q, CancellationToken ct)
    {
        var (items, total) = await _repo.GetPaginatedAsync(q.Page, q.PageSize, q.Rol, q.DepartamentoId, q.Activo, q.Search, ct);
        var activeAdminCount = await _repo.CountActiveAdministratorsAsync(ct);
        var currentUserId = _currentUser.UserId;

        var dtos = items.Select(u => u.ToAdminDto(
            esCuentaPropia: currentUserId.HasValue && currentUserId.Value == u.Id,
            esUltimoAdminActivo: u.Activo && u.RolNombre == "Administrador" && activeAdminCount == 1)).ToList();

        var totalPaginas = total == 0 ? 1 : (int)Math.Ceiling((double)total / q.PageSize);
        return new PagedResult<UsuarioAdminDto>(dtos, total, q.Page, totalPaginas);
    }
}
