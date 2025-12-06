using System;

namespace SimpleMediator;

/// <summary>
/// Implementación nula de <see cref="IFunctionalFailureDetector"/> que nunca detecta fallos.
/// </summary>
/// <remarks>
/// Se usa como valor por defecto para evitar comprobaciones de <c>null</c> en behaviors.
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
