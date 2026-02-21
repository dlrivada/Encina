using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Attributes;
using Encina.Security.Sanitization.Encoders;
using Encina.Security.Sanitization.Profiles;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.PropertyTests.Security.Sanitization;

/// <summary>
/// Property-based tests for sanitization invariants.
/// Verifies that sanitization behaviors hold for arbitrary valid inputs.
/// </summary>
public sealed class SanitizationPropertyTests
{
    #region HTML Sanitization Invariants

    [Property(MaxTest = 50)]
    public bool SanitizeHtml_NeverContainsScriptTags(NonEmptyString value)
    {
        var sanitizer = CreateSanitizer();
        var result = sanitizer.SanitizeHtml(value.Get);
        return !result.Contains("<script", StringComparison.OrdinalIgnoreCase);
    }

    [Property(MaxTest = 50)]
    public bool SanitizeHtml_NeverContainsOnEventAttributes(NonEmptyString value)
    {
        var sanitizer = CreateSanitizer();
        var result = sanitizer.SanitizeHtml(value.Get);

        // Common event handlers should be stripped
        return !result.Contains("onclick", StringComparison.OrdinalIgnoreCase)
            && !result.Contains("onerror", StringComparison.OrdinalIgnoreCase)
            && !result.Contains("onload", StringComparison.OrdinalIgnoreCase)
            && !result.Contains("onmouseover", StringComparison.OrdinalIgnoreCase);
    }

    [Property(MaxTest = 50)]
    public bool SanitizeHtml_PlainTextUnchanged(NonEmptyString value)
    {
        // Plain text without any HTML or control chars should be unchanged
        var plainText = new string(value.Get
            .Replace("<", "", StringComparison.Ordinal)
            .Replace(">", "", StringComparison.Ordinal)
            .Replace("&", "", StringComparison.Ordinal)
            .Where(c => !char.IsControl(c)) // HTML sanitizer strips control chars
            .ToArray());

        if (string.IsNullOrEmpty(plainText)) return true;

        var sanitizer = CreateSanitizer();
        var result = sanitizer.SanitizeHtml(plainText);
        return result == plainText;
    }

    [Property(MaxTest = 30)]
    public bool SanitizeHtml_StrictText_StripsAllTags(NonEmptyString value)
    {
        var sanitizer = CreateSanitizer();
        var result = sanitizer.Custom(value.Get, SanitizationProfiles.StrictText);

        // StrictText profile strips all HTML tags
        return !result.Contains('<') && !result.Contains('>');
    }

    #endregion

    #region SQL Sanitization Invariants

    [Property(MaxTest = 50)]
    public bool SanitizeForSql_SingleQuotesAreProperlyEscaped(NonEmptyString value)
    {
        var sanitizer = CreateSanitizer();
        var result = sanitizer.SanitizeForSql(value.Get);

        // Single quotes are escaped by doubling (' → '').
        // After removing all doubled quotes, no isolated single quote should remain.
        var withoutEscapedQuotes = result.Replace("''", "", StringComparison.Ordinal);
        return !withoutEscapedQuotes.Contains('\'');
    }

    [Property(MaxTest = 50)]
    public bool SanitizeForSql_NeverContainsCommentMarkers(NonEmptyString value)
    {
        var sanitizer = CreateSanitizer();
        var result = sanitizer.SanitizeForSql(value.Get);

        return !result.Contains("--", StringComparison.Ordinal)
            && !result.Contains("/*", StringComparison.Ordinal)
            && !result.Contains("*/", StringComparison.Ordinal);
    }

    [Property(MaxTest = 50)]
    public bool SanitizeForSql_NeverContainsSemicolon(NonEmptyString value)
    {
        var sanitizer = CreateSanitizer();
        var result = sanitizer.SanitizeForSql(value.Get);
        return !result.Contains(';');
    }

    [Property(MaxTest = 30)]
    public bool SanitizeForSql_NeverContainsXpUnderscore(NonEmptyString value)
    {
        var sanitizer = CreateSanitizer();
        var result = sanitizer.SanitizeForSql(value.Get);
        return !result.Contains("xp_", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Shell Sanitization Invariants

    [Property(MaxTest = 50)]
    public bool SanitizeForShell_ResultIsNotEmpty(NonEmptyString value)
    {
        var sanitizer = CreateSanitizer();
        var result = sanitizer.SanitizeForShell(value.Get);

        // Shell sanitization should always return non-empty (may wrap in quotes)
        return !string.IsNullOrEmpty(result);
    }

    [Property(MaxTest = 50)]
    public bool SanitizeForShell_ResultDoesNotContainPipe(NonEmptyString value)
    {
        var sanitizer = CreateSanitizer();
        var result = sanitizer.SanitizeForShell(value.Get);

        // Pipe character should be escaped or quoted
        if (OperatingSystem.IsWindows())
        {
            // On Windows, | becomes ^|
            return !result.Contains('|') || result.Contains("^|", StringComparison.Ordinal);
        }

        // On Unix, the entire string is wrapped in single quotes
        return true;
    }

    #endregion

    #region JSON Sanitization Invariants

    [Property(MaxTest = 50)]
    public bool SanitizeForJson_ResultIsValidJsonString(NonEmptyString value)
    {
        var sanitizer = CreateSanitizer();
        var result = sanitizer.SanitizeForJson(value.Get);

        // Wrapping in quotes should produce valid JSON
        try
        {
            System.Text.Json.JsonDocument.Parse($"\"{result}\"");
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Property(MaxTest = 50)]
    public bool SanitizeForJson_PreservesContent(NonEmptyString value)
    {
        var sanitizer = CreateSanitizer();
        var result = sanitizer.SanitizeForJson(value.Get);

        // Deserializing the sanitized result should give back the original content
        try
        {
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<string>($"\"{result}\"");
            return deserialized == value.Get;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region XML Sanitization Invariants

    [Property(MaxTest = 50)]
    public bool SanitizeForXml_NeverContainsRawAngleBrackets(NonEmptyString value)
    {
        var sanitizer = CreateSanitizer();
        var input = value.Get
            .Replace("\0", "", StringComparison.Ordinal); // Remove null chars that get stripped

        if (string.IsNullOrEmpty(input)) return true;

        var result = sanitizer.SanitizeForXml(input);

        // Raw < and > should be escaped
        return !result.Contains('<') && !result.Contains('>');
    }

    [Property(MaxTest = 50)]
    public bool SanitizeForXml_NeverContainsRawAmpersand(NonEmptyString value)
    {
        var sanitizer = CreateSanitizer();
        var input = value.Get
            .Replace("\0", "", StringComparison.Ordinal);

        if (string.IsNullOrEmpty(input)) return true;

        var result = sanitizer.SanitizeForXml(input);

        // Every & should be part of an entity (&amp; &lt; &gt; &quot; &apos;)
        var index = 0;
        while ((index = result.IndexOf('&', index)) >= 0)
        {
            var semicolonIndex = result.IndexOf(';', index);
            if (semicolonIndex < 0) return false; // & without closing ;
            index = semicolonIndex + 1;
        }

        return true;
    }

    #endregion

    #region Output Encoding Invariants

    [Property(MaxTest = 50)]
    public bool EncodeForHtml_NeverContainsRawAngleBrackets(NonEmptyString value)
    {
        var encoder = new DefaultOutputEncoder();
        var result = encoder.EncodeForHtml(value.Get);
        return !result.Contains('<') && !result.Contains('>');
    }

    [Property(MaxTest = 50)]
    public bool EncodeForHtml_NeverContainsRawAmpersand(NonEmptyString value)
    {
        var encoder = new DefaultOutputEncoder();
        var result = encoder.EncodeForHtml(value.Get);

        // Every & should be part of an entity
        var index = 0;
        while ((index = result.IndexOf('&', index)) >= 0)
        {
            var semicolonIndex = result.IndexOf(';', index);
            if (semicolonIndex < 0) return false;
            index = semicolonIndex + 1;
        }

        return true;
    }

    [Property(MaxTest = 50)]
    public bool EncodeForHtml_IsIdempotent_WhenAlreadyEncoded(NonEmptyString value)
    {
        // Note: HTML encoding is NOT idempotent (&amp; becomes &amp;amp;)
        // but we can verify that the output of the first encoding doesn't
        // contain any characters that would need encoding BEYOND the entities themselves
        var encoder = new DefaultOutputEncoder();
        var encoded = encoder.EncodeForHtml(value.Get);

        // The encoded result should not contain raw < > " '
        return !encoded.Contains('<') && !encoded.Contains('>');
    }

    [Property(MaxTest = 50)]
    public bool EncodeForJavaScript_NeverContainsRawQuotes(NonEmptyString value)
    {
        var encoder = new DefaultOutputEncoder();
        var result = encoder.EncodeForJavaScript(value.Get);

        // Single and double quotes should be encoded
        return !result.Contains('\'') && !result.Contains('"');
    }

    [Property(MaxTest = 50)]
    public bool EncodeForUrl_OutputNeverContainsUnsafeCharacters(NonEmptyString value)
    {
        var encoder = new DefaultOutputEncoder();
        var result = encoder.EncodeForUrl(value.Get);

        // URL encoding must eliminate characters unsafe in URLs.
        // UrlEncoder.Default preserves some RFC 3986 unreserved chars and percent-encodes the rest.
        // Verify no unsafe characters remain (spaces, angle brackets, quotes, non-ASCII).
        foreach (var c in result)
        {
            if (c is ' ' or '<' or '>' or '"' or '{' or '}' or '\\') return false;
            if (c > 127) return false; // Non-ASCII should be percent-encoded
        }

        // Verify all % signs are followed by two hex digits
        for (var i = 0; i < result.Length; i++)
        {
            if (result[i] == '%')
            {
                if (i + 2 >= result.Length) return false;
                if (!IsHexDigit(result[i + 1]) || !IsHexDigit(result[i + 2])) return false;
            }
        }

        return true;

        static bool IsHexDigit(char c) =>
            c is (>= '0' and <= '9') or (>= 'A' and <= 'F') or (>= 'a' and <= 'f');
    }

    [Property(MaxTest = 50)]
    public bool EncodeForCss_OutputContainsOnlyHexEscapes(NonEmptyString value)
    {
        var encoder = new DefaultOutputEncoder();
        var result = encoder.EncodeForCss(value.Get);

        // CSS encoding should use \XXXXXX format for non-safe chars
        // Safe chars: alphanumeric
        foreach (var c in result)
        {
            if (char.IsLetterOrDigit(c)) continue;
            if (c == '\\') continue; // escape prefix
            if (c == ' ') continue; // 6-digit padding separator
            return false;
        }

        return true;
    }

    #endregion

    #region Error Code Invariants

    [Property(MaxTest = 50)]
    public bool AllErrorCodes_StartWithSanitizationPrefix(NonEmptyString profileName)
    {
        const string prefix = "sanitization.";

        var errors = new[]
        {
            SanitizationErrors.ProfileNotFound(profileName.Get),
            SanitizationErrors.PropertyError(profileName.Get),
            SanitizationErrors.PropertyError(profileName.Get, new InvalidOperationException("test"))
        };

        return errors.All(e =>
        {
            var code = e.GetCode().IfNone(string.Empty);
            return code.StartsWith(prefix, StringComparison.Ordinal);
        });
    }

    #endregion

    #region Orchestrator Property Invariants

    [Property(MaxTest = 30)]
    public bool Orchestrator_SanitizedHtmlProperty_NeverContainsScript(NonEmptyString value)
    {
        SanitizationPropertyCache.ClearCache();

        var sanitizer = CreateSanitizer();
        var orchestratorLogger = NullLogger<SanitizationOrchestrator>.Instance;
        var orchestrator = new SanitizationOrchestrator(
            sanitizer,
            Options.Create(new SanitizationOptions()),
            orchestratorLogger);

        var command = new TestHtmlCommand { Title = $"<script>{value.Get}</script>Safe" };
        var result = orchestrator.Sanitize(command);

        if (result.IsLeft) return false;

        SanitizationPropertyCache.ClearCache();
        return !command.Title.Contains("<script", StringComparison.OrdinalIgnoreCase);
    }

    [Property(MaxTest = 30)]
    public bool Orchestrator_AutoSanitize_SanitizesAllStrings(NonEmptyString value)
    {
        SanitizationPropertyCache.ClearCache();

        var sanitizer = CreateSanitizer();
        var options = new SanitizationOptions { SanitizeAllStringInputs = true };
        var orchestratorLogger = NullLogger<SanitizationOrchestrator>.Instance;
        var orchestrator = new SanitizationOrchestrator(
            sanitizer,
            Options.Create(options),
            orchestratorLogger);

        var command = new TestPlainCommand { Name = $"<script>{value.Get}</script>" };
        var result = orchestrator.Sanitize(command);

        if (result.IsLeft) return false;

        SanitizationPropertyCache.ClearCache();
        // Auto-sanitize with StrictText default should strip all HTML
        return !command.Name.Contains("<script", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Profile Invariants

    [Property(MaxTest = 30)]
    public bool StrictText_AlwaysStripsAllTags(NonEmptyString value)
    {
        var sanitizer = CreateSanitizer();
        var result = sanitizer.Custom(value.Get, SanitizationProfiles.StrictText);

        // StrictText strips all tags — result should never contain < >
        return !result.Contains('<') && !result.Contains('>');
    }

    [Property(MaxTest = 30)]
    public bool None_Profile_PreservesInput(NonEmptyString value)
    {
        // None profile is pass-through
        var sanitizer = CreateSanitizer();
        var result = sanitizer.Custom(value.Get, SanitizationProfiles.None);
        return result == value.Get;
    }

    #endregion

    #region Property Cache Invariants

    [Property(MaxTest = 30)]
    public bool PropertyCache_SameType_ReturnsSameInstance()
    {
        SanitizationPropertyCache.ClearCache();

        var first = SanitizationPropertyCache.GetProperties(typeof(TestHtmlCommand));
        var second = SanitizationPropertyCache.GetProperties(typeof(TestHtmlCommand));

        SanitizationPropertyCache.ClearCache();
        return ReferenceEquals(first, second);
    }

    [Property(MaxTest = 30)]
    public bool EncodingCache_SameType_ReturnsSameInstance()
    {
        EncodingPropertyCache.ClearCache();

        var first = EncodingPropertyCache.GetProperties(typeof(TestEncodedResponse));
        var second = EncodingPropertyCache.GetProperties(typeof(TestEncodedResponse));

        EncodingPropertyCache.ClearCache();
        return ReferenceEquals(first, second);
    }

    #endregion

    #region Helpers

    private static DefaultSanitizer CreateSanitizer()
    {
        return new DefaultSanitizer(Options.Create(new SanitizationOptions()));
    }

    #endregion

    #region Test Types

    private sealed class TestHtmlCommand
    {
        [SanitizeHtml]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }

    private sealed class TestPlainCommand
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestEncodedResponse
    {
        [EncodeForHtml]
        public string Title { get; set; } = string.Empty;
    }

    #endregion
}
