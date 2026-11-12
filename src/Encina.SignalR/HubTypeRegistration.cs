using System.Globalization;
using System.Text;

namespace Encina.SignalR;

/// <summary>
/// The kind of message an allow-list entry may be invoked as; each <see cref="EncinaHub"/>
/// method accepts only its own kind.
/// </summary>
internal enum HubMessageKind
{
    /// <summary>A command, invoked through <see cref="EncinaHub.SendCommand"/>.</summary>
    Command,

    /// <summary>A query, invoked through <see cref="EncinaHub.SendQuery"/>.</summary>
    Query,

    /// <summary>A notification, invoked through <see cref="EncinaHub.PublishNotification"/>.</summary>
    Notification,
}

/// <summary>
/// One type the host exposed to <see cref="EncinaHub"/> clients through
/// <see cref="SignalROptions.AllowRequest(Type, bool)"/> or
/// <see cref="SignalROptions.AllowNotification(Type, bool)"/>.
/// </summary>
/// <param name="Type">The registered CLR type; the hub deserializes only to this type.</param>
/// <param name="Kind">The hub method kind the type may be invoked through.</param>
/// <param name="AllowAnonymous">Whether an unauthenticated caller may invoke the type.</param>
internal sealed record HubTypeRegistration(Type Type, HubMessageKind Kind, bool AllowAnonymous);

/// <summary>
/// Makes a client-supplied type name safe to write to a log: bounded length and no characters
/// that could forge or hide log lines.
/// </summary>
internal static class ClientTypeName
{
    /// <summary>The maximum number of characters of a client-supplied name that reach a log.</summary>
    internal const int MaxLoggedLength = 64;

    /// <summary>
    /// Returns at most the first <see cref="MaxLoggedLength"/> characters of <paramref name="value"/>,
    /// without control, format, separator or surrogate characters. The work is bounded by
    /// <see cref="MaxLoggedLength"/> whatever the length of the input.
    /// </summary>
    /// <param name="value">The raw client string; <see langword="null"/> is allowed.</param>
    /// <returns>The sanitized, truncated name.</returns>
    internal static string ForLog(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var head = value.AsSpan(0, Math.Min(value.Length, MaxLoggedLength));
        var builder = new StringBuilder(head.Length);
        foreach (var character in head)
        {
            if (IsLoggable(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    // Control and format characters (CR, LF, ESC, bidirectional overrides) can forge or hide log
    // lines; line/paragraph separators break lines; a surrogate may be left unpaired by truncation.
    private static bool IsLoggable(char character)
        => !char.IsControl(character)
           && !char.IsSurrogate(character)
           && char.GetUnicodeCategory(character) is not (UnicodeCategory.Format
               or UnicodeCategory.LineSeparator
               or UnicodeCategory.ParagraphSeparator);
}
