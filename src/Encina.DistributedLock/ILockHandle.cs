namespace Encina.DistributedLock;

/// <summary>
/// Represents an acquired distributed lock.
/// </summary>
/// <remarks>
/// <para>
/// The lock is released when this handle is disposed.
/// </para>
/// <para>
/// This interface extends <see cref="IAsyncDisposable"/> and provides
/// additional information about the lock state.
/// </para>
/// </remarks>
public interface ILockHandle : IAsyncDisposable
{
    /// <summary>
    /// Gets the resource that is locked.
    /// </summary>
    string Resource { get; }

    /// <summary>
    /// Gets the unique identifier for this lock acquisition.
    /// </summary>
    /// <remarks>
    /// This can be used to verify lock ownership when extending locks.
    /// </remarks>
    string LockId { get; }

    /// <summary>
    /// Gets the time when this lock was acquired.
    /// </summary>
    DateTime AcquiredAtUtc { get; }

    /// <summary>
    /// Gets the time when this lock will expire.
    /// </summary>
    DateTime ExpiresAtUtc { get; }

    /// <summary>
    /// Gets a value indicating whether this lock has been released.
    /// </summary>
    bool IsReleased { get; }

    /// <summary>
    /// Extends the lock expiry by the specified duration.
    /// </summary>
    /// <param name="extension">The duration to extend the lock by.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the lock was extended; <c>false</c> if the lock was no longer held.</returns>
    Task<bool> ExtendAsync(TimeSpan extension, CancellationToken cancellationToken = default);
}
