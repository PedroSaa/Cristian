using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.Departamentos.Commands.ActualizarDepartamento;

public record ActualizarDepartamentoCommand(
    Guid Id,
    string Nombre,
    string Codigo
) : IRequest;

public class ActualizarDepartamentoCommandValidator : AbstractValidator<ActualizarDepartamentoCommand>
{
    public ActualizarDepartamentoCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código es obligatorio.")
            .MaximumLength(20).WithMessage("El código no puede superar los 20 caracteres.");
    }
}

public class ActualizarDepartamentoCommandHandler : IRequestHandler<ActualizarDepartamentoCommand>
{
    private readonly IDepartamentoRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActualizarDepartamentoCommandHandler> _logger;

    public ActualizarDepartamentoCommandHandler(
        IDepartamentoRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<ActualizarDepartamentoCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(ActualizarDepartamentoCommand cmd, CancellationToken ct)
    {
        var dep = await _repo.GetByIdAsync(cmd.Id)
            ?? throw new KeyNotFoundException($"Departamento {cmd.Id} no encontrado.");

        if (dep.Nombre != cmd.Nombre && await _repo.ExistsByNombreAsync(cmd.Nombre))
            throw new InvalidOperationException($"Ya existe un departamento con el nombre {cmd.Nombre}.");

        if (dep.Codigo != cmd.Codigo && await _repo.ExistsByCodigoAsync(cmd.Codigo))
            throw new InvalidOperationException($"Ya existe un departamento con el código {cmd.Codigo}.");

        dep.Actualizar(cmd.Nombre, cmd.Codigo);
        await _repo.UpdateAsync(dep);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        var registro = RegistroAuditoria.Crear(
            usuarioId,
            "ActualizarDepartamento",
            "Departamento",
            dep.Id.ToString(),
            $"Departamento actualizado: {dep.Nombre} ({dep.Codigo})");
        await _auditoria.AddAsync(registro);
    }
}
