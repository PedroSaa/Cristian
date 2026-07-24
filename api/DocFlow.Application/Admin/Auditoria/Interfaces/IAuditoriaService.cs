namespace DocFlow.Application.Admin.Auditoria.Interfaces;

public interface IAuditoriaService
{
    Task RegistrarAsync(Guid usuarioId, string accion, string entidad, string entidadId, string detalle);
}
