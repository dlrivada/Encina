using Encina.Security.Sanitization.Encoders;
using Shouldly;

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

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
    }

    [Fact]
    public void EncodeForHtmlAttribute_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.EncodeForHtmlAttribute(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
    }

    [Fact]
    public void EncodeForJavaScript_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.EncodeForJavaScript(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
    }

    [Fact]
    public void EncodeForUrl_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.EncodeForUrl(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
    }

    [Fact]
    public void EncodeForCss_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.EncodeForCss(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
    }
}
