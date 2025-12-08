using System;

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

    public bool TryExtractFailure(object? response, out string reason, out object? error)
    {
        reason = string.Empty;
        error = null;
        return false;
    }

    public string? TryGetErrorCode(object? error) => null;

    public string? TryGetErrorMessage(object? error) => null;
}
