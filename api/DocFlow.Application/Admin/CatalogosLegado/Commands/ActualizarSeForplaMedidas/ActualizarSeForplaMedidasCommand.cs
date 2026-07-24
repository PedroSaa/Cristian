using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeForplaMedidas;

public record ActualizarSeForplaMedidaItem(short IdForplaMed, short X, short Y, short Ancho, short Alto);

/// <summary>
/// Actualiza las coordenadas de las medidas de una plantilla. Como el legacy
/// (pgrabamedidas), solo cambia X/Y/Ancho/Alto y solo de los items recibidos;
/// el objeto nunca se modifica.
/// </summary>
public record ActualizarSeForplaMedidasCommand(
    string CodForm,
    IReadOnlyList<ActualizarSeForplaMedidaItem> Items) : IRequest;

public class ActualizarSeForplaMedidasCommandValidator : AbstractValidator<ActualizarSeForplaMedidasCommand>
{
    public ActualizarSeForplaMedidasCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Debe enviar al menos una medida.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.X).InclusiveBetween((short)0, short.MaxValue)
                .WithMessage("La coordenada X debe estar entre 0 y 32767.");
            item.RuleFor(i => i.Y).InclusiveBetween((short)0, short.MaxValue)
                .WithMessage("La coordenada Y debe estar entre 0 y 32767.");
            item.RuleFor(i => i.Ancho).InclusiveBetween((short)0, short.MaxValue)
                .WithMessage("El ancho debe estar entre 0 y 32767.");
            item.RuleFor(i => i.Alto).InclusiveBetween((short)0, short.MaxValue)
                .WithMessage("El alto debe estar entre 0 y 32767.");
        });
    }
}

public class ActualizarSeForplaMedidasCommandHandler : IRequestHandler<ActualizarSeForplaMedidasCommand>
{
    private readonly ISeForplaRepository _plantillas;
    private readonly ISeForplaMedidaRepository _medidas;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActualizarSeForplaMedidasCommandHandler> _logger;

    public ActualizarSeForplaMedidasCommandHandler(
        ISeForplaRepository plantillas,
        ISeForplaMedidaRepository medidas,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<ActualizarSeForplaMedidasCommandHandler> logger)
    {
        _plantillas = plantillas;
        _medidas = medidas;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(ActualizarSeForplaMedidasCommand cmd, CancellationToken ct)
    {
        if (!await _plantillas.ExistsAsync(cmd.CodForm))
            throw new KeyNotFoundException($"Plantilla {cmd.CodForm} no encontrada.");

        var medidas = await _medidas.GetByCodFormAsync(cmd.CodForm);

        foreach (var item in cmd.Items)
        {
            var medida = medidas.FirstOrDefault(m => m.IdForplaMed == item.IdForplaMed)
                ?? throw new KeyNotFoundException($"Medida {item.IdForplaMed} de la plantilla {cmd.CodForm} no encontrada.");

            medida.ActualizarMedidas(item.X, item.Y, item.Ancho, item.Alto);
        }

        await _medidas.SaveChangesAsync();

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "ActualizarSeForplaMedidas",
            "SeForplaMedida",
            cmd.CodForm,
            $"Medidas actualizadas: {cmd.Items.Count}"));
    }
}
