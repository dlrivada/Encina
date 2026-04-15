using Encina.GuardClauses;

using Shouldly;

namespace Encina.GuardTests.Validation.GuardClauses;

/// <summary>
/// Guard tests for Encina.GuardClauses exercising each TryValidate* method
/// with both valid and invalid inputs to generate line coverage.
/// </summary>
[Trait("Category", "Guard")]
public sealed class GuardsGuardTests
{
    // Since EncinaError is a readonly record struct, it cannot be null; the
    // equivalent safety check is that it was not left as its default value.
    private static void AssertInvalid(bool result, EncinaError error)
    {
        result.ShouldBeFalse();
        error.ShouldNotBe(default);
        error.Message.ShouldNotBeNullOrEmpty();
    }

    // ─── TryValidateNotNull ───

    [Fact]
    public void TryValidateNotNull_Null_ReturnsFalse()
    {
        var result = Guards.TryValidateNotNull<string>(null, "param", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidateNotNull_Valid_ReturnsTrue()
    {
        var result = Guards.TryValidateNotNull("value", "param", out _);
        result.ShouldBeTrue();
    }

    // ─── TryValidateNotEmpty (string) ───

    [Fact]
    public void TryValidateNotEmpty_String_Null_ReturnsFalse()
    {
        var result = Guards.TryValidateNotEmpty((string?)null, "param", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidateNotEmpty_String_Empty_ReturnsFalse()
    {
        var result = Guards.TryValidateNotEmpty("", "param", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidateNotEmpty_String_Valid_ReturnsTrue()
    {
        var result = Guards.TryValidateNotEmpty("hello", "param", out _);
        result.ShouldBeTrue();
    }

    // ─── TryValidateNotWhiteSpace ───

    [Fact]
    public void TryValidateNotWhiteSpace_Whitespace_ReturnsFalse()
    {
        var result = Guards.TryValidateNotWhiteSpace("   ", "param", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidateNotWhiteSpace_Valid_ReturnsTrue()
    {
        var result = Guards.TryValidateNotWhiteSpace("text", "param", out _);
        result.ShouldBeTrue();
    }

    // ─── TryValidateNotEmpty (collection) ───

    [Fact]
    public void TryValidateNotEmpty_Collection_Null_ReturnsFalse()
    {
        var result = Guards.TryValidateNotEmpty<int>(null, "param", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidateNotEmpty_Collection_Empty_ReturnsFalse()
    {
        var result = Guards.TryValidateNotEmpty(Array.Empty<int>(), "param", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidateNotEmpty_Collection_Valid_ReturnsTrue()
    {
        int[] items = [1, 2, 3];
        var result = Guards.TryValidateNotEmpty(items, "param", out _);
        result.ShouldBeTrue();
    }

    // ─── TryValidatePositive ───

    [Fact]
    public void TryValidatePositive_Negative_ReturnsFalse()
    {
        var result = Guards.TryValidatePositive(-1, "param", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidatePositive_Zero_ReturnsFalse()
    {
        var result = Guards.TryValidatePositive(0, "param", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidatePositive_Positive_ReturnsTrue()
    {
        var result = Guards.TryValidatePositive(42, "param", out _);
        result.ShouldBeTrue();
    }

    // ─── TryValidateNegative ───

    [Fact]
    public void TryValidateNegative_Positive_ReturnsFalse()
    {
        var result = Guards.TryValidateNegative(1, "param", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidateNegative_Negative_ReturnsTrue()
    {
        var result = Guards.TryValidateNegative(-5, "param", out _);
        result.ShouldBeTrue();
    }

    // ─── TryValidateInRange ───

    [Fact]
    public void TryValidateInRange_BelowMin_ReturnsFalse()
    {
        var result = Guards.TryValidateInRange(0, "param", 1, 10, out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidateInRange_AboveMax_ReturnsFalse()
    {
        var result = Guards.TryValidateInRange(11, "param", 1, 10, out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidateInRange_InRange_ReturnsTrue()
    {
        var result = Guards.TryValidateInRange(5, "param", 1, 10, out _);
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidateInRange_AtMin_ReturnsTrue()
    {
        var result = Guards.TryValidateInRange(1, "param", 1, 10, out _);
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidateInRange_AtMax_ReturnsTrue()
    {
        var result = Guards.TryValidateInRange(10, "param", 1, 10, out _);
        result.ShouldBeTrue();
    }

    // ─── TryValidateEmail ───

    [Fact]
    public void TryValidateEmail_Null_ReturnsFalse()
    {
        var result = Guards.TryValidateEmail(null, "param", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidateEmail_Invalid_ReturnsFalse()
    {
        var result = Guards.TryValidateEmail("not-an-email", "param", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidateEmail_Valid_ReturnsTrue()
    {
        var result = Guards.TryValidateEmail("user@example.com", "param", out _);
        result.ShouldBeTrue();
    }

    // ─── TryValidateUrl ───

    [Fact]
    public void TryValidateUrl_Null_ReturnsFalse()
    {
        var result = Guards.TryValidateUrl(null, "param", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidateUrl_Invalid_ReturnsFalse()
    {
        var result = Guards.TryValidateUrl("not-a-url", "param", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidateUrl_Valid_ReturnsTrue()
    {
        var result = Guards.TryValidateUrl("https://example.com", "param", out _);
        result.ShouldBeTrue();
    }

    // ─── TryValidateNotEmpty (Guid) ───

    [Fact]
    public void TryValidateNotEmpty_Guid_Empty_ReturnsFalse()
    {
        var result = Guards.TryValidateNotEmpty(Guid.Empty, "param", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidateNotEmpty_Guid_Valid_ReturnsTrue()
    {
        var result = Guards.TryValidateNotEmpty(Guid.NewGuid(), "param", out _);
        result.ShouldBeTrue();
    }

    // ─── TryValidate (condition) ───

    [Fact]
    public void TryValidate_False_ReturnsFalse()
    {
        var result = Guards.TryValidate(false, "param", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidate_True_ReturnsTrue()
    {
        var result = Guards.TryValidate(true, "param", out _);
        result.ShouldBeTrue();
    }

    // ─── TryValidatePattern ───

    [Fact]
    public void TryValidatePattern_Null_ReturnsFalse()
    {
        var result = Guards.TryValidatePattern(null, "param", @"^\d+$", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidatePattern_NoMatch_ReturnsFalse()
    {
        var result = Guards.TryValidatePattern("abc", "param", @"^\d+$", out var error);
        AssertInvalid(result, error);
    }

    [Fact]
    public void TryValidatePattern_Match_ReturnsTrue()
    {
        var result = Guards.TryValidatePattern("123", "param", @"^\d+$", out _);
        result.ShouldBeTrue();
    }

    // ─── Custom message parameter ───

    [Fact]
    public void TryValidateNotNull_CustomMessage_IncludedInError()
    {
        Guards.TryValidateNotNull<string>(null, "param", out var error, "custom msg");
        error.Message.ShouldContain("custom msg");
    }
}
