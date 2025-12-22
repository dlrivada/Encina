namespace Encina.MassTransit;

/// <summary>
/// Exception thrown when a MassTransit consumer encounters a mediator error.
/// </summary>
public sealed class MediatorConsumerException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediatorConsumerException"/> class.
    /// </summary>
    /// <param name="error">The mediator error that caused the exception.</param>
    public MediatorConsumerException(MediatorError error)
        : base($"Mediator error: {error.Message}")
    {
        MediatorError = error;
    }

    /// <summary>
    /// Gets the mediator error that caused this exception.
    /// </summary>
    public MediatorError MediatorError { get; }
}
