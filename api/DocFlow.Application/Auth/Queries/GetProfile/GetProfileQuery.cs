using DocFlow.Application.Auth.DTOs;
using MediatR;

namespace DocFlow.Application.Auth.Queries.GetProfile;

public record GetProfileQuery : IRequest<UsuarioDto>;
