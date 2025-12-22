namespace Encina.Wolverine;

/// <summary>
/// Exception thrown when a Encina operation fails within a Wolverine handler.
/// </summary>
public sealed class WolverineMediatorException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WolverineMediatorException"/> class.
    /// </summary>
    /// <param name="error">The mediator error that caused this exception.</param>
    public WolverineMediatorException(MediatorError error)
        : base(error.Message)
    {
        Error = error;
    }

    /// <summary>
    /// Gets the underlying mediator error.
    /// </summary>
    public MediatorError Error { get; }
}
