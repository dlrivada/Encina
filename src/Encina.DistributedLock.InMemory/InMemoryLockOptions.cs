namespace Encina.DistributedLock.InMemory;

/// <summary>
/// Options for the in-memory distributed lock provider.
/// </summary>
public sealed class InMemoryLockOptions : DistributedLockOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to log a warning when the provider is used.
    /// </summary>
    /// <remarks>
    /// This is useful to remind developers that in-memory locks are not suitable for production
    /// multi-instance deployments. Default is <c>true</c>.
    /// </remarks>
    public bool WarnOnUse { get; set; } = true;
}
