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
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="exception">Optional exception that caused the error.</param>
    /// <param name="details">Optional structured metadata as a dictionary.</param>
    /// <returns>An EncinaError with the specified details.</returns>
    public static EncinaError Create(
        string code,
        string message,
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? details = null)
        => EncinaError.FromEncinaException(new EncinaException(code, message, exception, details));

    /// <summary>
    /// Creates an authorization forbidden error, optionally referencing the policy that was not satisfied.
    /// </summary>
    /// <param name="policy">The name of the authorization policy that failed, or <c>null</c> if not applicable.</param>
    /// <param name="details">Optional structured metadata as a dictionary.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="EncinaErrorCodes.AuthorizationForbidden"/>.</returns>
    public static EncinaError Forbidden(string? policy = null, IReadOnlyDictionary<string, object?>? details = null)
    {
        var message = policy is not null
            ? $"Access denied. Policy '{policy}' was not satisfied."
            : "Access denied.";

        return Create(EncinaErrorCodes.AuthorizationForbidden, message, details: details);
    }

    /// <summary>
    /// Creates an authorization unauthorized error indicating the user is not authenticated.
    /// </summary>
    /// <param name="details">Optional structured metadata as a dictionary.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="EncinaErrorCodes.AuthorizationUnauthorized"/>.</returns>
    public static EncinaError Unauthorized(IReadOnlyDictionary<string, object?>? details = null)
        => Create(EncinaErrorCodes.AuthorizationUnauthorized, "Authentication is required.", details: details);

    /// <summary>
    /// Wraps an exception inside a typed error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="exception">The exception to wrap.</param>
    /// <param name="message">Optional message override. If null, uses the exception message.</param>
    /// <param name="details">Optional structured metadata as a dictionary.</param>
    /// <returns>An EncinaError wrapping the exception.</returns>
    public static EncinaError FromException(
        string code,
        Exception exception,
        string? message = null,
        IReadOnlyDictionary<string, object?>? details = null)
        => Create(code, message ?? exception.Message, exception, details);
}

/// <summary>
/// Internal exception used to capture Encina failure metadata without leaking it.
/// </summary>
/// <remarks>
/// This exception is intentionally internal to encapsulate error metadata
/// without exposing implementation details to library consumers.
/// </remarks>
#pragma warning disable S3871 // Exception types should be "public" - Intentionally internal for encapsulation
internal sealed class EncinaException(
    string code,
    string message,
    Exception? innerException,
    IReadOnlyDictionary<string, object?>? details) : Exception(message, innerException)
{
    public string Code { get; } = code;

    public IReadOnlyDictionary<string, object?> Details { get; } = details ?? ImmutableDictionary<string, object?>.Empty;
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
                EncinaException enEx => Some(enEx.Code),
                _ => None
            },
            None: () => None);
    }

    /// <summary>
    /// Gets the error details dictionary from the Encina error.
    /// </summary>
    /// <param name="error">The Encina error.</param>
    /// <returns>The error details dictionary, or empty if not available.</returns>
    public static IReadOnlyDictionary<string, object?> GetDetails(this EncinaError error)
    {
        var details = error.MetadataException.MatchUnsafe(
            ex => ex switch
            {
                EncinaException enEx => enEx.Details,
                _ => ImmutableDictionary<string, object?>.Empty
            },
            () => ImmutableDictionary<string, object?>.Empty);

        return details ?? ImmutableDictionary<string, object?>.Empty;
    }

    /// <summary>
    /// Gets the error metadata dictionary from the Encina error.
    /// </summary>
    /// <param name="error">The Encina error.</param>
    /// <returns>The error metadata dictionary, or empty if not available.</returns>
    /// <remarks>This is an alias for <see cref="GetDetails"/>.</remarks>
    public static IReadOnlyDictionary<string, object?> GetMetadata(this EncinaError error)
        => GetDetails(error);

    // Internal method for compatibility
    internal static string GetEncinaCode(this EncinaError error)
    {
        return error.MetadataException.Match(
            Some: ex => ex switch
            {
                EncinaException enEx => enEx.Code,
                _ => ex.GetType().Name
            },
            None: () => string.IsNullOrWhiteSpace(error.Message) ? "Encina.unknown" : error.Message);
    }

    // Internal method for compatibility
    internal static IReadOnlyDictionary<string, object?> GetEncinaDetails(this EncinaError error)
        => GetDetails(error);

    // Internal method for compatibility
    internal static IReadOnlyDictionary<string, object?> GetEncinaMetadata(this EncinaError error)
        => GetDetails(error);
}
