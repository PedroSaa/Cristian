using System.IO;
using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeForplaPdf;

public record GetSeForplaPdfQuery(string CodForm) : IRequest<(byte[] FileBytes, string FileName)>;

public class GetSeForplaPdfQueryHandler : IRequestHandler<GetSeForplaPdfQuery, (byte[] FileBytes, string FileName)>
{
    private readonly ISeForplaRepository _repo;
    private readonly IOnlyOfficeDocumentService _conversionService;

    public GetSeForplaPdfQueryHandler(
        ISeForplaRepository repo,
        IOnlyOfficeDocumentService conversionService)
    {
        _repo = repo;
        _conversionService = conversionService;
    }

    public async Task<(byte[] FileBytes, string FileName)> Handle(GetSeForplaPdfQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.CodForm)
            ?? throw new KeyNotFoundException($"Plantilla {request.CodForm} no encontrada.");

        var pdfBytes = await _conversionService.ConvertToPdfAsync(entity.BlobForm, ct);
        if (!LooksLikePdf(pdfBytes))
            throw new InvalidOperationException("No hay un conversor PDF configurado para previsualizar plantillas.");

        var fileName = Path.ChangeExtension(string.IsNullOrWhiteSpace(entity.NomForm) ? entity.CodForm : entity.NomForm.Trim(), ".pdf");
        return (pdfBytes, fileName);
    }

    private static bool LooksLikePdf(IReadOnlyList<byte> bytes)
        => bytes.Count >= 5
           && bytes[0] == (byte)'%'
           && bytes[1] == (byte)'P'
           && bytes[2] == (byte)'D'
           && bytes[3] == (byte)'F'
           && bytes[4] == (byte)'-';
}
