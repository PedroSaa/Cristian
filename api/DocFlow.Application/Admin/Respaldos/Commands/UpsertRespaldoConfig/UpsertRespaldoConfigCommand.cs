using DocFlow.Application.Admin.Respaldos.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Admin.Respaldos.Commands.UpsertRespaldoConfig;

public record UpsertRespaldoConfigCommand(
    int IntervaloMinutos,
    bool Habilitado,
    int MaxBackupCount,
    int RetentionDays,
    string OutputPath,
    int TimeoutMinutos
) : IRequest<RespaldoConfigDto>;

public class UpsertRespaldoConfigCommandValidator : AbstractValidator<UpsertRespaldoConfigCommand>
{
    public UpsertRespaldoConfigCommandValidator()
    {
        RuleFor(x => x.IntervaloMinutos)
            .GreaterThan(0).WithMessage("El intervalo debe ser mayor a 0.");

        RuleFor(x => x.MaxBackupCount)
            .GreaterThanOrEqualTo(0).WithMessage("El número máximo de respaldos no puede ser negativo.");

        RuleFor(x => x.RetentionDays)
            .GreaterThanOrEqualTo(0).WithMessage("Los días de retención no pueden ser negativos.");

        RuleFor(x => x.TimeoutMinutos)
            .GreaterThan(0).WithMessage("El timeout debe ser mayor a 0.");

        RuleFor(x => x.OutputPath)
            .NotEmpty().WithMessage("La ruta de salida es obligatoria.");
    }
}

public class UpsertRespaldoConfigCommandHandler : IRequestHandler<UpsertRespaldoConfigCommand, RespaldoConfigDto>
{
    private readonly IRespaldoRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public UpsertRespaldoConfigCommandHandler(
        IRespaldoRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<RespaldoConfigDto> Handle(UpsertRespaldoConfigCommand cmd, CancellationToken ct)
    {
        var existing = await _repo.GetRespaldoConfigAsync();

        RespaldoConfig config;
        if (existing is not null)
        {
            existing.Actualizar(
                cmd.IntervaloMinutos, cmd.Habilitado,
                cmd.MaxBackupCount, cmd.RetentionDays,
                cmd.OutputPath, cmd.TimeoutMinutos);
            config = existing;
        }
        else
        {
            config = RespaldoConfig.Crear(
                Guid.NewGuid(),
                cmd.IntervaloMinutos, cmd.Habilitado,
                cmd.MaxBackupCount, cmd.RetentionDays,
                cmd.OutputPath, cmd.TimeoutMinutos);
        }

        await _repo.UpsertRespaldoConfigAsync(config);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId, "ActualizarConfigRespaldo", "RespaldoConfig", config.Id.ToString(),
            $"Configuración de respaldos actualizada: habilitado={config.Habilitado}, " +
            $"intervalo={config.IntervaloMinutos}min, retención={config.RetentionDays}d, ruta={config.OutputPath}",
            _currentUser.IpAddress, _currentUser.UserAgent));

        return new RespaldoConfigDto(
            config.Id,
            config.IntervaloMinutos,
            config.Habilitado,
            config.MaxBackupCount,
            config.RetentionDays,
            config.OutputPath,
            config.TimeoutMinutos,
            config.ActualizadoEn);
    }
}
