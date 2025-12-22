namespace Encina.MassTransit;

/// <summary>
/// Exception thrown when a MassTransit consumer encounters a Encina error.
/// </summary>
public sealed class EncinaConsumerException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaConsumerException"/> class.
    /// </summary>
    /// <param name="error">The Encina error that caused the exception.</param>
    public EncinaConsumerException(EncinaError error)
        : base($"Encina error: {error.Message}")
    {
        EncinaError = error;
    }

    /// <summary>
    /// Gets the Encina error that caused this exception.
    /// </summary>
    public EncinaError EncinaError { get; }
}
