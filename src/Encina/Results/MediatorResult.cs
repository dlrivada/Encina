using System.Collections.Immutable;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina;

/// <summary>
/// Factory for the standard errors produced by Encina.
/// </summary>
public static class MediatorErrors
{
    /// <summary>
    /// Unexpected infrastructure error.
    /// </summary>
    public static MediatorError Unknown { get; } = Create("mediator.unknown", "An unexpected error occurred in Encina.");

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
public static class MediatorErrorExtensions
{
    /// <summary>
    /// Gets the error code from the mediator error.
    /// </summary>
    /// <param name="error">The mediator error.</param>
    /// <returns>The error code if available.</returns>
    public static Option<string> GetCode(this MediatorError error)
    {
        return error.MetadataException.Match(
            Some: ex => ex switch
            {
                MediatorException mediatorException => Some(mediatorException.Code),
                _ => None
            },
            None: () => None);
    }

    /// <summary>
    /// Gets the error details from the mediator error.
    /// </summary>
    /// <param name="error">The mediator error.</param>
    /// <returns>The error details if available.</returns>
    public static Option<object> GetDetails(this MediatorError error)
    {
        return error.MetadataException.Bind(ex => ex switch
        {
            MediatorException mediatorException when mediatorException.Details is not null
                => Some(mediatorException.Details),
            _ => None
        });
    }

    /// <summary>
    /// Gets the error metadata dictionary from the mediator error.
    /// </summary>
    /// <param name="error">The mediator error.</param>
    /// <returns>The error metadata dictionary, or empty if not available.</returns>
    public static IReadOnlyDictionary<string, object?> GetMetadata(this MediatorError error)
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

    // Internal method for compatibility
    internal static string GetMediatorCode(this MediatorError error)
    {
        return error.MetadataException.Match(
            Some: ex => ex switch
            {
                MediatorException mediatorException => mediatorException.Code,
                _ => ex.GetType().Name
            },
            None: () => string.IsNullOrWhiteSpace(error.Message) ? "mediator.unknown" : error.Message);
    }

    // Internal method for compatibility
    internal static object? GetMediatorDetails(this MediatorError error)
    {
        return error.MetadataException.MatchUnsafe(
            ex => ex switch
            {
                MediatorException mediatorException => mediatorException.Details,
                _ => null
            },
            () => null);
    }

    // Internal method for compatibility
    internal static IReadOnlyDictionary<string, object?> GetMediatorMetadata(this MediatorError error)
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
