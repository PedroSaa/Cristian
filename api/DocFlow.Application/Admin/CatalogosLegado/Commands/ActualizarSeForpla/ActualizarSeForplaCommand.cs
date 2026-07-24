using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeForpla;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeForpla;

/// <summary>
/// Actualiza una plantilla SEFORPLA. La asociación (codForm/tipoCod) y el usuario creador
/// no cambian. Si viene archivo (FileName + BlobForm) se reemplaza el contenido y se
/// re-derivan nomForm/extForm; si no, se conservan. La observación siempre se actualiza.
/// </summary>
public record ActualizarSeForplaCommand(
    string CodForm,
    string? FileName,
    string? BlobForm,
    string? ObsForm) : IRequest;

public class ActualizarSeForplaCommandValidator : AbstractValidator<ActualizarSeForplaCommand>
{
    public ActualizarSeForplaCommandValidator()
    {
        RuleFor(x => x.CodForm)
            .NotEmpty().WithMessage("El código de plantilla es obligatorio.")
            .MaximumLength(100).WithMessage("El código de plantilla no puede superar los 100 caracteres.");

        RuleFor(x => x)
            .Must(x => string.IsNullOrEmpty(x.FileName) == string.IsNullOrEmpty(x.BlobForm))
            .WithMessage("Para reemplazar el archivo deben venir el nombre y el contenido juntos.");

        RuleFor(x => x.ObsForm)
            .MaximumLength(255).WithMessage("La observación no puede superar los 255 caracteres.");
    }
}

public class ActualizarSeForplaCommandHandler : IRequestHandler<ActualizarSeForplaCommand>
{
    private readonly ISeForplaRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActualizarSeForplaCommandHandler> _logger;

    public ActualizarSeForplaCommandHandler(
        ISeForplaRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<ActualizarSeForplaCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(ActualizarSeForplaCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.CodForm)
            ?? throw new KeyNotFoundException($"Plantilla {cmd.CodForm} no encontrada.");

        var reemplazaArchivo = !string.IsNullOrEmpty(cmd.FileName) && !string.IsNullOrEmpty(cmd.BlobForm);

        var (nomForm, extForm) = reemplazaArchivo
            ? CrearSeForplaCommandHandler.DerivarNombreYExtension(cmd.FileName!)
            : (entity.NomForm, entity.ExtForm);
        var blob = reemplazaArchivo
            ? CrearSeForplaCommandHandler.DecodificarBlob(cmd.BlobForm!)
            : entity.BlobForm;

        entity.Actualizar(entity.TipoCod, nomForm, blob, entity.SisForm, cmd.ObsForm, extForm, entity.Alto, entity.Ancho);
        await _repo.UpdateAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "ActualizarSeForpla",
            "SeForpla",
            entity.CodForm,
            $"Plantilla actualizada: {entity.NomForm}"));
    }
}
