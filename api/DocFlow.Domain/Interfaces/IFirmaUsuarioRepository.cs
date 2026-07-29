using DocFlow.Domain.Entities;

namespace DocFlow.Domain.Interfaces;

public interface IFirmaUsuarioRepository
{
    /// <summary>Loads the signature configured for a user, or null if none exists.</summary>
    Task<FirmaUsuario?> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct = default);

    /// <summary>Creates the signature if the user has none, or replaces the existing one (one per user).</summary>
    Task UpsertAsync(FirmaUsuario firma, CancellationToken ct = default);

    /// <summary>Deletes the signature configured for a user. No-op if none exists.</summary>
    Task DeleteAsync(Guid usuarioId, CancellationToken ct = default);
}
