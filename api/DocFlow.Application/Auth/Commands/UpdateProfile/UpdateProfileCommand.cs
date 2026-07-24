using DocFlow.Application.Auth.DTOs;
using MediatR;

namespace DocFlow.Application.Auth.Commands.UpdateProfile;

public record UpdateProfileCommand(string? Nombre, string? Email) : IRequest<UsuarioDto>;
