using DocFlow.Application.Admin.Auditoria.DTOs;

namespace DocFlow.Application.Admin.Auditoria.Interfaces;

/// <summary>
/// Generates CSV content from audit log entries.
/// </summary>
public interface IAuditoriaCsvService
{
    /// <summary>
    /// Generates a CSV byte array from the given audit records.
    /// </summary>
    byte[] GenerateCsv(IReadOnlyList<RegistroAuditoriaDto> registros);
}
