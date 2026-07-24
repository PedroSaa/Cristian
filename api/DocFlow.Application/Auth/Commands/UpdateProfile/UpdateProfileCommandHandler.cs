using DocFlow.Application.Auth.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Auth.Commands.UpdateProfile;

public class UpdateProfileHandler : IRequestHandler<UpdateProfileCommand, UsuarioDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly ISeUsuariRepository _repo;
    private readonly ISePersonalRepository _personalRepository;
    private readonly IAuditoriaRepository _auditoria;

    public UpdateProfileHandler(ICurrentUser currentUser, ISeUsuariRepository repo, ISePersonalRepository personalRepository, IAuditoriaRepository auditoria)
    {
        _currentUser = currentUser;
        _repo = repo;
        _personalRepository = personalRepository;
        _auditoria = auditoria;
    }

    public async Task<UsuarioDto> Handle(UpdateProfileCommand cmd, CancellationToken ct)
    {
        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException();

        var usuario = await _repo.GetByIdAsync(_currentUser.UserId.Value, ct)
            ?? throw new KeyNotFoundException("No se pudo cargar la cuenta actual.");

        if (usuario.Personal is null)
            throw new KeyNotFoundException("No se pudo cargar el perfil de la cuenta.");

        // Unicidad de email: solo si cambia respecto del actual, verificar que no lo use otra cuenta.
        var nuevoEmail = cmd.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(nuevoEmail)
            && !string.Equals(nuevoEmail, usuario.Personal.Correo, StringComparison.OrdinalIgnoreCase))
        {
            var existente = await _personalRepository.GetByCorreoAsync(nuevoEmail, ct);
            if (existente is not null
                && !string.Equals(existente.Usucod, usuario.Personal.Usucod, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Ya existe un usuario con el correo {nuevoEmail}.");
            }
        }

        usuario.Personal.Actualizar(
            cmd.Nombre ?? usuario.Personal.Nombres,
            apellidoPaterno: null,
            apellidoMaterno: null,
            correo: cmd.Email);

        await _personalRepository.UpdateAsync(usuario.Personal, ct);

        // IP/User-Agent los completa AuditoriaRepository de forma central.
        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuario.UsuarioId, "ActualizarPerfil", "Usuario", usuario.UsuarioId.ToString(),
            $"Perfil propio actualizado: nombre={usuario.Personal.Nombres}, email={usuario.Personal.Correo}"));

        var dbPermisos = usuario.Rol?.RolPermisos?.Select(rp => rp.Permiso).ToList() as IReadOnlyList<Permiso>;
        return AuthUserMapper.ToDto(usuario, dbPermisos: dbPermisos);
    }
}
