using System.Collections.Immutable;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina;

/// <summary>
/// Factory for the standard errors produced by Encina.
/// </summary>
public static class EncinaErrors
{
    /// <summary>
    /// Unexpected infrastructure error.
    /// </summary>
    public static EncinaError Unknown { get; } = Create("Encina.unknown", "An unexpected error occurred in Encina.");

    /// <summary>
    /// Creates an error with explicit code and message.
    /// </summary>
    public static EncinaError Create(string code, string message, Exception? exception = null, object? details = null)
        => EncinaError.FromEncinaException(new EncinaException(code, message, exception, details));

    /// <summary>
    /// Wraps an exception inside a typed error.
    /// </summary>
    public static EncinaError FromException(string code, Exception exception, string? message = null, object? details = null)
        => Create(code, message ?? exception.Message, exception, details);
}

/// <summary>
/// Internal exception used to capture Encina failure metadata without leaking it.
/// </summary>
internal sealed class EncinaException(string code, string message, Exception? innerException, object? details) : Exception(message, innerException)
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
            return dict;
        }

        return ImmutableDictionary<string, object?>.Empty.Add("detail", details);
    }
}

/// <summary>
/// Helper extensions to extract metadata from <see cref="EncinaError"/>.
/// </summary>
public static class EncinaErrorExtensions
{
    /// <summary>
    /// Gets the error code from the Encina error.
    /// </summary>
    /// <param name="error">The Encina error.</param>
    /// <returns>The error code if available.</returns>
    public static Option<string> GetCode(this EncinaError error)
    {
        return error.MetadataException.Match(
            Some: ex => ex switch
            {
                EncinaException EncinaException => Some(EncinaException.Code),
                _ => None
            },
            None: () => None);
    }

    /// <summary>
    /// Gets the error details from the Encina error.
    /// </summary>
    /// <param name="error">The Encina error.</param>
    /// <returns>The error details if available.</returns>
    public static Option<object> GetDetails(this EncinaError error)
    {
        return error.MetadataException.Bind(ex => ex switch
        {
            EncinaException EncinaException when EncinaException.Details is not null
                => Some(EncinaException.Details),
            _ => None
        });
    }

    /// <summary>
    /// Gets the error metadata dictionary from the Encina error.
    /// </summary>
    /// <param name="error">The Encina error.</param>
    /// <returns>The error metadata dictionary, or empty if not available.</returns>
    public static IReadOnlyDictionary<string, object?> GetMetadata(this EncinaError error)
    {
        var metadata = error.MetadataException.MatchUnsafe(
            ex => ex switch
            {
                EncinaException EncinaException => EncinaException.Metadata,
                _ => ImmutableDictionary<string, object?>.Empty
            },
            () => ImmutableDictionary<string, object?>.Empty);

        return metadata ?? ImmutableDictionary<string, object?>.Empty;
    }

    // Internal method for compatibility
    internal static string GetEncinaCode(this EncinaError error)
    {
        return error.MetadataException.Match(
            Some: ex => ex switch
            {
                EncinaException EncinaException => EncinaException.Code,
                _ => ex.GetType().Name
            },
            None: () => string.IsNullOrWhiteSpace(error.Message) ? "Encina.unknown" : error.Message);
    }

    // Internal method for compatibility
    internal static object? GetEncinaDetails(this EncinaError error)
    {
        return error.MetadataException.MatchUnsafe(
            ex => ex switch
            {
                EncinaException EncinaException => EncinaException.Details,
                _ => null
            },
            () => null);
    }

    // Internal method for compatibility
    internal static IReadOnlyDictionary<string, object?> GetEncinaMetadata(this EncinaError error)
    {
        var metadata = error.MetadataException.MatchUnsafe(
            ex => ex switch
            {
                EncinaException EncinaException => EncinaException.Metadata,
                _ => ImmutableDictionary<string, object?>.Empty
            },
            () => ImmutableDictionary<string, object?>.Empty);

        return metadata ?? ImmutableDictionary<string, object?>.Empty;
    }
}
