using System.Text.Json;

namespace Encina.Security.Sanitization.Sanitizers;

/// <summary>
/// Provides sanitization for JSON string contexts using the
/// <see cref="JsonSerializer"/> for spec-compliant escaping.
/// </summary>
/// <remarks>
/// Escapes control characters, quotes, and backslashes according to
/// the JSON specification (RFC 8259) by leveraging <c>JsonSerializer.Serialize</c>.
/// </remarks>
internal static class JsonSanitizer
{
    /// <summary>
    /// Sanitizes input for safe embedding in a JSON string value.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <returns>The sanitized string with JSON special characters escaped.</returns>
    /// <remarks>
    /// Uses <see cref="JsonSerializer"/> to produce spec-compliant escaping,
    /// then strips the surrounding double quotes from the serialized output.
    /// </remarks>
    internal static string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // JsonSerializer.Serialize wraps the string in quotes and escapes
        // all special characters per RFC 8259
        var serialized = JsonSerializer.Serialize(input);

        // Strip the surrounding double quotes added by the serializer
        return serialized[1..^1];
    }
}
