namespace Encina;

/// <summary>
/// Transitional inference of an <see cref="ErrorCategory"/> from the text of an error code, for errors created
/// without an explicit category.
/// </summary>
/// <remarks>
/// <para>
/// These are the suffix and prefix rules of the hosts' former <c>MapErrorCodeToStatusCode</c>, moved verbatim
/// into core and returning a category instead of a status, so a producer that has not migrated yet keeps its
/// HTTP status. <c>.already_exists</c> and <c>.duplicate</c> answer <see cref="ErrorCategory.AlreadyExists"/>
/// and <c>.conflict</c> answers <see cref="ErrorCategory.Conflict"/>; both are 409, as before.
/// </para>
/// <para>
/// This is a staged migration step approved by the maintainer on 2026-11-11 (#3419), not a compatibility layer:
/// it exists only until sub-issue F2, which deletes this class and makes a category-less error
/// <see cref="ErrorCategory.Internal"/> (ADR-062).
/// </para>
/// </remarks>
internal static class LegacyErrorCategoryInference
{
    private static readonly (Func<string, bool> Matches, ErrorCategory Category)[] Rules =
    [
        (IsValidationCode, ErrorCategory.Validation),
        (IsUnauthenticatedCode, ErrorCategory.Unauthenticated),
        (IsAuthorizationCode, ErrorCategory.Forbidden),
        (IsNotFoundCode, ErrorCategory.NotFound),
        (IsConflictCode, ErrorCategory.Conflict),
        (IsAlreadyExistsCode, ErrorCategory.AlreadyExists),
    ];

    /// <summary>
    /// Infers the category of an error code; the first matching rule wins and no match is
    /// <see cref="ErrorCategory.Internal"/>.
    /// </summary>
    /// <param name="code">The error code, compared case-insensitively; <c>null</c> or empty is <see cref="ErrorCategory.Internal"/>.</param>
    /// <returns>The inferred category.</returns>
    internal static ErrorCategory Infer(string? code)
    {
        var normalized = (code ?? string.Empty).ToLowerInvariant();

        foreach (var (matches, category) in Rules)
        {
            if (matches(normalized))
            {
                return category;
            }
        }

        return ErrorCategory.Internal;
    }

    private static bool IsValidationCode(string code) =>
        code.StartsWith("validation.", StringComparison.Ordinal) || code == "encina.guard.validation_failed";

    private static bool IsUnauthenticatedCode(string code) =>
        code == EncinaErrorCodes.AuthorizationUnauthenticated;

    // Every other encina.authorization.* code is an authenticated identity that is denied.
    private static bool IsAuthorizationCode(string code) =>
        code.StartsWith(EncinaErrorCodes.AuthorizationPrefix, StringComparison.Ordinal);

    private static bool IsNotFoundCode(string code) =>
        code.EndsWith(".not_found", StringComparison.Ordinal) ||
        code == "encina.request.handler_missing" ||
        code.EndsWith(".missing", StringComparison.Ordinal);

    private static bool IsConflictCode(string code) =>
        code.EndsWith(".conflict", StringComparison.Ordinal);

    private static bool IsAlreadyExistsCode(string code) =>
        code.EndsWith(".already_exists", StringComparison.Ordinal) ||
        code.EndsWith(".duplicate", StringComparison.Ordinal);
}
