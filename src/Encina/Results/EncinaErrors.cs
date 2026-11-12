using System.Collections.Immutable;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina;

/// <summary>
/// Factory for the standard errors produced by Encina.
/// </summary>
public static class EncinaErrors
{
    private const string CategoryOverloadReason =
        "The categorized overloads sit next to the category-less ones only during the staged migration of ADR-062; "
        + "the category is a required, non-optional parameter, so no call binds ambiguously, and F2 makes the "
        + "category-less overloads internal.";

    /// <summary>
    /// Unexpected infrastructure error.
    /// </summary>
    public static EncinaError Unknown { get; } = Create("Encina.unknown", ErrorCategory.Internal, "An unexpected error occurred in Encina.");

    /// <summary>
    /// Creates an error with explicit code and message and no category.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="exception">Optional exception that caused the error.</param>
    /// <param name="details">Optional structured metadata as a dictionary.</param>
    /// <returns>An EncinaError with the specified details.</returns>
    /// <remarks>
    /// Prefer <see cref="Create(string, ErrorCategory, string, Exception?, IReadOnlyDictionary{string, object?}?)"/>.
    /// Until the migration of ADR-062 ends, the category of an error created here is inferred from the code text.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = CategoryOverloadReason)]
    public static EncinaError Create(
        string code,
        string message,
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? details = null)
        => EncinaError.FromEncinaException(new EncinaException(code, message, exception, details));

    /// <summary>
    /// Creates an error with an explicit code, category and message.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="category">The protocol-neutral category; each host maps it to its own status (ADR-062).</param>
    /// <param name="message">The error message.</param>
    /// <param name="exception">Optional exception that caused the error.</param>
    /// <param name="details">
    /// Optional structured metadata as a dictionary; see <see cref="EncinaErrorMetadataKeys"/> for the keys hosts read.
    /// </param>
    /// <returns>An EncinaError whose <see cref="EncinaErrorExtensions.GetCategory(EncinaError)"/> is <paramref name="category"/>.</returns>
    /// <example>
    /// <code>
    /// EncinaErrors.Create("orders.not_found", ErrorCategory.NotFound, "Order 42 was not found.");
    /// </code>
    /// </example>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = CategoryOverloadReason)]
    public static EncinaError Create(
        string code,
        ErrorCategory category,
        string message,
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? details = null)
        => EncinaError.FromEncinaException(new EncinaException(code, message, exception, details, category));

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

        return Create(EncinaErrorCodes.AuthorizationForbidden, ErrorCategory.Forbidden, message, details: details);
    }

    /// <summary>
    /// Creates an authorization denial: an authenticated identity is not allowed to perform the operation (403).
    /// </summary>
    /// <param name="details">Optional structured metadata as a dictionary.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="EncinaErrorCodes.AuthorizationUnauthorized"/>.</returns>
    /// <remarks>Use <see cref="Unauthenticated"/> when the caller has no authenticated identity (401).</remarks>
    public static EncinaError Unauthorized(IReadOnlyDictionary<string, object?>? details = null)
        => Create(EncinaErrorCodes.AuthorizationUnauthorized, ErrorCategory.Forbidden, "Access denied.", details: details);

    /// <summary>
    /// Creates an authentication error: the request needs an authenticated identity and has none (401).
    /// </summary>
    /// <param name="details">Optional structured metadata as a dictionary.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="EncinaErrorCodes.AuthorizationUnauthenticated"/>.</returns>
    public static EncinaError Unauthenticated(IReadOnlyDictionary<string, object?>? details = null)
        => Create(EncinaErrorCodes.AuthorizationUnauthenticated, ErrorCategory.Unauthenticated, "Authentication is required.", details: details);

    /// <summary>
    /// Wraps an exception inside a typed error with no category.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="exception">The exception to wrap.</param>
    /// <param name="message">Optional message override. If null, uses the exception message.</param>
    /// <param name="details">Optional structured metadata as a dictionary.</param>
    /// <returns>An EncinaError wrapping the exception.</returns>
    /// <remarks>
    /// Prefer <see cref="FromException(string, ErrorCategory, Exception, string?, IReadOnlyDictionary{string, object?}?)"/>.
    /// Until the migration of ADR-062 ends, the category of an error created here is inferred from the code text.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = CategoryOverloadReason)]
    public static EncinaError FromException(
        string code,
        Exception exception,
        string? message = null,
        IReadOnlyDictionary<string, object?>? details = null)
        => Create(code, message ?? exception.Message, exception, details);

    /// <summary>
    /// Wraps an exception inside a typed error with an explicit category.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="category">The protocol-neutral category; each host maps it to its own status (ADR-062).</param>
    /// <param name="exception">The exception to wrap.</param>
    /// <param name="message">Optional message override. If null, uses the exception message.</param>
    /// <param name="details">
    /// Optional structured metadata as a dictionary; see <see cref="EncinaErrorMetadataKeys"/> for the keys hosts read.
    /// </param>
    /// <returns>An EncinaError wrapping the exception, whose category is <paramref name="category"/>.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = CategoryOverloadReason)]
    public static EncinaError FromException(
        string code,
        ErrorCategory category,
        Exception exception,
        string? message = null,
        IReadOnlyDictionary<string, object?>? details = null)
        => Create(code, category, message ?? exception.Message, exception, details);
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
    IReadOnlyDictionary<string, object?>? details,
    ErrorCategory? category = null) : Exception(message, innerException)
{
    public string Code { get; } = code;

    public IReadOnlyDictionary<string, object?> Details { get; } = details ?? ImmutableDictionary<string, object?>.Empty;

    /// <summary>
    /// The category given when the error was created, or <c>null</c> when it was created without one (the
    /// category is then inferred from the code until ADR-062's migration ends).
    /// </summary>
    public ErrorCategory? Category { get; } = category;
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
    /// Gets the protocol-neutral category of the error (ADR-062).
    /// </summary>
    /// <param name="error">The Encina error.</param>
    /// <returns>
    /// The category given when the error was created; <see cref="ErrorCategory.Internal"/> when the error has no code
    /// (for example <see cref="EncinaError.New(string)"/>); otherwise, during the staged migration only, the category
    /// inferred from the code text.
    /// </returns>
    /// <example>
    /// <code>
    /// if (error.GetCategory() == ErrorCategory.NotFound) { /* answer 404 */ }
    /// </code>
    /// </example>
    public static ErrorCategory GetCategory(this EncinaError error)
    {
        return error.MetadataException.Match(
            Some: CategoryOf,
            None: () => ErrorCategory.Internal);
    }

    private static ErrorCategory CategoryOf(Exception exception)
        => exception is EncinaException encinaException
            ? encinaException.Category ?? LegacyErrorCategoryInference.Infer(encinaException.Code)
            : ErrorCategory.Internal;

    /// <summary>
    /// Gets the exception that caused the error, if any.
    /// </summary>
    /// <param name="error">The Encina error.</param>
    /// <returns>
    /// The causing exception, or <c>None</c> when the error was created without one. Unlike
    /// <see cref="EncinaError.Exception"/>, this never returns the internal carrier that
    /// <see cref="EncinaErrors"/> uses to hold the code and details, whose message is the
    /// error message itself.
    /// </returns>
    /// <remarks>
    /// Use it when rethrowing an error as an exception (for example in a background-job adapter):
    /// passing <see cref="EncinaError.Exception"/> as the inner exception would copy
    /// <see cref="EncinaError.Message"/>, which may contain personal data, into the exception chain.
    /// </remarks>
    public static Option<Exception> GetCause(this EncinaError error)
    {
        return error.Exception.Filter(ex => ex is not EncinaException);
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
        // The None branch never falls back to error.Message: an EncinaError created via
        // EncinaError.New(string) or the implicit string conversion has no code, and the message
        // may carry personal data. "Encina.unknown" is the safe, stable fallback (#1319).
        return error.MetadataException.Match(
            Some: ex => ex switch
            {
                EncinaException enEx => enEx.Code,
                _ => ex.GetType().Name
            },
            None: () => "Encina.unknown");
    }

    // Internal method for compatibility
    internal static IReadOnlyDictionary<string, object?> GetEncinaDetails(this EncinaError error)
        => GetDetails(error);

    // Internal method for compatibility
    internal static IReadOnlyDictionary<string, object?> GetEncinaMetadata(this EncinaError error)
        => GetDetails(error);
}
