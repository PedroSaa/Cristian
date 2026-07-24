using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Admin.Permisos.Commands;

public record AsignarPermisosRolCommand(Guid RolId, IEnumerable<Guid> PermisoIds)
    : IRequest;

public class AsignarPermisosRolCommandHandler : IRequestHandler<AsignarPermisosRolCommand>
{
    private readonly IRolRepository _rolRepo;
    private readonly IPermisoRepository _permisoRepo;
    private readonly IPermissionService _permissionService;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public AsignarPermisosRolCommandHandler(
        IRolRepository rolRepo,
        IPermisoRepository permisoRepo,
        IPermissionService permissionService,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _rolRepo = rolRepo;
        _permisoRepo = permisoRepo;
        _permissionService = permissionService;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task Handle(AsignarPermisosRolCommand cmd, CancellationToken ct)
    {
        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        var rol = await _rolRepo.GetByIdAsync(cmd.RolId)
            ?? throw new KeyNotFoundException($"No se encontró el rol con id {cmd.RolId}.");

        var permisosAnteriores = await _permisoRepo.GetByRolIdAsync(cmd.RolId);
        var permisoList = cmd.PermisoIds.ToList();
        var permisosNuevos = new List<Permiso>();

        foreach (var pid in permisoList)
        {
            var permiso = await _permisoRepo.GetByIdAsync(pid);
            if (permiso is null)
                throw new KeyNotFoundException($"No se encontró el permiso con id {pid}.");

            permisosNuevos.Add(permiso);
        }

        await _permisoRepo.UpdateRolPermisosAsync(cmd.RolId, permisoList);
        await _permissionService.InvalidateAllAsync();

        var anteriores = permisosAnteriores.ToList();
        var nombresAnteriores = anteriores.Select(p => p.Nombre).ToHashSet();
        var nombresNuevos = permisosNuevos.Select(p => p.Nombre).ToHashSet();
        var agregados = permisosNuevos.Where(p => !nombresAnteriores.Contains(p.Nombre));
        var quitados = anteriores.Where(p => !nombresNuevos.Contains(p.Nombre));

        // Detalle conciso (diff) en vez de listar todos los permisos: el rol Administrador tiene
        // decenas de permisos y la lista completa excedía la columna detalle (varchar(2000)).
        var registro = RegistroAuditoria.Crear(
            usuarioId,
            "PermisosRolAsignados",
            "Rol",
            rol.Id.ToString(),
            $"Permisos del rol {rol.Nombre} actualizados. Antes: {anteriores.Count}; Después: {permisoList.Count}. Agregados: {FormatPermisos(agregados)}. Quitados: {FormatPermisos(quitados)}");
        await _auditoria.AddAsync(registro);
    }

    private static string FormatPermisos(IEnumerable<Permiso> permisos)
    {
        var nombres = permisos
            .Select(p => p.Nombre)
            .OrderBy(nombre => nombre)
            .ToArray();

        return nombres.Length == 0 ? "sin permisos" : string.Join(", ", nombres);
    }
}

public class AsignarPermisosRolCommandValidator : AbstractValidator<AsignarPermisosRolCommand>
{
    public AsignarPermisosRolCommandValidator()
    {
        RuleFor(x => x.RolId)
            .NotEmpty().WithMessage("El identificador del rol es obligatorio.");

        RuleFor(x => x.PermisoIds)
            .NotNull().WithMessage("La lista de permisos es obligatoria.");
    }
}
