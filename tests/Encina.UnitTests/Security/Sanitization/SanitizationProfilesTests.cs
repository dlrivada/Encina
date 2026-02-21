using Encina.Security.Sanitization.Profiles;
using FluentAssertions;

namespace Encina.UnitTests.Security.Sanitization;

public sealed class SanitizationProfilesTests
{
    #region None Profile

    [Fact]
    public void None_HasNoAllowedTags()
    {
        SanitizationProfiles.None.AllowedTags.Should().BeEmpty();
    }

    [Fact]
    public void None_HasNoAllowedAttributes()
    {
        SanitizationProfiles.None.AllowedAttributes.Should().BeEmpty();
    }

    [Fact]
    public void None_HasNoAllowedProtocols()
    {
        SanitizationProfiles.None.AllowedProtocols.Should().BeEmpty();
    }

    [Fact]
    public void None_DoesNotStripComments()
    {
        SanitizationProfiles.None.StripComments.Should().BeFalse();
    }

    [Fact]
    public void None_DoesNotStripScripts()
    {
        SanitizationProfiles.None.StripScripts.Should().BeFalse();
    }

    #endregion

    #region StrictText Profile

    [Fact]
    public void StrictText_HasNoAllowedTags()
    {
        SanitizationProfiles.StrictText.AllowedTags.Should().BeEmpty();
    }

    [Fact]
    public void StrictText_StripsComments()
    {
        SanitizationProfiles.StrictText.StripComments.Should().BeTrue();
    }

    [Fact]
    public void StrictText_StripsScripts()
    {
        SanitizationProfiles.StrictText.StripScripts.Should().BeTrue();
    }

    #endregion

    #region BasicFormatting Profile

    [Fact]
    public void BasicFormatting_AllowsBasicTags()
    {
        var tags = SanitizationProfiles.BasicFormatting.AllowedTags;

        tags.Should().Contain("b");
        tags.Should().Contain("i");
        tags.Should().Contain("u");
        tags.Should().Contain("em");
        tags.Should().Contain("strong");
        tags.Should().Contain("br");
        tags.Should().Contain("p");
        tags.Should().Contain("span");
    }

    [Fact]
    public void BasicFormatting_HasNoAllowedAttributes()
    {
        SanitizationProfiles.BasicFormatting.AllowedAttributes.Should().BeEmpty();
    }

    [Fact]
    public void BasicFormatting_StripsScripts()
    {
        SanitizationProfiles.BasicFormatting.StripScripts.Should().BeTrue();
    }

    #endregion

    #region RichText Profile

    [Fact]
    public void RichText_AllowsHeadingTags()
    {
        var tags = SanitizationProfiles.RichText.AllowedTags;

        tags.Should().Contain("h1");
        tags.Should().Contain("h2");
        tags.Should().Contain("h3");
        tags.Should().Contain("h4");
        tags.Should().Contain("h5");
        tags.Should().Contain("h6");
    }

    [Fact]
    public void RichText_AllowsListTags()
    {
        var tags = SanitizationProfiles.RichText.AllowedTags;

        tags.Should().Contain("ul");
        tags.Should().Contain("ol");
        tags.Should().Contain("li");
    }

    [Fact]
    public void RichText_AllowsLinkAndImageTags()
    {
        var tags = SanitizationProfiles.RichText.AllowedTags;

        tags.Should().Contain("a");
        tags.Should().Contain("img");
    }

    [Fact]
    public void RichText_AllowsTableTags()
    {
        var tags = SanitizationProfiles.RichText.AllowedTags;

        tags.Should().Contain("table");
        tags.Should().Contain("thead");
        tags.Should().Contain("tbody");
        tags.Should().Contain("tr");
        tags.Should().Contain("th");
        tags.Should().Contain("td");
    }

    [Fact]
    public void RichText_AllowsSafeAttributes()
    {
        var attrs = SanitizationProfiles.RichText.AllowedAttributes;

        attrs.Should().Contain("href");
        attrs.Should().Contain("src");
        attrs.Should().Contain("alt");
        attrs.Should().Contain("title");
        attrs.Should().Contain("class");
    }

    [Fact]
    public void RichText_AllowsOnlyHttpsAndMailto()
    {
        var protocols = SanitizationProfiles.RichText.AllowedProtocols;

        protocols.Should().Contain("https");
        protocols.Should().Contain("mailto");
        protocols.Should().HaveCount(2);
    }

    [Fact]
    public void RichText_StripsScripts()
    {
        SanitizationProfiles.RichText.StripScripts.Should().BeTrue();
    }

    #endregion

    #region Markdown Profile

    [Fact]
    public void Markdown_IncludesAllRichTextTags()
    {
        var richTextTags = SanitizationProfiles.RichText.AllowedTags;
        var markdownTags = SanitizationProfiles.Markdown.AllowedTags;

        foreach (var tag in richTextTags)
        {
            markdownTags.Should().Contain(tag);
        }
    }

    [Fact]
    public void Markdown_AddsDefinitionListTags()
    {
        var tags = SanitizationProfiles.Markdown.AllowedTags;

        tags.Should().Contain("dl");
        tags.Should().Contain("dt");
        tags.Should().Contain("dd");
    }

    [Fact]
    public void Markdown_AddsDetailsSummaryTags()
    {
        var tags = SanitizationProfiles.Markdown.AllowedTags;

        tags.Should().Contain("details");
        tags.Should().Contain("summary");
    }

    [Fact]
    public void Markdown_AllowsIdAttribute()
    {
        SanitizationProfiles.Markdown.AllowedAttributes.Should().Contain("id");
    }

    [Fact]
    public void Markdown_AllowsTargetAttribute()
    {
        SanitizationProfiles.Markdown.AllowedAttributes.Should().Contain("target");
    }

    #endregion

    #region SanitizationProfileBuilder

    [Fact]
    public void Builder_EmptyBuild_ReturnsProfileWithDefaults()
    {
        var profile = new SanitizationProfileBuilder().Build();

        profile.AllowedTags.Should().BeEmpty();
        profile.AllowedAttributes.Should().BeEmpty();
        profile.AllowedProtocols.Should().BeEmpty();
        profile.StripComments.Should().BeTrue();
        profile.StripScripts.Should().BeTrue();
    }

    [Fact]
    public void Builder_AllowTags_AddsTags()
    {
        var profile = new SanitizationProfileBuilder()
            .AllowTags("p", "b", "i")
            .Build();

        profile.AllowedTags.Should().HaveCount(3);
        profile.AllowedTags.Should().Contain("p");
        profile.AllowedTags.Should().Contain("b");
        profile.AllowedTags.Should().Contain("i");
    }

    [Fact]
    public void Builder_AllowAttributes_AddsAttributes()
    {
        var profile = new SanitizationProfileBuilder()
            .AllowAttributes("href", "src")
            .Build();

        profile.AllowedAttributes.Should().HaveCount(2);
    }

    [Fact]
    public void Builder_AllowProtocols_AddsProtocols()
    {
        var profile = new SanitizationProfileBuilder()
            .AllowProtocols("https", "mailto")
            .Build();

        profile.AllowedProtocols.Should().HaveCount(2);
    }

    [Fact]
    public void Builder_WithStripComments_SetsFlag()
    {
        var profile = new SanitizationProfileBuilder()
            .WithStripComments(true)
            .Build();

        profile.StripComments.Should().BeTrue();
    }

    [Fact]
    public void Builder_WithStripScripts_SetsFlag()
    {
        var profile = new SanitizationProfileBuilder()
            .WithStripScripts(true)
            .Build();

        profile.StripScripts.Should().BeTrue();
    }

    [Fact]
    public void Builder_FluentChaining_Works()
    {
        var profile = new SanitizationProfileBuilder()
            .AllowTags("p", "a")
            .AllowAttributes("href")
            .AllowProtocols("https")
            .WithStripComments(true)
            .WithStripScripts(true)
            .Build();

        profile.AllowedTags.Should().HaveCount(2);
        profile.AllowedAttributes.Should().HaveCount(1);
        profile.AllowedProtocols.Should().HaveCount(1);
        profile.StripComments.Should().BeTrue();
        profile.StripScripts.Should().BeTrue();
    }

    #endregion
}
