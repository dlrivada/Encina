namespace SimpleMediator;

/// <summary>
/// Null implementation of <see cref="IFunctionalFailureDetector"/> that never reports failures.
/// </summary>
/// <remarks>
/// Used as the default value to avoid <c>null</c> checks inside behaviors.
/// </remarks>
internal sealed class NullFunctionalFailureDetector : IFunctionalFailureDetector
{
    public static NullFunctionalFailureDetector Instance { get; } = new();

    private NullFunctionalFailureDetector()
    {
    }

    public bool TryExtractFailure(object? response, out string reason, out object? capturedFailure)
    {
        reason = string.Empty;
        capturedFailure = null;
        return false;
    }

    public string? TryGetErrorCode(object? capturedFailure) => null;

    public string? TryGetErrorMessage(object? capturedFailure) => null;
}
