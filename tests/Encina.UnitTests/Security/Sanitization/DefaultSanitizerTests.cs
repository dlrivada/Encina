using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Profiles;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Security.Sanitization;

public sealed class DefaultSanitizerTests
{
    private readonly DefaultSanitizer _sut;

    public DefaultSanitizerTests()
    {
        _sut = new DefaultSanitizer(Options.Create(new SanitizationOptions()));
    }

    #region SanitizeHtml

    [Fact]
    public void SanitizeHtml_ScriptTag_RemovesScript()
    {
        var input = "<script>alert('xss')</script><p>safe</p>";

        var result = _sut.SanitizeHtml(input);

        result.Should().NotContain("<script>");
        result.Should().NotContain("alert");
    }

    [Fact]
    public void SanitizeHtml_PlainText_ReturnsPlainText()
    {
        var input = "Hello World";

        var result = _sut.SanitizeHtml(input);

        result.Should().Be("Hello World");
    }

    [Fact]
    public void SanitizeHtml_OnEventHandler_RemovesEvent()
    {
        var input = "<img src='x' onerror='alert(1)' />";

        var result = _sut.SanitizeHtml(input);

        result.Should().NotContain("onerror");
        result.Should().NotContain("alert");
    }

    [Fact]
    public void SanitizeHtml_JavascriptProtocol_Removes()
    {
        var input = "<a href='javascript:alert(1)'>click</a>";

        var result = _sut.SanitizeHtml(input);

        result.Should().NotContain("javascript:");
    }

    [Fact]
    public void SanitizeHtml_EmptyString_ReturnsEmpty()
    {
        var result = _sut.SanitizeHtml(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizeHtml_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.SanitizeHtml(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    [Theory]
    [InlineData("<script>document.cookie</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("<svg onload=alert(1)>")]
    [InlineData("<body onload=alert(1)>")]
    [InlineData("<iframe src='javascript:alert(1)'></iframe>")]
    [InlineData("<input onfocus=alert(1) autofocus>")]
    public void SanitizeHtml_XssPayloads_RemovesDangerousContent(string payload)
    {
        var result = _sut.SanitizeHtml(payload);

        result.Should().NotContain("alert");
        result.Should().NotContain("onerror");
        result.Should().NotContain("onload");
        result.Should().NotContain("onfocus");
        result.Should().NotContain("<script");
        result.Should().NotContain("<iframe");
    }

    #endregion

    #region SanitizeForSql

    [Fact]
    public void SanitizeForSql_SingleQuote_EscapesQuote()
    {
        var input = "O'Brien";

        var result = _sut.SanitizeForSql(input);

        result.Should().Be("O''Brien");
    }

    [Fact]
    public void SanitizeForSql_CommentMarker_RemovesComment()
    {
        var input = "value -- comment";

        var result = _sut.SanitizeForSql(input);

        result.Should().NotContain("--");
    }

    [Fact]
    public void SanitizeForSql_BlockComment_RemovesBlockComment()
    {
        var input = "value /* comment */ more";

        var result = _sut.SanitizeForSql(input);

        result.Should().NotContain("/*");
        result.Should().NotContain("*/");
    }

    [Fact]
    public void SanitizeForSql_Semicolon_RemovesSemicolon()
    {
        var input = "1; DROP TABLE users";

        var result = _sut.SanitizeForSql(input);

        result.Should().NotContain(";");
    }

    [Fact]
    public void SanitizeForSql_ExtendedProc_RemovesXpCall()
    {
        var input = "xp_cmdshell 'dir'";

        var result = _sut.SanitizeForSql(input);

        result.Should().NotContain("xp_cmdshell");
    }

    [Fact]
    public void SanitizeForSql_PlainText_ReturnsUnchanged()
    {
        var input = "normal value";

        var result = _sut.SanitizeForSql(input);

        result.Should().Be("normal value");
    }

    [Fact]
    public void SanitizeForSql_EmptyString_ReturnsEmpty()
    {
        var result = _sut.SanitizeForSql(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizeForSql_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.SanitizeForSql(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    [Theory]
    [InlineData("'; DROP TABLE users--")]
    [InlineData("1 OR 1=1")]
    [InlineData("1; EXEC xp_cmdshell 'net user'")]
    [InlineData("admin'/* comment */")]
    public void SanitizeForSql_InjectionPayloads_NeutralizesThreats(string payload)
    {
        var result = _sut.SanitizeForSql(payload);

        result.Should().NotContain("--");
        result.Should().NotContain(";");
        result.Should().NotContain("/*");
        result.Should().NotContain("xp_");
    }

    #endregion

    #region SanitizeForShell

    [Fact]
    public void SanitizeForShell_MetaCharacters_EscapesThem()
    {
        var input = "hello & world";

        var result = _sut.SanitizeForShell(input);

        // On Windows: ^& escaped; on Unix: wrapped in single quotes
        result.Should().NotBe(input);
    }

    [Fact]
    public void SanitizeForShell_PlainText_HandlesText()
    {
        var input = "normaltext";

        var result = _sut.SanitizeForShell(input);

        result.Should().Contain("normaltext");
    }

    [Fact]
    public void SanitizeForShell_EmptyString_ReturnsEmpty()
    {
        var result = _sut.SanitizeForShell(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizeForShell_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.SanitizeForShell(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    [Theory]
    [InlineData("hello | rm -rf /")]
    [InlineData("file & cat /etc/passwd")]
    [InlineData("$(whoami)")]
    public void SanitizeForShell_DangerousPayloads_EscapesOrQuotes(string payload)
    {
        var result = _sut.SanitizeForShell(payload);

        // Result should be different from the raw input (escaped or quoted)
        result.Should().NotBe(payload);
    }

    #endregion

    #region SanitizeForJson

    [Fact]
    public void SanitizeForJson_DoubleQuote_EscapesQuote()
    {
        var input = "value with \"quotes\"";

        var result = _sut.SanitizeForJson(input);

        // JsonSerializer may use \u0022 or \" for double quotes — both are valid JSON escaping
        result.Should().NotContain("\"");
    }

    [Fact]
    public void SanitizeForJson_Backslash_EscapesBackslash()
    {
        var input = "path\\to\\file";

        var result = _sut.SanitizeForJson(input);

        result.Should().Contain("\\\\");
    }

    [Fact]
    public void SanitizeForJson_ControlCharacters_EscapesThem()
    {
        var input = "line1\nline2\ttab";

        var result = _sut.SanitizeForJson(input);

        result.Should().Contain("\\n");
        result.Should().Contain("\\t");
    }

    [Fact]
    public void SanitizeForJson_PlainText_ReturnsUnchanged()
    {
        var input = "normal value";

        var result = _sut.SanitizeForJson(input);

        result.Should().Be("normal value");
    }

    [Fact]
    public void SanitizeForJson_EmptyString_ReturnsEmpty()
    {
        var result = _sut.SanitizeForJson(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizeForJson_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.SanitizeForJson(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    #endregion

    #region SanitizeForXml

    [Fact]
    public void SanitizeForXml_Ampersand_EscapesEntity()
    {
        var input = "Tom & Jerry";

        var result = _sut.SanitizeForXml(input);

        result.Should().Contain("&amp;");
    }

    [Fact]
    public void SanitizeForXml_AngleBrackets_EscapesEntities()
    {
        var input = "<tag>value</tag>";

        var result = _sut.SanitizeForXml(input);

        result.Should().Contain("&lt;");
        result.Should().Contain("&gt;");
    }

    [Fact]
    public void SanitizeForXml_Quotes_EscapesEntities()
    {
        var input = "value with \"quotes\" and 'apostrophes'";

        var result = _sut.SanitizeForXml(input);

        result.Should().Contain("&quot;");
        result.Should().Contain("&apos;");
    }

    [Fact]
    public void SanitizeForXml_InvalidXmlChars_RemovesThem()
    {
        // U+0001 is invalid in XML 1.0
        var input = "valid\x01invalid";

        var result = _sut.SanitizeForXml(input);

        result.Should().NotContain("\x01");
        result.Should().Contain("valid");
        result.Should().Contain("invalid");
    }

    [Fact]
    public void SanitizeForXml_ValidWhitespace_Preserves()
    {
        var input = "line1\nline2\ttab\rreturn";

        var result = _sut.SanitizeForXml(input);

        result.Should().Contain("\n");
        result.Should().Contain("\t");
        result.Should().Contain("\r");
    }

    [Fact]
    public void SanitizeForXml_PlainText_ReturnsUnchanged()
    {
        var input = "normal value";

        var result = _sut.SanitizeForXml(input);

        result.Should().Be("normal value");
    }

    [Fact]
    public void SanitizeForXml_EmptyString_ReturnsEmpty()
    {
        var result = _sut.SanitizeForXml(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizeForXml_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.SanitizeForXml(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    #endregion

    #region Custom

    [Fact]
    public void Custom_WithRichTextProfile_AllowsSafeTags()
    {
        var input = "<p>Hello</p><script>alert(1)</script>";

        var result = _sut.Custom(input, SanitizationProfiles.RichText);

        result.Should().Contain("<p>");
        result.Should().NotContain("<script>");
    }

    [Fact]
    public void Custom_WithNoneProfile_PassesThrough()
    {
        var input = "<script>alert(1)</script><p>safe</p>";

        var result = _sut.Custom(input, SanitizationProfiles.None);

        // None profile: no allowed tags, no strip flags → pass-through
        result.Should().Be(input);
    }

    [Fact]
    public void Custom_WithBasicFormattingProfile_AllowsBasicTags()
    {
        var input = "<b>bold</b><script>alert(1)</script><p>para</p>";

        var result = _sut.Custom(input, SanitizationProfiles.BasicFormatting);

        result.Should().Contain("<b>");
        result.Should().Contain("<p>");
        result.Should().NotContain("<script>");
    }

    [Fact]
    public void Custom_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.Custom(null!, SanitizationProfiles.StrictText);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    [Fact]
    public void Custom_NullProfile_ThrowsArgumentNullException()
    {
        var act = () => _sut.Custom("test", null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("profile");
    }

    [Fact]
    public void Custom_WithCustomProfile_UsesProfileSettings()
    {
        var profile = new SanitizationProfileBuilder()
            .AllowTags("p", "a")
            .AllowAttributes("href")
            .AllowProtocols("https")
            .WithStripScripts(true)
            .Build();

        var input = "<p><a href='https://safe.com'>link</a></p><div>removed</div>";

        var result = _sut.Custom(input, profile);

        result.Should().Contain("<p>");
        result.Should().Contain("<a");
        result.Should().NotContain("<div>");
    }

    #endregion

    #region Custom HtmlSanitizer Configuration

    [Fact]
    public void Constructor_WithHtmlSanitizerConfigurator_AppliesCustomization()
    {
        var options = new SanitizationOptions();
        options.UseHtmlSanitizer(sanitizer =>
        {
            sanitizer.AllowedCssProperties.Add("color");
        });

        var sanitizer = new DefaultSanitizer(Options.Create(options));

        // Should not throw and should work
        var result = sanitizer.SanitizeHtml("<p>test</p>");
        result.Should().NotBeNull();
    }

    #endregion
}
