using DocFlow.Domain.Enums;

namespace DocFlow.Application.Common.Interfaces;

/// <summary>
/// Best-effort resolution of workflow-step responsible names. Implemented in Infrastructure
/// with a single batched query per <see cref="ResponsableFlujoTipo"/> to avoid N+1.
/// </summary>
public interface IResponsableFlujoNombreResolver
{
    /// <summary>
    /// Returns a map from responsible id to display name for the given <paramref name="tipo"/>.
    /// Ids that cannot be resolved are simply absent from the map (best-effort).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> ResolverNombresAsync(
        ResponsableFlujoTipo tipo,
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);
}
