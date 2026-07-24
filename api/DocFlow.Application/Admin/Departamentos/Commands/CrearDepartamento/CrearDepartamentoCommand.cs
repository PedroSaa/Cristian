using DocFlow.Application.Admin.Departamentos.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.Departamentos.Commands.CrearDepartamento;

public record CrearDepartamentoCommand(
    string Nombre,
    string Codigo
) : IRequest<DepartamentoAdminDto>;

public class CrearDepartamentoCommandValidator : AbstractValidator<CrearDepartamentoCommand>
{
    public CrearDepartamentoCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código es obligatorio.")
            .MaximumLength(20).WithMessage("El código no puede superar los 20 caracteres.");
    }
}

public class CrearDepartamentoCommandHandler : IRequestHandler<CrearDepartamentoCommand, DepartamentoAdminDto>
{
    private readonly IDepartamentoRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CrearDepartamentoCommandHandler> _logger;

    public CrearDepartamentoCommandHandler(
        IDepartamentoRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<CrearDepartamentoCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<DepartamentoAdminDto> Handle(CrearDepartamentoCommand cmd, CancellationToken ct)
    {
        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        if (await _repo.ExistsByNombreAsync(cmd.Nombre))
            throw new InvalidOperationException($"Ya existe un departamento con el nombre {cmd.Nombre}.");

        if (await _repo.ExistsByCodigoAsync(cmd.Codigo))
            throw new InvalidOperationException($"Ya existe un departamento con el código {cmd.Codigo}.");

        var dep = Departamento.Crear(Guid.NewGuid(), cmd.Nombre, cmd.Codigo);
        await _repo.CreateAsync(dep);

        var registro = RegistroAuditoria.Crear(
            usuarioId,
            "CrearDepartamento",
            "Departamento",
            dep.Id.ToString(),
            $"Departamento creado: {dep.Nombre} ({dep.Codigo})");
        await _auditoria.AddAsync(registro);

        return new DepartamentoAdminDto(dep.Id, dep.Nombre, dep.Codigo, dep.Activo, dep.CreadoEn, 0);
    }
}
