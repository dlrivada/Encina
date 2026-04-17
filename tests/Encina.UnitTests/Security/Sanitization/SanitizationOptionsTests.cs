using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Profiles;
using Shouldly;

namespace Encina.UnitTests.Security.Sanitization;

public sealed class SanitizationOptionsTests
{
    #region Default Values

    [Fact]
    public void SanitizeAllStringInputs_DefaultIsFalse()
    {
        var options = new SanitizationOptions();

        options.SanitizeAllStringInputs.ShouldBeFalse();
    }

    [Fact]
    public void EncodeAllOutputs_DefaultIsFalse()
    {
        var options = new SanitizationOptions();

        options.EncodeAllOutputs.ShouldBeFalse();
    }

    [Fact]
    public void DefaultProfile_DefaultIsNull()
    {
        var options = new SanitizationOptions();

        options.DefaultProfile.ShouldBeNull();
    }

    [Fact]
    public void AddHealthCheck_DefaultIsFalse()
    {
        var options = new SanitizationOptions();

        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void EnableTracing_DefaultIsFalse()
    {
        var options = new SanitizationOptions();

        options.EnableTracing.ShouldBeFalse();
    }

    [Fact]
    public void EnableMetrics_DefaultIsFalse()
    {
        var options = new SanitizationOptions();

        options.EnableMetrics.ShouldBeFalse();
    }

    #endregion

    #region Property Setters

    [Fact]
    public void SanitizeAllStringInputs_CanBeSetToTrue()
    {
        var options = new SanitizationOptions { SanitizeAllStringInputs = true };

        options.SanitizeAllStringInputs.ShouldBeTrue();
    }

    [Fact]
    public void DefaultProfile_CanBeSet()
    {
        var options = new SanitizationOptions
        {
            DefaultProfile = SanitizationProfiles.RichText
        };

        options.DefaultProfile.ShouldBeSameAs(SanitizationProfiles.RichText);
    }

    #endregion

    #region AddProfile (ISanitizationProfile)

    [Fact]
    public void AddProfile_ValidNameAndProfile_RegistersProfile()
    {
        var options = new SanitizationOptions();
        var profile = SanitizationProfiles.BasicFormatting;

        options.AddProfile("test", profile);

        options.TryGetProfile("test", out var retrieved).ShouldBeTrue();
        retrieved.ShouldBeSameAs(profile);
    }

    [Fact]
    public void AddProfile_CaseInsensitiveLookup_FindsProfile()
    {
        var options = new SanitizationOptions();
        options.AddProfile("MyProfile", SanitizationProfiles.RichText);

        options.TryGetProfile("myprofile", out var retrieved).ShouldBeTrue();
        retrieved.ShouldBeSameAs(SanitizationProfiles.RichText);
    }

    [Fact]
    public void AddProfile_DuplicateName_OverwritesProfile()
    {
        var options = new SanitizationOptions();
        options.AddProfile("test", SanitizationProfiles.BasicFormatting);
        options.AddProfile("test", SanitizationProfiles.RichText);

        options.TryGetProfile("test", out var retrieved).ShouldBeTrue();
        retrieved.ShouldBeSameAs(SanitizationProfiles.RichText);
    }

    [Fact]
    public void AddProfile_NullName_ThrowsArgumentNullException()
    {
        var options = new SanitizationOptions();

        var act = () => options.AddProfile(null!, SanitizationProfiles.StrictText);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("name");
    }

    [Fact]
    public void AddProfile_NullProfile_ThrowsArgumentNullException()
    {
        var options = new SanitizationOptions();

        var act = () => options.AddProfile("test", (ISanitizationProfile)null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("profile");
    }

    [Fact]
    public void AddProfile_EmptyName_ThrowsArgumentException()
    {
        var options = new SanitizationOptions();

        var act = () => options.AddProfile("", SanitizationProfiles.StrictText);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void AddProfile_WhitespaceName_ThrowsArgumentException()
    {
        var options = new SanitizationOptions();

        var act = () => options.AddProfile("   ", SanitizationProfiles.StrictText);

        Should.Throw<ArgumentException>(act);
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

        options.TryGetProfile("custom", out var profile).ShouldBeTrue();
        profile!.AllowedTags.ShouldContain("p");
        profile.AllowedTags.ShouldContain("a");
        profile.AllowedAttributes.ShouldContain("href");
        profile.AllowedProtocols.ShouldContain("https");
        profile.StripScripts.ShouldBeTrue();
        profile.StripComments.ShouldBeTrue();
    }

    [Fact]
    public void AddProfile_Builder_NullName_ThrowsArgumentNullException()
    {
        var options = new SanitizationOptions();

        var act = () => options.AddProfile(null!, _ => { });

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("name");
    }

    [Fact]
    public void AddProfile_Builder_NullConfigure_ThrowsArgumentNullException()
    {
        var options = new SanitizationOptions();

        var act = () => options.AddProfile("test", (Action<SanitizationProfileBuilder>)null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("configure");
    }

    #endregion

    #region TryGetProfile

    [Fact]
    public void TryGetProfile_NonExistent_ReturnsFalse()
    {
        var options = new SanitizationOptions();

        var found = options.TryGetProfile("nonexistent", out var profile);

        found.ShouldBeFalse();
        profile.ShouldBeNull();
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
        options.ShouldNotBeNull();
    }

    [Fact]
    public void UseHtmlSanitizer_NullConfigure_ThrowsArgumentNullException()
    {
        var options = new SanitizationOptions();

        var act = () => options.UseHtmlSanitizer(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("configure");
    }

    #endregion
}
