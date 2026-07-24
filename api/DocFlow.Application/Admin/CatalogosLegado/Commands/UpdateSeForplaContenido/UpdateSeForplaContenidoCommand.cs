using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.UpdateSeForplaContenido;

/// <summary>
/// Reemplaza solo el contenido (archivo Word) de una plantilla, conservando el resto
/// de sus metadatos. Lo usa el callback de guardado del editor OnlyOffice.
/// </summary>
public record UpdateSeForplaContenidoCommand(string CodForm, byte[] Contenido) : IRequest;

public class UpdateSeForplaContenidoCommandHandler : IRequestHandler<UpdateSeForplaContenidoCommand>
{
    private readonly ISeForplaRepository _repo;

    public UpdateSeForplaContenidoCommandHandler(ISeForplaRepository repo) => _repo = repo;

    public async Task Handle(UpdateSeForplaContenidoCommand cmd, CancellationToken ct)
    {
        if (cmd.Contenido is null || cmd.Contenido.Length == 0)
            throw new ArgumentException("El contenido de la plantilla no puede estar vacío.", nameof(cmd));

        var entity = await _repo.GetByIdAsync(cmd.CodForm)
            ?? throw new KeyNotFoundException($"Plantilla {cmd.CodForm} no encontrada.");

        // Conserva todos los metadatos; solo cambia el blob del documento.
        entity.Actualizar(
            entity.TipoCod,
            entity.NomForm,
            cmd.Contenido,
            entity.SisForm,
            entity.ObsForm,
            entity.ExtForm,
            entity.Alto,
            entity.Ancho);

        await _repo.UpdateAsync(entity);
    }
}
