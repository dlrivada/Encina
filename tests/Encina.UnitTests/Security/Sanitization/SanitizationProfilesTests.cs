using Encina.Security.Sanitization.Profiles;
using Shouldly;

namespace Encina.UnitTests.Security.Sanitization;

public sealed class SanitizationProfilesTests
{
    #region None Profile

    [Fact]
    public void None_HasNoAllowedTags()
    {
        SanitizationProfiles.None.AllowedTags.ShouldBeEmpty();
    }

    [Fact]
    public void None_HasNoAllowedAttributes()
    {
        SanitizationProfiles.None.AllowedAttributes.ShouldBeEmpty();
    }

    [Fact]
    public void None_HasNoAllowedProtocols()
    {
        SanitizationProfiles.None.AllowedProtocols.ShouldBeEmpty();
    }

    [Fact]
    public void None_DoesNotStripComments()
    {
        SanitizationProfiles.None.StripComments.ShouldBeFalse();
    }

    [Fact]
    public void None_DoesNotStripScripts()
    {
        SanitizationProfiles.None.StripScripts.ShouldBeFalse();
    }

    #endregion

    #region StrictText Profile

    [Fact]
    public void StrictText_HasNoAllowedTags()
    {
        SanitizationProfiles.StrictText.AllowedTags.ShouldBeEmpty();
    }

    [Fact]
    public void StrictText_StripsComments()
    {
        SanitizationProfiles.StrictText.StripComments.ShouldBeTrue();
    }

    [Fact]
    public void StrictText_StripsScripts()
    {
        SanitizationProfiles.StrictText.StripScripts.ShouldBeTrue();
    }

    #endregion

    #region BasicFormatting Profile

    [Fact]
    public void BasicFormatting_AllowsBasicTags()
    {
        var tags = SanitizationProfiles.BasicFormatting.AllowedTags;

        tags.ShouldContain("b");
        tags.ShouldContain("i");
        tags.ShouldContain("u");
        tags.ShouldContain("em");
        tags.ShouldContain("strong");
        tags.ShouldContain("br");
        tags.ShouldContain("p");
        tags.ShouldContain("span");
    }

    [Fact]
    public void BasicFormatting_HasNoAllowedAttributes()
    {
        SanitizationProfiles.BasicFormatting.AllowedAttributes.ShouldBeEmpty();
    }

    [Fact]
    public void BasicFormatting_StripsScripts()
    {
        SanitizationProfiles.BasicFormatting.StripScripts.ShouldBeTrue();
    }

    #endregion

    #region RichText Profile

    [Fact]
    public void RichText_AllowsHeadingTags()
    {
        var tags = SanitizationProfiles.RichText.AllowedTags;

        tags.ShouldContain("h1");
        tags.ShouldContain("h2");
        tags.ShouldContain("h3");
        tags.ShouldContain("h4");
        tags.ShouldContain("h5");
        tags.ShouldContain("h6");
    }

    [Fact]
    public void RichText_AllowsListTags()
    {
        var tags = SanitizationProfiles.RichText.AllowedTags;

        tags.ShouldContain("ul");
        tags.ShouldContain("ol");
        tags.ShouldContain("li");
    }

    [Fact]
    public void RichText_AllowsLinkAndImageTags()
    {
        var tags = SanitizationProfiles.RichText.AllowedTags;

        tags.ShouldContain("a");
        tags.ShouldContain("img");
    }

    [Fact]
    public void RichText_AllowsTableTags()
    {
        var tags = SanitizationProfiles.RichText.AllowedTags;

        tags.ShouldContain("table");
        tags.ShouldContain("thead");
        tags.ShouldContain("tbody");
        tags.ShouldContain("tr");
        tags.ShouldContain("th");
        tags.ShouldContain("td");
    }

    [Fact]
    public void RichText_AllowsSafeAttributes()
    {
        var attrs = SanitizationProfiles.RichText.AllowedAttributes;

        attrs.ShouldContain("href");
        attrs.ShouldContain("src");
        attrs.ShouldContain("alt");
        attrs.ShouldContain("title");
        attrs.ShouldContain("class");
    }

    [Fact]
    public void RichText_AllowsOnlyHttpsAndMailto()
    {
        var protocols = SanitizationProfiles.RichText.AllowedProtocols;

        protocols.ShouldContain("https");
        protocols.ShouldContain("mailto");
        protocols.Count.ShouldBe(2);
    }

    [Fact]
    public void RichText_StripsScripts()
    {
        SanitizationProfiles.RichText.StripScripts.ShouldBeTrue();
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
            markdownTags.ShouldContain(tag);
        }
    }

    [Fact]
    public void Markdown_AddsDefinitionListTags()
    {
        var tags = SanitizationProfiles.Markdown.AllowedTags;

        tags.ShouldContain("dl");
        tags.ShouldContain("dt");
        tags.ShouldContain("dd");
    }

    [Fact]
    public void Markdown_AddsDetailsSummaryTags()
    {
        var tags = SanitizationProfiles.Markdown.AllowedTags;

        tags.ShouldContain("details");
        tags.ShouldContain("summary");
    }

    [Fact]
    public void Markdown_AllowsIdAttribute()
    {
        SanitizationProfiles.Markdown.AllowedAttributes.ShouldContain("id");
    }

    [Fact]
    public void Markdown_AllowsTargetAttribute()
    {
        SanitizationProfiles.Markdown.AllowedAttributes.ShouldContain("target");
    }

    #endregion

    #region SanitizationProfileBuilder

    [Fact]
    public void Builder_EmptyBuild_ReturnsProfileWithDefaults()
    {
        var profile = new SanitizationProfileBuilder().Build();

        profile.AllowedTags.ShouldBeEmpty();
        profile.AllowedAttributes.ShouldBeEmpty();
        profile.AllowedProtocols.ShouldBeEmpty();
        profile.StripComments.ShouldBeTrue();
        profile.StripScripts.ShouldBeTrue();
    }

    [Fact]
    public void Builder_AllowTags_AddsTags()
    {
        var profile = new SanitizationProfileBuilder()
            .AllowTags("p", "b", "i")
            .Build();

        profile.AllowedTags.Count.ShouldBe(3);
        profile.AllowedTags.ShouldContain("p");
        profile.AllowedTags.ShouldContain("b");
        profile.AllowedTags.ShouldContain("i");
    }

    [Fact]
    public void Builder_AllowAttributes_AddsAttributes()
    {
        var profile = new SanitizationProfileBuilder()
            .AllowAttributes("href", "src")
            .Build();

        profile.AllowedAttributes.Count.ShouldBe(2);
    }

    [Fact]
    public void Builder_AllowProtocols_AddsProtocols()
    {
        var profile = new SanitizationProfileBuilder()
            .AllowProtocols("https", "mailto")
            .Build();

        profile.AllowedProtocols.Count.ShouldBe(2);
    }

    [Fact]
    public void Builder_WithStripComments_SetsFlag()
    {
        var profile = new SanitizationProfileBuilder()
            .WithStripComments(true)
            .Build();

        profile.StripComments.ShouldBeTrue();
    }

    [Fact]
    public void Builder_WithStripScripts_SetsFlag()
    {
        var profile = new SanitizationProfileBuilder()
            .WithStripScripts(true)
            .Build();

        profile.StripScripts.ShouldBeTrue();
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

        profile.AllowedTags.Count.ShouldBe(2);
        profile.AllowedAttributes.Count.ShouldBe(1);
        profile.AllowedProtocols.Count.ShouldBe(1);
        profile.StripComments.ShouldBeTrue();
        profile.StripScripts.ShouldBeTrue();
    }

    #endregion
}
