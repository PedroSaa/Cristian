using DocFlow.Application.Admin.Configuracion.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Configuracion.Queries.GetConfiguracion;

public record GetConfiguracionQuery(string Clave) : IRequest<ConfiguracionDto>;

public class GetConfiguracionQueryHandler : IRequestHandler<GetConfiguracionQuery, ConfiguracionDto>
{
    private readonly IConfiguracionRepository _repo;

    public GetConfiguracionQueryHandler(IConfiguracionRepository repo) => _repo = repo;

    public async Task<ConfiguracionDto> Handle(GetConfiguracionQuery query, CancellationToken ct)
    {
        var config = await _repo.GetByClaveAsync(query.Clave)
            ?? throw new KeyNotFoundException($"Configuración con clave '{query.Clave}' no encontrada.");

        return new ConfiguracionDto(config.Id, config.Clave, config.Valor, config.Descripcion, config.ActualizadoEn);
    }
}
