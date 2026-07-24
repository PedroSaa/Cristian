using DocFlow.Application.Auth.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Auth.Queries.GetProfile;

public class GetProfileHandler : IRequestHandler<GetProfileQuery, UsuarioDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly ISeUsuariRepository _repo;

    public GetProfileHandler(ICurrentUser currentUser, ISeUsuariRepository repo)
    {
        _currentUser = currentUser;
        _repo = repo;
    }

    public async Task<UsuarioDto> Handle(GetProfileQuery query, CancellationToken ct)
    {
        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException();

        var usuario = await _repo.GetByIdAsync(_currentUser.UserId.Value, ct)
            ?? throw new KeyNotFoundException("No se pudo cargar la cuenta actual.");

        var dbPermisos = usuario.Rol?.RolPermisos?.Select(rp => rp.Permiso).ToList() as IReadOnlyList<Permiso>;
        return AuthUserMapper.ToDto(usuario, dbPermisos: dbPermisos);
    }
}
