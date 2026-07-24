using DocFlow.Application.Admin.Integraciones.DTOs;
using MediatR;

namespace DocFlow.Application.Admin.Integraciones.Commands.ProbarConexion;

public record ProbarConexionIntegracionCommand(Guid Id) : IRequest<IntegracionTestResultDto>;
