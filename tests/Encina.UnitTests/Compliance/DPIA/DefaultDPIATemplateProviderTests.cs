#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;

using Shouldly;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DefaultDPIATemplateProvider"/>.
/// </summary>
public class DefaultDPIATemplateProviderTests
{
    private readonly DefaultDPIATemplateProvider _sut = new();

    #region GetTemplateAsync Tests

    [Theory]
    [InlineData("profiling")]
    [InlineData("special-category")]
    [InlineData("public-monitoring")]
    [InlineData("ai-ml")]
    [InlineData("biometric")]
    [InlineData("health-data")]
    [InlineData("general")]
    public async Task GetTemplateAsync_KnownType_ReturnsTemplate(string processingType)
    {
        var result = await _sut.GetTemplateAsync(processingType);

        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: t =>
            {
                t.ProcessingType.ShouldBe(processingType);
                t.Name.ShouldNotBeNullOrWhiteSpace();
                t.Description.ShouldNotBeNullOrWhiteSpace();
                t.Sections.ShouldNotBeEmpty();
                t.RiskCategories.ShouldNotBeEmpty();
                t.SuggestedMitigations.ShouldNotBeEmpty();
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetTemplateAsync_UnknownType_FallsBackToGeneral()
    {
        var result = await _sut.GetTemplateAsync("unknown-type");

        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: t => t.ProcessingType.ShouldBe("general"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetTemplateAsync_CaseInsensitive_MatchesTemplate()
    {
        var result = await _sut.GetTemplateAsync("PROFILING");

        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: t => t.ProcessingType.ShouldBe("profiling"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetAllTemplatesAsync Tests

    [Fact]
    public async Task GetAllTemplatesAsync_Returns7Templates()
    {
        var result = await _sut.GetAllTemplatesAsync();

        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: templates => templates.Count.ShouldBe(7),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetAllTemplatesAsync_AllHaveRequiredSections()
    {
        var result = await _sut.GetAllTemplatesAsync();

        _ = result.Match(
            Right: templates =>
            {
                foreach (var template in templates)
                {
                    template.Sections.ShouldNotBeEmpty();
                    template.Sections.ShouldAllBe(
                        s => s.IsRequired,
                        $"All sections in '{template.Name}' should be required");
                }
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Template Content Tests

    [Fact]
    public async Task ProfilingTemplate_HasExpectedRiskCategories()
    {
        var result = await _sut.GetTemplateAsync("profiling");

        _ = result.Match(
            Right: t => t.RiskCategories.ShouldContain("Automated Decision-Making"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task SpecialCategoryTemplate_HasExpectedRiskCategories()
    {
        var result = await _sut.GetTemplateAsync("special-category");

        _ = result.Match(
            Right: t => t.RiskCategories.ShouldContain("Special Category Data"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion
}
