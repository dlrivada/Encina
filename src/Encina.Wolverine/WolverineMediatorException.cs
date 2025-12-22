namespace Encina.Wolverine;

/// <summary>
/// Exception thrown when a Encina operation fails within a Wolverine handler.
/// </summary>
public sealed class WolverineEncinaException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WolverineEncinaException"/> class.
    /// </summary>
    /// <param name="error">The Encina error that caused this exception.</param>
    public WolverineEncinaException(EncinaError error)
        : base(error.Message)
    {
        Error = error;
    }

    /// <summary>
    /// Gets the underlying Encina error.
    /// </summary>
    public EncinaError Error { get; }
}
