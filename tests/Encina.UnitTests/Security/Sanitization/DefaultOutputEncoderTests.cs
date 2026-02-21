using Encina.Security.Sanitization.Encoders;
using FluentAssertions;

namespace Encina.UnitTests.Security.Sanitization;

public sealed class DefaultOutputEncoderTests
{
    private readonly DefaultOutputEncoder _sut = new();

    #region EncodeForHtml

    [Fact]
    public void EncodeForHtml_AngleBrackets_EncodesEntities()
    {
        var result = _sut.EncodeForHtml("<script>alert(1)</script>");

        result.Should().NotContain("<");
        result.Should().NotContain(">");
        result.Should().Contain("&lt;");
        result.Should().Contain("&gt;");
    }

    [Fact]
    public void EncodeForHtml_Ampersand_EncodesEntity()
    {
        var result = _sut.EncodeForHtml("Tom & Jerry");

        result.Should().Contain("&amp;");
    }

    [Fact]
    public void EncodeForHtml_DoubleQuote_EncodesEntity()
    {
        var result = _sut.EncodeForHtml("value with \"quotes\"");

        result.Should().Contain("&quot;");
    }

    [Fact]
    public void EncodeForHtml_SingleQuote_EncodesEntity()
    {
        var result = _sut.EncodeForHtml("it's");

        result.Should().Contain("&#x27;");
    }

    [Fact]
    public void EncodeForHtml_PlainText_ReturnsUnchanged()
    {
        var result = _sut.EncodeForHtml("Hello World");

        result.Should().Be("Hello World");
    }

    [Fact]
    public void EncodeForHtml_EmptyString_ReturnsEmpty()
    {
        var result = _sut.EncodeForHtml(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void EncodeForHtml_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.EncodeForHtml(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    #endregion

    #region EncodeForHtmlAttribute

    [Fact]
    public void EncodeForHtmlAttribute_DoubleQuote_Encodes()
    {
        var result = _sut.EncodeForHtmlAttribute("value\"break");

        result.Should().Contain("&quot;");
    }

    [Fact]
    public void EncodeForHtmlAttribute_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.EncodeForHtmlAttribute(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    #endregion

    #region EncodeForJavaScript

    [Fact]
    public void EncodeForJavaScript_BackslashAndQuote_Encodes()
    {
        var result = _sut.EncodeForJavaScript("value\"with\\slash");

        result.Should().NotBe("value\"with\\slash");
    }

    [Fact]
    public void EncodeForJavaScript_AngleBrackets_Encodes()
    {
        var result = _sut.EncodeForJavaScript("<script>");

        result.Should().NotContain("<");
        result.Should().NotContain(">");
    }

    [Fact]
    public void EncodeForJavaScript_PlainText_ReturnsUnchanged()
    {
        var result = _sut.EncodeForJavaScript("Hello World");

        result.Should().Be("Hello World");
    }

    [Fact]
    public void EncodeForJavaScript_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.EncodeForJavaScript(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    #endregion

    #region EncodeForUrl

    [Fact]
    public void EncodeForUrl_SpaceAndSpecialChars_Encodes()
    {
        var result = _sut.EncodeForUrl("hello world&foo=bar");

        result.Should().NotContain(" ");
        result.Should().NotContain("&");
    }

    [Fact]
    public void EncodeForUrl_AlphanumericText_ReturnsUnchanged()
    {
        var result = _sut.EncodeForUrl("helloworld123");

        result.Should().Be("helloworld123");
    }

    [Fact]
    public void EncodeForUrl_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.EncodeForUrl(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    #endregion

    #region EncodeForCss

    [Fact]
    public void EncodeForCss_SpecialCharacters_EscapesAsHex()
    {
        var result = _sut.EncodeForCss("expression(alert(1))");

        // Non-alphanumeric characters should be hex-escaped
        result.Should().Contain("\\");
    }

    [Fact]
    public void EncodeForCss_AlphanumericText_ReturnsUnchanged()
    {
        var result = _sut.EncodeForCss("color123");

        result.Should().Be("color123");
    }

    [Fact]
    public void EncodeForCss_EmptyString_ReturnsEmpty()
    {
        var result = _sut.EncodeForCss(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void EncodeForCss_Semicolon_EscapesAs6DigitHex()
    {
        var result = _sut.EncodeForCss(";");

        // ';' is 0x3B → \00003B
        result.Should().Be("\\00003B");
    }

    [Fact]
    public void EncodeForCss_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.EncodeForCss(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    #endregion
}
