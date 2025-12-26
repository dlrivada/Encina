namespace Encina.Messaging.RoutingSlip;

/// <summary>
/// Abstraction for routing slip persistence.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface for each data provider (EF Core, Dapper, etc.).
/// </para>
/// </remarks>
public interface IRoutingSlipStore
{
    /// <summary>
    /// Gets a routing slip by its identifier.
    /// </summary>
    /// <param name="routingSlipId">The routing slip identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The routing slip state, or null if not found.</returns>
    Task<IRoutingSlipState?> GetAsync(Guid routingSlipId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new routing slip.
    /// </summary>
    /// <param name="state">The routing slip state to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(IRoutingSlipState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing routing slip.
    /// </summary>
    /// <param name="state">The routing slip state to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(IRoutingSlipState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets routing slips that appear to be stuck (not updated recently).
    /// </summary>
    /// <param name="olderThan">The threshold for considering a routing slip stuck.</param>
    /// <param name="batchSize">Maximum number of routing slips to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of stuck routing slips.</returns>
    Task<IReadOnlyList<IRoutingSlipState>> GetStuckAsync(
        TimeSpan olderThan,
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets routing slips that have exceeded their timeout.
    /// </summary>
    /// <param name="batchSize">Maximum number of routing slips to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of expired routing slips.</returns>
    Task<IReadOnlyList<IRoutingSlipState>> GetExpiredAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves pending changes to the store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// For providers that support unit of work (like EF Core), this commits
    /// pending changes. For other providers, this may be a no-op.
    /// </remarks>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
