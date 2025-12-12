using System.Collections.Immutable;

namespace SimpleMediator;

/// <summary>
/// Factory for the standard errors produced by SimpleMediator.
/// </summary>
internal static class MediatorErrors
{
    /// <summary>
    /// Unexpected infrastructure error.
    /// </summary>
    public static MediatorError Unknown { get; } = Create("mediator.unknown", "An unexpected error occurred in SimpleMediator.");

    /// <summary>
    /// Creates an error with explicit code and message.
    /// </summary>
    public static MediatorError Create(string code, string message, Exception? exception = null, object? details = null)
        => MediatorError.FromMediatorException(new MediatorException(code, message, exception, details));

    /// <summary>
    /// Wraps an exception inside a typed error.
    /// </summary>
    public static MediatorError FromException(string code, Exception exception, string? message = null, object? details = null)
        => Create(code, message ?? exception.Message, exception, details);
}

/// <summary>
/// Internal exception used to capture mediator failure metadata without leaking it.
/// </summary>
internal sealed class MediatorException(string code, string message, Exception? innerException, object? details) : Exception(message, innerException)
{
    public string Code { get; } = code;

    public object? Details { get; } = details;

    public IReadOnlyDictionary<string, object?> Metadata { get; } = NormalizeMetadata(details);

    private static IReadOnlyDictionary<string, object?> NormalizeMetadata(object? details)
    {
        if (details is null)
        {
            return ImmutableDictionary<string, object?>.Empty;
        }

        if (details is IReadOnlyDictionary<string, object?> dict)
        {
            return dict ?? ImmutableDictionary<string, object?>.Empty;
        }

        return ImmutableDictionary<string, object?>.Empty.Add("detail", details);
    }
}

/// <summary>
/// Helper extensions to extract metadata from <see cref="MediatorError"/>.
/// </summary>
internal static class MediatorErrorExtensions
{
    public static string GetMediatorCode(this MediatorError error)
    {
        return error.MetadataException.Match(
            Some: ex => ex switch
            {
                MediatorException mediatorException => mediatorException.Code,
                _ => ex.GetType().Name
            },
            None: () => string.IsNullOrWhiteSpace(error.Message) ? "mediator.unknown" : error.Message);
    }

    public static object? GetMediatorDetails(this MediatorError error)
    {
        return error.MetadataException.MatchUnsafe(
            ex => ex switch
            {
                MediatorException mediatorException => mediatorException.Details,
                _ => null
            },
            () => null);
    }

    public static IReadOnlyDictionary<string, object?> GetMediatorMetadata(this MediatorError error)
    {
        var metadata = error.MetadataException.MatchUnsafe(
            ex => ex switch
            {
                MediatorException mediatorException => mediatorException.Metadata,
                _ => (IReadOnlyDictionary<string, object?>)ImmutableDictionary<string, object?>.Empty
            },
            () => ImmutableDictionary<string, object?>.Empty);

        return metadata ?? ImmutableDictionary<string, object?>.Empty;
    }
}
