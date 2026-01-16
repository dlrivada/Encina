using Encina.Messaging.DeadLetter;
using Shouldly;

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// Unit tests for <see cref="DeadLetterFilter"/>.
/// </summary>
public sealed class DeadLetterFilterTests
{
    [Fact]
    public void DefaultValues_AreAllNull()
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
    public void CanSetSourcePattern()
    {
        // Arrange & Act
        var filter = new DeadLetterFilter { SourcePattern = "Outbox" };

        // Assert
        filter.SourcePattern.ShouldBe("Outbox");
    }

    [Fact]
    public void CanSetRequestType()
    {
        // Arrange & Act
        var filter = new DeadLetterFilter { RequestType = "TestRequest" };

        // Assert
        filter.RequestType.ShouldBe("TestRequest");
    }

    [Fact]
    public void CanSetErrorCode()
    {
        // Arrange & Act
        var filter = new DeadLetterFilter { ErrorCode = "ERR001" };

        // Assert
        filter.ErrorCode.ShouldBe("ERR001");
    }

    [Fact]
    public void CanSetCorrelationId()
    {
        // Arrange & Act
        var filter = new DeadLetterFilter { CorrelationId = "corr-123" };

        // Assert
        filter.CorrelationId.ShouldBe("corr-123");
    }

    [Fact]
    public void CanSetExcludeReplayed()
    {
        // Arrange & Act
        var filter = new DeadLetterFilter { ExcludeReplayed = true };

        // Assert
        filter.ExcludeReplayed.ShouldBe(true);
    }

    [Fact]
    public void CanSetDeadLetteredAfterUtc()
    {
        // Arrange
        var date = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var filter = new DeadLetterFilter { DeadLetteredAfterUtc = date };

        // Assert
        filter.DeadLetteredAfterUtc.ShouldBe(date);
    }

    [Fact]
    public void CanSetDeadLetteredBeforeUtc()
    {
        // Arrange
        var date = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var filter = new DeadLetterFilter { DeadLetteredBeforeUtc = date };

        // Assert
        filter.DeadLetteredBeforeUtc.ShouldBe(date);
    }

    #region Static Factory Methods

    [Fact]
    public void All_ReturnsEmptyFilter()
    {
        // Act
        var filter = DeadLetterFilter.All;

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
    public void FromSource_SetsSourcePatternAndExcludeReplayed()
    {
        // Act
        var filter = DeadLetterFilter.FromSource("Recoverability");

        // Assert
        filter.SourcePattern.ShouldBe("Recoverability");
        filter.ExcludeReplayed.ShouldBe(true);
        filter.RequestType.ShouldBeNull();
        filter.ErrorCode.ShouldBeNull();
        filter.CorrelationId.ShouldBeNull();
    }

    [Fact]
    public void FromSource_WithOutbox_SetsCorrectly()
    {
        // Act
        var filter = DeadLetterFilter.FromSource(DeadLetterSourcePatterns.Outbox);

        // Assert
        filter.SourcePattern.ShouldBe("Outbox");
        filter.ExcludeReplayed.ShouldBe(true);
    }

    [Fact]
    public void Since_SetsDeadLetteredAfterUtcAndExcludeReplayed()
    {
        // Arrange
        var since = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var filter = DeadLetterFilter.Since(since);

        // Assert
        filter.DeadLetteredAfterUtc.ShouldBe(since);
        filter.ExcludeReplayed.ShouldBe(true);
        filter.SourcePattern.ShouldBeNull();
        filter.RequestType.ShouldBeNull();
    }

    [Fact]
    public void ByCorrelationId_SetsCorrelationId()
    {
        // Act
        var filter = DeadLetterFilter.ByCorrelationId("test-correlation-123");

        // Assert
        filter.CorrelationId.ShouldBe("test-correlation-123");
        filter.ExcludeReplayed.ShouldBeNull();
        filter.SourcePattern.ShouldBeNull();
        filter.RequestType.ShouldBeNull();
    }

    #endregion

    [Fact]
    public void CanSetAllProperties()
    {
        // Arrange
        var afterDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var beforeDate = new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var filter = new DeadLetterFilter
        {
            SourcePattern = "Inbox",
            RequestType = "OrderCommand",
            ErrorCode = "ORD_001",
            CorrelationId = "corr-xyz",
            ExcludeReplayed = false,
            DeadLetteredAfterUtc = afterDate,
            DeadLetteredBeforeUtc = beforeDate
        };

        // Assert
        filter.SourcePattern.ShouldBe("Inbox");
        filter.RequestType.ShouldBe("OrderCommand");
        filter.ErrorCode.ShouldBe("ORD_001");
        filter.CorrelationId.ShouldBe("corr-xyz");
        filter.ExcludeReplayed.ShouldBe(false);
        filter.DeadLetteredAfterUtc.ShouldBe(afterDate);
        filter.DeadLetteredBeforeUtc.ShouldBe(beforeDate);
    }
}

/// <summary>
/// Unit tests for <see cref="DeadLetterSourcePatterns"/>.
/// </summary>
public sealed class DeadLetterSourcePatternsTests
{
    [Fact]
    public void Recoverability_HasCorrectValue()
    {
        DeadLetterSourcePatterns.Recoverability.ShouldBe("Recoverability");
    }

    [Fact]
    public void Outbox_HasCorrectValue()
    {
        DeadLetterSourcePatterns.Outbox.ShouldBe("Outbox");
    }

    [Fact]
    public void Inbox_HasCorrectValue()
    {
        DeadLetterSourcePatterns.Inbox.ShouldBe("Inbox");
    }

    [Fact]
    public void Scheduling_HasCorrectValue()
    {
        DeadLetterSourcePatterns.Scheduling.ShouldBe("Scheduling");
    }

    [Fact]
    public void Saga_HasCorrectValue()
    {
        DeadLetterSourcePatterns.Saga.ShouldBe("Saga");
    }

    [Fact]
    public void Choreography_HasCorrectValue()
    {
        DeadLetterSourcePatterns.Choreography.ShouldBe("Choreography");
    }
}
