namespace Encina.DistributedLock;

/// <summary>
/// Provides distributed locking capabilities for coordinating access to shared resources.
/// </summary>
/// <remarks>
/// <para>
/// Distributed locks are essential for:
/// </para>
/// <list type="bullet">
/// <item><description>Saga coordination - Preventing concurrent execution of the same saga</description></item>
/// <item><description>Cache stampede prevention - Ensuring only one process regenerates cache</description></item>
/// <item><description>Resource protection - Coordinating access to external resources</description></item>
/// <item><description>Scheduled task execution - Ensuring only one instance processes a task</description></item>
/// </list>
/// <para>
/// This interface abstracts over different distributed lock implementations:
/// </para>
/// <list type="bullet">
/// <item><description>Redis (Redlock algorithm) - High availability locking</description></item>
/// <item><description>SQL Server (sp_getapplock) - Database-level locking</description></item>
/// <item><description>Memory - Local process locking (single instance only, for testing)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class PaymentSagaHandler
/// {
///     private readonly IDistributedLockProvider _lockProvider;
///
///     public async Task ProcessPaymentAsync(string orderId, CancellationToken ct)
///     {
///         var lockResource = $"saga:payment:{orderId}";
///
///         await using var @lock = await _lockProvider.TryAcquireAsync(
///             resource: lockResource,
///             expiry: TimeSpan.FromMinutes(5),
///             wait: TimeSpan.FromSeconds(30),
///             retry: TimeSpan.FromMilliseconds(500),
///             cancellationToken: ct);
///
///         if (@lock is null)
///         {
///             throw new LockAcquisitionException(lockResource);
///         }
///
///         // Process payment safely - no other instance can process this order
///         await ExecutePaymentStepsAsync(orderId, ct);
///     }
/// }
/// </code>
/// </example>
public interface IDistributedLockProvider
{
    /// <summary>
    /// Attempts to acquire a distributed lock on a resource.
    /// </summary>
    /// <param name="resource">The resource identifier to lock.</param>
    /// <param name="expiry">How long the lock should be held before auto-release.</param>
    /// <param name="wait">Maximum time to wait for the lock to become available.</param>
    /// <param name="retry">Time between retry attempts while waiting.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="IAsyncDisposable"/> that releases the lock when disposed,
    /// or <c>null</c> if the lock could not be acquired within the wait time.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The lock is automatically released when the returned handle is disposed.
    /// It is also released after the expiry time, even if not explicitly disposed.
    /// </para>
    /// <para>
    /// For Redis-based implementations, this typically uses the Redlock algorithm
    /// for high availability across multiple Redis instances.
    /// </para>
    /// </remarks>
    Task<IAsyncDisposable?> TryAcquireAsync(
        string resource,
        TimeSpan expiry,
        TimeSpan wait,
        TimeSpan retry,
        CancellationToken cancellationToken);

    /// <summary>
    /// Acquires a distributed lock on a resource, waiting indefinitely if necessary.
    /// </summary>
    /// <param name="resource">The resource identifier to lock.</param>
    /// <param name="expiry">How long the lock should be held before auto-release.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An <see cref="IAsyncDisposable"/> that releases the lock when disposed.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the cancellation token is triggered.</exception>
    /// <exception cref="LockAcquisitionException">Thrown if the lock cannot be acquired due to an error.</exception>
    Task<IAsyncDisposable> AcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a resource is currently locked.
    /// </summary>
    /// <param name="resource">The resource identifier to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the resource is locked; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This is a point-in-time check. The lock status may change immediately after this call returns.
    /// Do not use this for synchronization - use <see cref="TryAcquireAsync"/> instead.
    /// </remarks>
    Task<bool> IsLockedAsync(string resource, CancellationToken cancellationToken);

    /// <summary>
    /// Extends the expiry time of an existing lock.
    /// </summary>
    /// <param name="resource">The resource identifier of the lock to extend.</param>
    /// <param name="extension">The additional time to add to the lock expiry.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the lock was extended; <c>false</c> if the lock was not held.</returns>
    /// <remarks>
    /// This is useful for long-running operations where the lock may need to be held
    /// longer than initially anticipated.
    /// </remarks>
    Task<bool> ExtendAsync(string resource, TimeSpan extension, CancellationToken cancellationToken);
}
