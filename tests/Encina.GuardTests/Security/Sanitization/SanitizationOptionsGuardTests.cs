using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Profiles;
using Shouldly;

namespace Encina.GuardTests.Security.Sanitization;

/// <summary>
/// Guard tests for <see cref="SanitizationOptions"/> to verify null parameter handling.
/// </summary>
public sealed class SanitizationOptionsGuardTests
{
    private readonly SanitizationOptions _sut = new();

    [Fact]
    public void AddProfile_NullName_ThrowsArgumentNullException()
    {
        var act = () => _sut.AddProfile(null!, SanitizationProfiles.StrictText);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("name");
    }

    [Fact]
    public void AddProfile_NullProfile_ThrowsArgumentNullException()
    {
        var act = () => _sut.AddProfile("test", (ISanitizationProfile)null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("profile");
    }

    [Fact]
    public void AddProfile_EmptyName_ThrowsArgumentException()
    {
        var act = () => _sut.AddProfile("", SanitizationProfiles.StrictText);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void AddProfile_WhitespaceName_ThrowsArgumentException()
    {
        var act = () => _sut.AddProfile("   ", SanitizationProfiles.StrictText);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void AddProfile_Builder_NullName_ThrowsArgumentNullException()
    {
        var act = () => _sut.AddProfile(null!, _ => { });

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("name");
    }

    [Fact]
    public void AddProfile_Builder_NullConfigure_ThrowsArgumentNullException()
    {
        var act = () => _sut.AddProfile("test", (Action<SanitizationProfileBuilder>)null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("configure");
    }

    [Fact]
    public void UseHtmlSanitizer_NullConfigure_ThrowsArgumentNullException()
    {
        var act = () => _sut.UseHtmlSanitizer(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("configure");
    }
}
