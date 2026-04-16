#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using Shouldly;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="SpecialCategoryDataCriterion"/>.
/// </summary>
public class SpecialCategoryDataCriterionTests
{
    private readonly SpecialCategoryDataCriterion _sut = new();

    private static DPIAContext CreateContext(
        IReadOnlyList<string> dataCategories,
        params string[] triggers) => new()
        {
            RequestType = typeof(object),
            DataCategories = dataCategories,
            HighRiskTriggers = triggers,
        };

    #region Name Tests

    [Fact]
    public void Name_ShouldReturnExpectedValue()
    {
        _sut.Name.ShouldBe("Special Category Data (Art. 35(3)(b))");
    }

    #endregion

    #region EvaluateAsync Tests

    [Fact]
    public async Task EvaluateAsync_NullContext_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.EvaluateAsync(null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task EvaluateAsync_NoSpecialCategories_ReturnsNull()
    {
        var context = CreateContext(["general", "marketing"]);

        var result = await _sut.EvaluateAsync(context);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task EvaluateAsync_EmptyCategories_ReturnsNull()
    {
        var context = CreateContext([]);

        var result = await _sut.EvaluateAsync(context);

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("health")]
    [InlineData("biometric")]
    [InlineData("genetic")]
    [InlineData("racial")]
    [InlineData("ethnic")]
    [InlineData("political")]
    [InlineData("religious")]
    [InlineData("philosophical")]
    [InlineData("trade-union")]
    [InlineData("sexual-orientation")]
    [InlineData("criminal")]
    public async Task EvaluateAsync_SpecialCategory_ReturnsHigh(string category)
    {
        var context = CreateContext([category]);

        var result = await _sut.EvaluateAsync(context);

        result.ShouldNotBeNull();
        result!.Level.ShouldBe(RiskLevel.High);
        result.Category.ShouldBe("Special Category Data");
    }

    [Fact]
    public async Task EvaluateAsync_SpecialCategoryCaseInsensitive_ReturnsRiskItem()
    {
        var context = CreateContext(["Health", "BIOMETRIC"]);

        var result = await _sut.EvaluateAsync(context);

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task EvaluateAsync_SpecialCategoryWithLargeScale_ReturnsVeryHigh()
    {
        var context = CreateContext(
            ["health"],
            HighRiskTriggers.LargeScaleProcessing);

        var result = await _sut.EvaluateAsync(context);

        result.ShouldNotBeNull();
        result!.Level.ShouldBe(RiskLevel.VeryHigh);
    }

    [Fact]
    public async Task EvaluateAsync_MultipleSpecialCategories_IncludesAllInDescription()
    {
        var context = CreateContext(["health", "biometric"]);

        var result = await _sut.EvaluateAsync(context);

        result.ShouldNotBeNull();
        result!.Description.ShouldContain("health");
        result.Description.ShouldContain("biometric");
    }

    #endregion
}
