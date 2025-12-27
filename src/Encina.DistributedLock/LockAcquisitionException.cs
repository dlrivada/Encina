namespace Encina.DistributedLock;

/// <summary>
/// Exception thrown when a distributed lock cannot be acquired.
/// </summary>
public sealed class LockAcquisitionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LockAcquisitionException"/> class.
    /// </summary>
    /// <param name="resource">The resource that could not be locked.</param>
    public LockAcquisitionException(string resource)
        : base($"Failed to acquire lock on resource: {resource}")
    {
        Resource = resource;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LockAcquisitionException"/> class.
    /// </summary>
    /// <param name="resource">The resource that could not be locked.</param>
    /// <param name="innerException">The inner exception that caused the failure.</param>
    public LockAcquisitionException(string resource, Exception innerException)
        : base($"Failed to acquire lock on resource: {resource}", innerException)
    {
        Resource = resource;
    }

    /// <summary>
    /// Gets the resource that could not be locked.
    /// </summary>
    public string Resource { get; }
}
