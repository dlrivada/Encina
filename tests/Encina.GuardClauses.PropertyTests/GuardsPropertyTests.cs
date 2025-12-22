using Shouldly;

namespace Encina.GuardClauses.PropertyTests;

/// <summary>
/// Property-based tests for Guards.
/// Verifies invariants hold across different scenarios.
/// </summary>
public sealed class GuardsPropertyTests
{
    [Fact]
    public void Property_TryValidateNotNull_Idempotency_SameInputAlwaysSameResult()
    {
        // Arrange
        var validValue = "test";
        string? nullValue = null;

        // Act - Call multiple times
        var result1 = Guards.TryValidateNotNull(validValue, nameof(validValue), out _);
        var result2 = Guards.TryValidateNotNull(validValue, nameof(validValue), out _);
        var result3 = Guards.TryValidateNotNull(nullValue, nameof(nullValue), out _);
        var result4 = Guards.TryValidateNotNull(nullValue, nameof(nullValue), out _);

        // Assert - Property: Same input ALWAYS produces same result
        result1.ShouldBe(result2);
        result3.ShouldBe(result4);
    }

    [Fact]
    public void Property_TryValidatePositive_AllPositiveNumbers_AlwaysReturnTrue()
    {
        // Arrange
        var positiveNumbers = new[] { 1, 10, 100, 1000, int.MaxValue };

        // Act & Assert - Property: ALL positive numbers ALWAYS return true
        foreach (var num in positiveNumbers)
        {
            var result = Guards.TryValidatePositive(num, nameof(num), out _);
            result.ShouldBeTrue($"Expected {num} to be positive");
        }
    }

    [Fact]
    public void Property_TryValidateNegative_AllNegativeNumbers_AlwaysReturnTrue()
    {
        // Arrange
        var negativeNumbers = new[] { -1, -10, -100, -1000, int.MinValue };

        // Act & Assert - Property: ALL negative numbers ALWAYS return true
        foreach (var num in negativeNumbers)
        {
            var result = Guards.TryValidateNegative(num, nameof(num), out _);
            result.ShouldBeTrue($"Expected {num} to be negative");
        }
    }

    [Fact]
    public void Property_TryValidateInRange_BoundaryConditions_AlwaysConsistent()
    {
        // Arrange - Property: Min and Max are inclusive
        var testCases = new[]
        {
            (value: 1, min: 1, max: 100, expected: true),    // At min boundary
            (value: 100, min: 1, max: 100, expected: true),  // At max boundary
            (value: 0, min: 1, max: 100, expected: false),   // Below min
            (value: 101, min: 1, max: 100, expected: false), // Above max
            (value: 50, min: 1, max: 100, expected: true),   // Within range
        };

        // Act & Assert
        foreach (var testCase in testCases)
        {
            var result = Guards.TryValidateInRange(testCase.value, "value", testCase.min, testCase.max, out _);
            result.ShouldBe(testCase.expected,
                $"Value {testCase.value} in range [{testCase.min}, {testCase.max}] should be {testCase.expected}");
        }
    }

    [Fact]
    public void Property_TryValidateNotEmpty_Collection_EmptyCollectionAlwaysFails()
    {
        // Arrange
        var emptyArray = Array.Empty<int>();
        var emptyList = new List<string>();
        var emptyEnumerable = Enumerable.Empty<object>();

        // Act
        var result1 = Guards.TryValidateNotEmpty(emptyArray, nameof(emptyArray), out _);
        var result2 = Guards.TryValidateNotEmpty(emptyList, nameof(emptyList), out _);
        var result3 = Guards.TryValidateNotEmpty(emptyEnumerable, nameof(emptyEnumerable), out _);

        // Assert - Property: ALL empty collections ALWAYS return false
        result1.ShouldBeFalse();
        result2.ShouldBeFalse();
        result3.ShouldBeFalse();
    }

    [Fact]
    public void Property_TryValidateNotWhiteSpace_WhitespaceVariations_AlwaysFail()
    {
        // Arrange - Different whitespace scenarios
        var whitespaceVariations = new[] { " ", "  ", "\t", "\n", "\r\n", "   \t\n   " };

        // Act & Assert - Property: ALL whitespace strings ALWAYS fail
        foreach (var whitespace in whitespaceVariations)
        {
            var result = Guards.TryValidateNotWhiteSpace(whitespace, nameof(whitespace), out _);
            result.ShouldBeFalse($"Whitespace '{whitespace.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t")}' should fail");
        }
    }

    [Fact]
    public void Property_TryValidateEmail_CommonValidEmails_AlwaysPass()
    {
        // Arrange
        var validEmails = new[]
        {
            "user@example.com",
            "test.user@example.co.uk",
            "user+tag@example.com",
            "123@example.com",
            "user_name@example-domain.org"
        };

        // Act & Assert - Property: Valid email formats ALWAYS pass
        foreach (var email in validEmails)
        {
            var result = Guards.TryValidateEmail(email, nameof(email), out _);
            result.ShouldBeTrue($"Email '{email}' should be valid");
        }
    }

    [Fact]
    public void Property_TryValidateUrl_ValidHttpsUrls_AlwaysPass()
    {
        // Arrange
        var validUrls = new[]
        {
            "https://example.com",
            "http://example.com",
            "https://www.example.com/path",
            "https://example.com:8080/path?query=value"
        };

        // Act & Assert - Property: Valid HTTP(S) URLs ALWAYS pass
        foreach (var url in validUrls)
        {
            var result = Guards.TryValidateUrl(url, nameof(url), out _);
            result.ShouldBeTrue($"URL '{url}' should be valid");
        }
    }

    [Fact]
    public void Property_TryValidateNotEmpty_Guid_EmptyGuidAlwaysFails()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var result = Guards.TryValidateNotEmpty(emptyGuid, nameof(emptyGuid), out var error);

        // Assert - Property: Empty GUID ALWAYS fails
        result.ShouldBeFalse();
        error.Message.ShouldContain("cannot be an empty GUID");
    }

    [Fact]
    public void Property_CustomMessage_AlwaysOverridesDefaultMessage()
    {
        // Arrange
        string? nullValue = null;
        var customNull = "Custom null";
        var customEmpty = "Custom empty";
        var customPositive = "Custom positive";

        // Act
        Guards.TryValidateNotNull(nullValue, "param", out var error1, customNull);
        Guards.TryValidateNotEmpty("", "param", out var error2, customEmpty);
        Guards.TryValidatePositive(0, "param", out var error3, customPositive);

        // Assert - Property: Custom message ALWAYS used when provided
        error1.Message.ShouldBe(customNull);
        error2.Message.ShouldBe(customEmpty);
        error3.Message.ShouldBe(customPositive);
    }

    [Fact]
    public void Property_TryValidate_CustomCondition_BooleanLogic_AlwaysConsistent()
    {
        // Arrange
        var trueConditions = new[] { true, 1 > 0, "test" != null, 5 == 5 };
        var falseConditions = new[] { false, 1 < 0, "test" == null, 5 != 5 };

        // Act & Assert - Property: true conditions ALWAYS pass, false ALWAYS fail
        foreach (var condition in trueConditions)
        {
            var result = Guards.TryValidate(condition, "condition", out _);
            result.ShouldBeTrue();
        }

        foreach (var condition in falseConditions)
        {
            var result = Guards.TryValidate(condition, "condition", out _);
            result.ShouldBeFalse();
        }
    }
}
