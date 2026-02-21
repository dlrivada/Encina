using System.Runtime.InteropServices;
using System.Text;

namespace Encina.Security.Sanitization.Sanitizers;

/// <summary>
/// Provides sanitization for shell/command-line contexts with OS-aware escaping.
/// </summary>
/// <remarks>
/// <para>
/// On Windows, escapes <c>cmd.exe</c> metacharacters using the caret (<c>^</c>) escape character.
/// On Unix-like systems, wraps the input in single quotes and escapes embedded single quotes,
/// following POSIX shell quoting rules.
/// </para>
/// </remarks>
internal static class ShellSanitizer
{
    /// <summary>
    /// Characters that have special meaning in Windows cmd.exe.
    /// </summary>
    private static readonly char[] WindowsMetaChars =
    [
        '&', '|', '<', '>', '^', '(', ')', '@', '!', '%', '"', '\''
    ];

    /// <summary>
    /// Sanitizes input for safe use in shell contexts.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <returns>The sanitized string with shell metacharacters escaped.</returns>
    internal static string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? SanitizeForWindows(input)
            : SanitizeForUnix(input);
    }

    /// <summary>
    /// Escapes cmd.exe metacharacters using the caret (^) escape character.
    /// </summary>
    private static string SanitizeForWindows(string input)
    {
        var sb = new StringBuilder(input.Length * 2);

        foreach (var c in input)
        {
            if (Array.IndexOf(WindowsMetaChars, c) >= 0)
            {
                sb.Append('^');
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes for POSIX shells by wrapping in single quotes.
    /// Existing single quotes are escaped as: ' → '\''
    /// </summary>
    private static string SanitizeForUnix(string input)
    {
        // In POSIX shells, single-quoted strings treat everything literally
        // except single quotes themselves. To include a single quote:
        // end the quoted string, add an escaped quote, restart quoted string.
        // Example: "it's" → 'it'\''s'
        var escaped = input.Replace("'", @"'\''", StringComparison.Ordinal);
        return $"'{escaped}'";
    }
}
