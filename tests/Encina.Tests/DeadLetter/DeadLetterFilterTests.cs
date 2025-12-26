using Encina.Messaging.DeadLetter;
using Shouldly;

namespace Encina.Tests.DeadLetter;

public sealed class DeadLetterFilterTests
{
    [Fact]
    public void DefaultValues_AreNull()
    {
        // Arrange & Act
        var filter = new DeadLetterFilter();

        // Assert
        filter.SourcePattern.ShouldBeNull();
        filter.RequestType.ShouldBeNull();
        filter.ErrorCode.ShouldBeNull();
        filter.CorrelationId.ShouldBeNull();
        filter.ExcludeReplayed.ShouldBeNull();
        filter.DeadLetteredAfterUtc.ShouldBeNull();
        filter.DeadLetteredBeforeUtc.ShouldBeNull();
    }

    [Fact]
    public void All_ReturnsEmptyFilter()
    {
        // Act
        var filter = DeadLetterFilter.All;

        // Assert
        filter.SourcePattern.ShouldBeNull();
        filter.ExcludeReplayed.ShouldBeNull();
    }

    [Fact]
    public void FromSource_CreatesFilterWithSourcePatternAndExcludeReplayed()
    {
        // Act
        var filter = DeadLetterFilter.FromSource("Recoverability");

        // Assert
        filter.SourcePattern.ShouldBe("Recoverability");
        filter.ExcludeReplayed.ShouldBe(true);
    }

    [Fact]
    public void Since_CreatesFilterWithDateAndExcludeReplayed()
    {
        // Arrange
        var sinceDate = DateTime.UtcNow.AddHours(-1);

        // Act
        var filter = DeadLetterFilter.Since(sinceDate);

        // Assert
        filter.DeadLetteredAfterUtc.ShouldBe(sinceDate);
        filter.ExcludeReplayed.ShouldBe(true);
    }

    [Fact]
    public void ByCorrelationId_CreatesFilterWithCorrelationId()
    {
        // Act
        var filter = DeadLetterFilter.ByCorrelationId("test-correlation-123");

        // Assert
        filter.CorrelationId.ShouldBe("test-correlation-123");
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var filter = new DeadLetterFilter
        {
            SourcePattern = "Outbox",
            RequestType = "MyRequest",
            ErrorCode = "error.code",
            CorrelationId = "correlation-123",
            ExcludeReplayed = true,
            DeadLetteredAfterUtc = now.AddDays(-1),
            DeadLetteredBeforeUtc = now
        };

        // Assert
        filter.SourcePattern.ShouldBe("Outbox");
        filter.RequestType.ShouldBe("MyRequest");
        filter.ErrorCode.ShouldBe("error.code");
        filter.CorrelationId.ShouldBe("correlation-123");
        filter.ExcludeReplayed.ShouldBe(true);
        filter.DeadLetteredAfterUtc.ShouldBe(now.AddDays(-1));
        filter.DeadLetteredBeforeUtc.ShouldBe(now);
    }
}
