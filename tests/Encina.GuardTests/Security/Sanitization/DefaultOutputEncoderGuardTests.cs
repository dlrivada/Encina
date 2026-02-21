using Encina.Security.Sanitization.Encoders;
using FluentAssertions;

namespace Encina.GuardTests.Security.Sanitization;

/// <summary>
/// Guard tests for <see cref="DefaultOutputEncoder"/> to verify null parameter handling.
/// </summary>
public sealed class DefaultOutputEncoderGuardTests
{
    private readonly DefaultOutputEncoder _sut = new();

    [Fact]
    public void EncodeForHtml_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.EncodeForHtml(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    [Fact]
    public void EncodeForHtmlAttribute_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.EncodeForHtmlAttribute(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    [Fact]
    public void EncodeForJavaScript_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.EncodeForJavaScript(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    [Fact]
    public void EncodeForUrl_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.EncodeForUrl(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    [Fact]
    public void EncodeForCss_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.EncodeForCss(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }
}
