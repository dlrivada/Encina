using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Profiles;
using Shouldly;
using Microsoft.Extensions.Options;

namespace Encina.GuardTests.Security.Sanitization;

/// <summary>
/// Guard tests for <see cref="DefaultSanitizer"/> to verify null parameter handling.
/// </summary>
public sealed class DefaultSanitizerGuardTests
{
    private readonly DefaultSanitizer _sut;

    public DefaultSanitizerGuardTests()
    {
        _sut = new DefaultSanitizer(Options.Create(new SanitizationOptions()));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultSanitizer(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void SanitizeHtml_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.SanitizeHtml(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
    }

    [Fact]
    public void SanitizeForSql_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.SanitizeForSql(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
    }

    [Fact]
    public void SanitizeForShell_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.SanitizeForShell(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
    }

    [Fact]
    public void SanitizeForJson_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.SanitizeForJson(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
    }

    [Fact]
    public void SanitizeForXml_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.SanitizeForXml(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("input");
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
}
