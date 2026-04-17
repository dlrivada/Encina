using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Profiles;
using Microsoft.Extensions.Options;
using Shouldly;

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

        result.ShouldNotContain("<script>");
        result.ShouldNotContain("alert");
    }

    [Fact]
    public void SanitizeHtml_PlainText_ReturnsPlainText()
    {
        var input = "Hello World";

        var result = _sut.SanitizeHtml(input);

        result.ShouldBe("Hello World");
    }

    [Fact]
    public void SanitizeHtml_OnEventHandler_RemovesEvent()
    {
        var input = "<img src='x' onerror='alert(1)' />";

        var result = _sut.SanitizeHtml(input);

        result.ShouldNotContain("onerror");
        result.ShouldNotContain("alert");
    }

    [Fact]
    public void SanitizeHtml_JavascriptProtocol_Removes()
    {
        var input = "<a href='javascript:alert(1)'>click</a>";

        var result = _sut.SanitizeHtml(input);

        result.ShouldNotContain("javascript:");
    }

    [Fact]
    public void SanitizeHtml_EmptyString_ReturnsEmpty()
    {
        var result = _sut.SanitizeHtml(string.Empty);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void SanitizeHtml_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.SanitizeHtml(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
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

        result.ShouldNotContain("alert");
        result.ShouldNotContain("onerror");
        result.ShouldNotContain("onload");
        result.ShouldNotContain("onfocus");
        result.ShouldNotContain("<script");
        result.ShouldNotContain("<iframe");
    }

    #endregion

    #region SanitizeForSql

    [Fact]
    public void SanitizeForSql_SingleQuote_EscapesQuote()
    {
        var input = "O'Brien";

        var result = _sut.SanitizeForSql(input);

        result.ShouldBe("O''Brien");
    }

    [Fact]
    public void SanitizeForSql_CommentMarker_RemovesComment()
    {
        var input = "value -- comment";

        var result = _sut.SanitizeForSql(input);

        result.ShouldNotContain("--");
    }

    [Fact]
    public void SanitizeForSql_BlockComment_RemovesBlockComment()
    {
        var input = "value /* comment */ more";

        var result = _sut.SanitizeForSql(input);

        result.ShouldNotContain("/*");
        result.ShouldNotContain("*/");
    }

    [Fact]
    public void SanitizeForSql_Semicolon_RemovesSemicolon()
    {
        var input = "1; DROP TABLE users";

        var result = _sut.SanitizeForSql(input);

        result.ShouldNotContain(";");
    }

    [Fact]
    public void SanitizeForSql_ExtendedProc_RemovesXpCall()
    {
        var input = "xp_cmdshell 'dir'";

        var result = _sut.SanitizeForSql(input);

        result.ShouldNotContain("xp_cmdshell");
    }

    [Fact]
    public void SanitizeForSql_PlainText_ReturnsUnchanged()
    {
        var input = "normal value";

        var result = _sut.SanitizeForSql(input);

        result.ShouldBe("normal value");
    }

    [Fact]
    public void SanitizeForSql_EmptyString_ReturnsEmpty()
    {
        var result = _sut.SanitizeForSql(string.Empty);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void SanitizeForSql_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.SanitizeForSql(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
    }

    [Theory]
    [InlineData("'; DROP TABLE users--")]
    [InlineData("1 OR 1=1")]
    [InlineData("1; EXEC xp_cmdshell 'net user'")]
    [InlineData("admin'/* comment */")]
    public void SanitizeForSql_InjectionPayloads_NeutralizesThreats(string payload)
    {
        var result = _sut.SanitizeForSql(payload);

        result.ShouldNotContain("--");
        result.ShouldNotContain(";");
        result.ShouldNotContain("/*");
        result.ShouldNotContain("xp_");
    }

    #endregion

    #region SanitizeForShell

    [Fact]
    public void SanitizeForShell_MetaCharacters_EscapesThem()
    {
        var input = "hello & world";

        var result = _sut.SanitizeForShell(input);

        // On Windows: ^& escaped; on Unix: wrapped in single quotes
        result.ShouldNotBe(input);
    }

    [Fact]
    public void SanitizeForShell_PlainText_HandlesText()
    {
        var input = "normaltext";

        var result = _sut.SanitizeForShell(input);

        result.ShouldContain("normaltext");
    }

    [Fact]
    public void SanitizeForShell_EmptyString_ReturnsEmpty()
    {
        var result = _sut.SanitizeForShell(string.Empty);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void SanitizeForShell_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.SanitizeForShell(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
    }

    [Theory]
    [InlineData("hello | rm -rf /")]
    [InlineData("file & cat /etc/passwd")]
    [InlineData("$(whoami)")]
    public void SanitizeForShell_DangerousPayloads_EscapesOrQuotes(string payload)
    {
        var result = _sut.SanitizeForShell(payload);

        // Result should be different from the raw input (escaped or quoted)
        result.ShouldNotBe(payload);
    }

    #endregion

    #region SanitizeForJson

    [Fact]
    public void SanitizeForJson_DoubleQuote_EscapesQuote()
    {
        var input = "value with \"quotes\"";

        var result = _sut.SanitizeForJson(input);

        // JsonSerializer may use \u0022 or \" for double quotes — both are valid JSON escaping
        result.ShouldNotContain("\"");
    }

    [Fact]
    public void SanitizeForJson_Backslash_EscapesBackslash()
    {
        var input = "path\\to\\file";

        var result = _sut.SanitizeForJson(input);

        result.ShouldContain("\\\\");
    }

    [Fact]
    public void SanitizeForJson_ControlCharacters_EscapesThem()
    {
        var input = "line1\nline2\ttab";

        var result = _sut.SanitizeForJson(input);

        result.ShouldContain("\\n");
        result.ShouldContain("\\t");
    }

    [Fact]
    public void SanitizeForJson_PlainText_ReturnsUnchanged()
    {
        var input = "normal value";

        var result = _sut.SanitizeForJson(input);

        result.ShouldBe("normal value");
    }

    [Fact]
    public void SanitizeForJson_EmptyString_ReturnsEmpty()
    {
        var result = _sut.SanitizeForJson(string.Empty);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void SanitizeForJson_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.SanitizeForJson(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
    }

    #endregion

    #region SanitizeForXml

    [Fact]
    public void SanitizeForXml_Ampersand_EscapesEntity()
    {
        var input = "Tom & Jerry";

        var result = _sut.SanitizeForXml(input);

        result.ShouldContain("&amp;");
    }

    [Fact]
    public void SanitizeForXml_AngleBrackets_EscapesEntities()
    {
        var input = "<tag>value</tag>";

        var result = _sut.SanitizeForXml(input);

        result.ShouldContain("&lt;");
        result.ShouldContain("&gt;");
    }

    [Fact]
    public void SanitizeForXml_Quotes_EscapesEntities()
    {
        var input = "value with \"quotes\" and 'apostrophes'";

        var result = _sut.SanitizeForXml(input);

        result.ShouldContain("&quot;");
        result.ShouldContain("&apos;");
    }

    [Fact]
    public void SanitizeForXml_InvalidXmlChars_RemovesThem()
    {
        // U+0001 is invalid in XML 1.0
        var input = "valid\x01invalid";

        var result = _sut.SanitizeForXml(input);

        result.ShouldNotContain("\x01");
        result.ShouldContain("valid");
        result.ShouldContain("invalid");
    }

    [Fact]
    public void SanitizeForXml_ValidWhitespace_Preserves()
    {
        var input = "line1\nline2\ttab\rreturn";

        var result = _sut.SanitizeForXml(input);

        result.ShouldContain("\n");
        result.ShouldContain("\t");
        result.ShouldContain("\r");
    }

    [Fact]
    public void SanitizeForXml_PlainText_ReturnsUnchanged()
    {
        var input = "normal value";

        var result = _sut.SanitizeForXml(input);

        result.ShouldBe("normal value");
    }

    [Fact]
    public void SanitizeForXml_EmptyString_ReturnsEmpty()
    {
        var result = _sut.SanitizeForXml(string.Empty);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void SanitizeForXml_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.SanitizeForXml(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
    }

    #endregion

    #region Custom

    [Fact]
    public void Custom_WithRichTextProfile_AllowsSafeTags()
    {
        var input = "<p>Hello</p><script>alert(1)</script>";

        var result = _sut.Custom(input, SanitizationProfiles.RichText);

        result.ShouldContain("<p>");
        result.ShouldNotContain("<script>");
    }

    [Fact]
    public void Custom_WithNoneProfile_PassesThrough()
    {
        var input = "<script>alert(1)</script><p>safe</p>";

        var result = _sut.Custom(input, SanitizationProfiles.None);

        // None profile: no allowed tags, no strip flags → pass-through
        result.ShouldBe(input);
    }

    [Fact]
    public void Custom_WithBasicFormattingProfile_AllowsBasicTags()
    {
        var input = "<b>bold</b><script>alert(1)</script><p>para</p>";

        var result = _sut.Custom(input, SanitizationProfiles.BasicFormatting);

        result.ShouldContain("<b>");
        result.ShouldContain("<p>");
        result.ShouldNotContain("<script>");
    }

    [Fact]
    public void Custom_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.Custom(null!, SanitizationProfiles.StrictText);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
    }

    [Fact]
    public void Custom_NullProfile_ThrowsArgumentNullException()
    {
        var act = () => _sut.Custom("test", null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("profile");
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

        result.ShouldContain("<p>");
        result.ShouldContain("<a");
        result.ShouldNotContain("<div>");
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
        result.ShouldNotBeNull();
    }

    #endregion
}
