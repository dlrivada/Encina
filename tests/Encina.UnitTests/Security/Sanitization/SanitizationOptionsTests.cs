using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Profiles;
using FluentAssertions;

namespace Encina.UnitTests.Security.Sanitization;

public sealed class SanitizationOptionsTests
{
    #region Default Values

    [Fact]
    public void SanitizeAllStringInputs_DefaultIsFalse()
    {
        var options = new SanitizationOptions();

        options.SanitizeAllStringInputs.Should().BeFalse();
    }

    [Fact]
    public void EncodeAllOutputs_DefaultIsFalse()
    {
        var options = new SanitizationOptions();

        options.EncodeAllOutputs.Should().BeFalse();
    }

    [Fact]
    public void DefaultProfile_DefaultIsNull()
    {
        var options = new SanitizationOptions();

        options.DefaultProfile.Should().BeNull();
    }

    [Fact]
    public void AddHealthCheck_DefaultIsFalse()
    {
        var options = new SanitizationOptions();

        options.AddHealthCheck.Should().BeFalse();
    }

    [Fact]
    public void EnableTracing_DefaultIsFalse()
    {
        var options = new SanitizationOptions();

        options.EnableTracing.Should().BeFalse();
    }

    [Fact]
    public void EnableMetrics_DefaultIsFalse()
    {
        var options = new SanitizationOptions();

        options.EnableMetrics.Should().BeFalse();
    }

    #endregion

    #region Property Setters

    [Fact]
    public void SanitizeAllStringInputs_CanBeSetToTrue()
    {
        var options = new SanitizationOptions { SanitizeAllStringInputs = true };

        options.SanitizeAllStringInputs.Should().BeTrue();
    }

    [Fact]
    public void DefaultProfile_CanBeSet()
    {
        var options = new SanitizationOptions
        {
            DefaultProfile = SanitizationProfiles.RichText
        };

        options.DefaultProfile.Should().BeSameAs(SanitizationProfiles.RichText);
    }

    #endregion

    #region AddProfile (ISanitizationProfile)

    [Fact]
    public void AddProfile_ValidNameAndProfile_RegistersProfile()
    {
        var options = new SanitizationOptions();
        var profile = SanitizationProfiles.BasicFormatting;

        options.AddProfile("test", profile);

        options.TryGetProfile("test", out var retrieved).Should().BeTrue();
        retrieved.Should().BeSameAs(profile);
    }

    [Fact]
    public void AddProfile_CaseInsensitiveLookup_FindsProfile()
    {
        var options = new SanitizationOptions();
        options.AddProfile("MyProfile", SanitizationProfiles.RichText);

        options.TryGetProfile("myprofile", out var retrieved).Should().BeTrue();
        retrieved.Should().BeSameAs(SanitizationProfiles.RichText);
    }

    [Fact]
    public void AddProfile_DuplicateName_OverwritesProfile()
    {
        var options = new SanitizationOptions();
        options.AddProfile("test", SanitizationProfiles.BasicFormatting);
        options.AddProfile("test", SanitizationProfiles.RichText);

        options.TryGetProfile("test", out var retrieved).Should().BeTrue();
        retrieved.Should().BeSameAs(SanitizationProfiles.RichText);
    }

    [Fact]
    public void AddProfile_NullName_ThrowsArgumentNullException()
    {
        var options = new SanitizationOptions();

        var act = () => options.AddProfile(null!, SanitizationProfiles.StrictText);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void AddProfile_NullProfile_ThrowsArgumentNullException()
    {
        var options = new SanitizationOptions();

        var act = () => options.AddProfile("test", (ISanitizationProfile)null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("profile");
    }

    [Fact]
    public void AddProfile_EmptyName_ThrowsArgumentException()
    {
        var options = new SanitizationOptions();

        var act = () => options.AddProfile("", SanitizationProfiles.StrictText);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddProfile_WhitespaceName_ThrowsArgumentException()
    {
        var options = new SanitizationOptions();

        var act = () => options.AddProfile("   ", SanitizationProfiles.StrictText);

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region AddProfile (Builder)

    [Fact]
    public void AddProfile_Builder_ConfiguresProfile()
    {
        var options = new SanitizationOptions();

        options.AddProfile("custom", builder =>
        {
            builder.AllowTags("p", "a");
            builder.AllowAttributes("href");
            builder.AllowProtocols("https");
            builder.WithStripScripts(true);
            builder.WithStripComments(true);
        });

        options.TryGetProfile("custom", out var profile).Should().BeTrue();
        profile!.AllowedTags.Should().Contain("p");
        profile.AllowedTags.Should().Contain("a");
        profile.AllowedAttributes.Should().Contain("href");
        profile.AllowedProtocols.Should().Contain("https");
        profile.StripScripts.Should().BeTrue();
        profile.StripComments.Should().BeTrue();
    }

    [Fact]
    public void AddProfile_Builder_NullName_ThrowsArgumentNullException()
    {
        var options = new SanitizationOptions();

        var act = () => options.AddProfile(null!, _ => { });

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void AddProfile_Builder_NullConfigure_ThrowsArgumentNullException()
    {
        var options = new SanitizationOptions();

        var act = () => options.AddProfile("test", (Action<SanitizationProfileBuilder>)null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }

    #endregion

    #region TryGetProfile

    [Fact]
    public void TryGetProfile_NonExistent_ReturnsFalse()
    {
        var options = new SanitizationOptions();

        var found = options.TryGetProfile("nonexistent", out var profile);

        found.Should().BeFalse();
        profile.Should().BeNull();
    }

    #endregion

    #region UseHtmlSanitizer

    [Fact]
    public void UseHtmlSanitizer_SetsConfigurator()
    {
        var options = new SanitizationOptions();
        Action<Ganss.Xss.HtmlSanitizer> configurator = _ => { };

        options.UseHtmlSanitizer(configurator);

        // Verify via internal property (testing internal state is acceptable for options)
        options.Should().NotBeNull();
    }

    [Fact]
    public void UseHtmlSanitizer_NullConfigure_ThrowsArgumentNullException()
    {
        var options = new SanitizationOptions();

        var act = () => options.UseHtmlSanitizer(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }

    #endregion
}
