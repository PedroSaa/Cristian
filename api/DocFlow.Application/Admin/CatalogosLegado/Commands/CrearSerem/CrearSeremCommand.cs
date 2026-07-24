using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSerem;

public record CrearSeremCommand(
    string RemCod,
    string RemTipo,
    string RemNomb,
    short? RemRutValid = null,
    string? RemSector = null,
    string? RemComuna = null,
    int? RemNro = null,
    string? RemEmail = null,
    string? RemFax = null,
    string? RemRut = null,
    string? RemDirec = null,
    string? RemTelef = null,
    string? RemZip = null,
    string? RemRegion = null,
    string? RemBlock = null,
    string? RemCalle = null,
    decimal? RemCodDocDigital = null
) : IRequest<SeremDto>;

public class CrearSeremCommandValidator : AbstractValidator<CrearSeremCommand>
{
    public CrearSeremCommandValidator()
    {
        RuleFor(x => x.RemCod)
            .NotEmpty().WithMessage("El código de remitente es obligatorio.")
            .MaximumLength(20).WithMessage("El código de remitente no puede superar los 20 caracteres.");

        RuleFor(x => x.RemTipo)
            .NotEmpty().WithMessage("El tipo de remitente es obligatorio.")
            .MaximumLength(3).WithMessage("El tipo de remitente no puede superar los 3 caracteres.");

        RuleFor(x => x.RemNomb)
            .NotEmpty().WithMessage("El nombre del remitente es obligatorio.")
            .MaximumLength(60).WithMessage("El nombre del remitente no puede superar los 60 caracteres.");

        RuleFor(x => x.RemSector)
            .MaximumLength(20).WithMessage("El sector no puede superar los 20 caracteres.");

        RuleFor(x => x.RemComuna)
            .MaximumLength(18).WithMessage("La comuna no puede superar los 18 caracteres.");

        RuleFor(x => x.RemEmail)
            .MaximumLength(30).WithMessage("El email no puede superar los 30 caracteres.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .When(x => !string.IsNullOrWhiteSpace(x.RemEmail));

        RuleFor(x => x.RemFax)
            .MaximumLength(10).WithMessage("El fax no puede superar los 10 caracteres.");

        RuleFor(x => x.RemRut)
            .MaximumLength(12).WithMessage("El RUT no puede superar los 12 caracteres.");

        RuleFor(x => x.RemDirec)
            .MaximumLength(60).WithMessage("La dirección no puede superar los 60 caracteres.");

        RuleFor(x => x.RemTelef)
            .MaximumLength(10).WithMessage("El teléfono no puede superar los 10 caracteres.");

        RuleFor(x => x.RemZip)
            .MaximumLength(40).WithMessage("El código ZIP no puede superar los 40 caracteres.");

        RuleFor(x => x.RemRegion)
            .MaximumLength(40).WithMessage("La región no puede superar los 40 caracteres.");

        RuleFor(x => x.RemBlock)
            .MaximumLength(60).WithMessage("El bloque no puede superar los 60 caracteres.");

        RuleFor(x => x.RemCalle)
            .MaximumLength(60).WithMessage("La calle no puede superar los 60 caracteres.");
    }
}

public class CrearSeremCommandHandler : IRequestHandler<CrearSeremCommand, SeremDto>
{
    private readonly ISeremRepository _repo;
    private readonly ISeremTipoRepository _tipoRepo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CrearSeremCommandHandler> _logger;

    public CrearSeremCommandHandler(
        ISeremRepository repo,
        ISeremTipoRepository tipoRepo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<CrearSeremCommandHandler> logger)
    {
        _repo = repo;
        _tipoRepo = tipoRepo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<SeremDto> Handle(CrearSeremCommand cmd, CancellationToken ct)
    {
        if (await _repo.ExistsAsync(cmd.RemCod))
            throw new InvalidOperationException($"Ya existe un remitente con el código {cmd.RemCod}.");

        var tipo = await _tipoRepo.GetByIdAsync(cmd.RemTipo)
            ?? throw new KeyNotFoundException($"Tipo de remitente {cmd.RemTipo} no encontrado.");

        var serem = new Serem(
            cmd.RemCod,
            cmd.RemTipo,
            cmd.RemNomb,
            cmd.RemRutValid,
            cmd.RemSector,
            cmd.RemComuna,
            cmd.RemNro,
            cmd.RemEmail,
            cmd.RemFax,
            cmd.RemRut,
            cmd.RemDirec,
            cmd.RemTelef,
            cmd.RemZip,
            cmd.RemRegion,
            cmd.RemBlock,
            cmd.RemCalle,
            cmd.RemCodDocDigital);

        await _repo.CreateAsync(serem);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "CrearSerem",
            "Serem",
            serem.RemCod,
            $"Remitente creado: {serem.RemNomb} ({tipo.RemDesc})"));

        return new SeremDto(
            serem.RemCod,
            serem.RemTipo,
            tipo.RemDesc,
            serem.RemRutValid,
            serem.RemSector,
            serem.RemNomb,
            serem.RemComuna,
            serem.RemNro,
            serem.RemEmail,
            serem.RemFax,
            serem.RemRut,
            serem.RemDirec,
            serem.RemTelef,
            serem.RemZip,
            serem.RemRegion,
            serem.RemBlock,
            serem.RemCalle,
            serem.RemCodDocDigital);
    }
}
