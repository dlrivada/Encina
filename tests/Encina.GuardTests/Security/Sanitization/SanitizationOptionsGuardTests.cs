using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Profiles;
using FluentAssertions;

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

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void AddProfile_NullProfile_ThrowsArgumentNullException()
    {
        var act = () => _sut.AddProfile("test", (ISanitizationProfile)null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("profile");
    }

    [Fact]
    public void AddProfile_EmptyName_ThrowsArgumentException()
    {
        var act = () => _sut.AddProfile("", SanitizationProfiles.StrictText);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddProfile_WhitespaceName_ThrowsArgumentException()
    {
        var act = () => _sut.AddProfile("   ", SanitizationProfiles.StrictText);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddProfile_Builder_NullName_ThrowsArgumentNullException()
    {
        var act = () => _sut.AddProfile(null!, _ => { });

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void AddProfile_Builder_NullConfigure_ThrowsArgumentNullException()
    {
        var act = () => _sut.AddProfile("test", (Action<SanitizationProfileBuilder>)null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }

    [Fact]
    public void UseHtmlSanitizer_NullConfigure_ThrowsArgumentNullException()
    {
        var act = () => _sut.UseHtmlSanitizer(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }
}
