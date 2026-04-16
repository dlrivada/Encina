using System.Text.Json;
using Encina.Security.Audit;
using Shouldly;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="AuditOutcome"/> enum.
/// </summary>
public class AuditOutcomeTests
{
    [Fact]
    public void AllValues_ShouldExist()
    {
        // Assert
        Enum.GetValues<AuditOutcome>().Count.ShouldBe(4);
        Enum.IsDefined(AuditOutcome.Success).ShouldBeTrue();
        Enum.IsDefined(AuditOutcome.Failure).ShouldBeTrue();
        Enum.IsDefined(AuditOutcome.Denied).ShouldBeTrue();
        Enum.IsDefined(AuditOutcome.Error).ShouldBeTrue();
    }

    [Theory]
    [InlineData(AuditOutcome.Success, 0)]
    [InlineData(AuditOutcome.Failure, 1)]
    [InlineData(AuditOutcome.Denied, 2)]
    [InlineData(AuditOutcome.Error, 3)]
    public void Values_ShouldHaveExpectedUnderlyingValue(AuditOutcome outcome, int expectedValue)
    {
        // Assert
        ((int)outcome).ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData(AuditOutcome.Success)]
    [InlineData(AuditOutcome.Failure)]
    [InlineData(AuditOutcome.Denied)]
    [InlineData(AuditOutcome.Error)]
    public void JsonSerialization_ShouldRoundTrip(AuditOutcome outcome)
    {
        // Arrange
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var json = JsonSerializer.Serialize(outcome, options);
        var deserialized = JsonSerializer.Deserialize<AuditOutcome>(json, options);

        // Assert
        deserialized.ShouldBe(outcome);
    }

    [Theory]
    [InlineData(AuditOutcome.Success, "Success")]
    [InlineData(AuditOutcome.Failure, "Failure")]
    [InlineData(AuditOutcome.Denied, "Denied")]
    [InlineData(AuditOutcome.Error, "Error")]
    public void ToString_ShouldReturnExpectedName(AuditOutcome outcome, string expectedName)
    {
        // Assert
        outcome.ToString().ShouldBe(expectedName);
    }

    [Theory]
    [InlineData("Success", AuditOutcome.Success)]
    [InlineData("Failure", AuditOutcome.Failure)]
    [InlineData("Denied", AuditOutcome.Denied)]
    [InlineData("Error", AuditOutcome.Error)]
    public void Parse_ShouldConvertFromString(string name, AuditOutcome expected)
    {
        // Act
        var result = Enum.Parse<AuditOutcome>(name);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void TryParse_ShouldReturnFalseForInvalidValue()
    {
        // Act
        var result = Enum.TryParse<AuditOutcome>("Invalid", out _);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void DefaultValue_ShouldBeSuccess()
    {
        // Arrange & Act
        var defaultValue = default(AuditOutcome);

        // Assert - Default enum value is 0, which is Success
        defaultValue.ShouldBe(AuditOutcome.Success);
    }

    [Fact]
    public void JsonSerialization_WithAuditEntry_ShouldWorkCorrectly()
    {
        // Arrange
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "test-correlation",
            Action = "Test",
            EntityType = "TestEntity",
            Outcome = AuditOutcome.Denied,
            TimestampUtc = DateTime.UtcNow,
            StartedAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        var json = JsonSerializer.Serialize(entry, options);
        var deserialized = JsonSerializer.Deserialize<AuditEntry>(json, options);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized!.Outcome.ShouldBe(AuditOutcome.Denied);
    }
}
