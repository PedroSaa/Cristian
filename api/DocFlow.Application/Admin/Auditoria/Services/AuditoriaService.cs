using DocFlow.Application.Admin.Auditoria.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DocFlow.Application.Admin.Auditoria.Services;

public class AuditoriaService : IAuditoriaService
{
    private readonly IAuditoriaRepository _repo;
    private readonly IHttpContextAccessor _http;

    public AuditoriaService(IAuditoriaRepository repo, IHttpContextAccessor http)
    {
        _repo = repo;
        _http = http;
    }

    public async Task RegistrarAsync(Guid usuarioId, string accion, string entidad, string entidadId, string detalle)
    {
        var ctx = _http.HttpContext;
        var ip = ctx?.Connection.RemoteIpAddress?.ToString();
        var ua = ctx?.Request.Headers["User-Agent"].FirstOrDefault();

        var registro = RegistroAuditoria.Crear(usuarioId, accion, entidad, entidadId, detalle, ip, ua);
        await _repo.AddAsync(registro);
    }
}
