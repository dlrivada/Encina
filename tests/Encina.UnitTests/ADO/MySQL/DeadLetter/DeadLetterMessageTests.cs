using Encina.ADO.MySQL.DeadLetter;

namespace Encina.UnitTests.ADO.MySQL.DeadLetter;

public sealed class DeadLetterMessageTests
{
    private static readonly DateTime Instant = new(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NewMessage_HasEmptyDefaults()
    {
        var message = new DeadLetterMessage();

        message.Id.ShouldBe(Guid.Empty);
        message.RequestType.ShouldBeEmpty();
        message.RequestContent.ShouldBeEmpty();
        message.ErrorCode.ShouldBeEmpty();
        message.SourcePattern.ShouldBeEmpty();
        message.SourceMessageId.ShouldBeEmpty();
        message.ExceptionType.ShouldBeNull();
        message.ExceptionStackTrace.ShouldBeNull();
        message.CorrelationId.ShouldBeNull();
        message.TenantId.ShouldBeNull();
        message.TotalRetryAttempts.ShouldBe(0);
        message.ExpiresAtUtc.ShouldBeNull();
        message.ReplayClaimedAtUtc.ShouldBeNull();
        message.ReplayedAtUtc.ShouldBeNull();
        message.ReplayResult.ShouldBeNull();
        message.IsReplayed.ShouldBeFalse();
    }

    [Fact]
    public void Properties_RoundTripThroughTheInterface()
    {
        var id = Guid.NewGuid();
        var message = new DeadLetterMessage
        {
            Id = id,
            RequestType = "OrderCommand",
            RequestContent = "{}",
            ErrorCode = "order.failed",
            ExceptionType = "System.InvalidOperationException",
            ExceptionStackTrace = "at Handler",
            CorrelationId = "corr-1",
            SourcePattern = "Outbox",
            SourceMessageId = "src-1",
            TenantId = "tenant-1",
            TotalRetryAttempts = 5,
            FirstFailedAtUtc = Instant.AddHours(-2),
            DeadLetteredAtUtc = Instant.AddHours(-1),
            ExpiresAtUtc = Instant.AddDays(1),
            ReplayClaimedAtUtc = Instant,
            ReplayedAtUtc = Instant.AddMinutes(1),
            ReplayResult = "success"
        };

        message.Id.ShouldBe(id);
        message.RequestType.ShouldBe("OrderCommand");
        message.RequestContent.ShouldBe("{}");
        message.ErrorCode.ShouldBe("order.failed");
        message.ExceptionType.ShouldBe("System.InvalidOperationException");
        message.ExceptionStackTrace.ShouldBe("at Handler");
        message.CorrelationId.ShouldBe("corr-1");
        message.SourcePattern.ShouldBe("Outbox");
        message.SourceMessageId.ShouldBe("src-1");
        message.TenantId.ShouldBe("tenant-1");
        message.TotalRetryAttempts.ShouldBe(5);
        message.FirstFailedAtUtc.ShouldBe(Instant.AddHours(-2));
        message.DeadLetteredAtUtc.ShouldBe(Instant.AddHours(-1));
        message.ExpiresAtUtc.ShouldBe(Instant.AddDays(1));
        message.ReplayClaimedAtUtc.ShouldBe(Instant);
        message.ReplayedAtUtc.ShouldBe(Instant.AddMinutes(1));
        message.ReplayResult.ShouldBe("success");
    }

    [Fact]
    public void IsReplayed_FollowsReplayedAtUtc()
    {
        var message = new DeadLetterMessage();
        message.IsReplayed.ShouldBeFalse();

        message.ReplayedAtUtc = Instant;

        message.IsReplayed.ShouldBeTrue();
    }

    [Fact]
    public void IsExpiredAt_WithoutExpiry_IsNeverExpired()
        => new DeadLetterMessage().IsExpiredAt(DateTime.MaxValue).ShouldBeFalse();

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void IsExpiredAt_ComparesTheExpiryToTheSuppliedInstant(int expiryOffsetSeconds, bool expected)
    {
        var message = new DeadLetterMessage { ExpiresAtUtc = Instant.AddSeconds(expiryOffsetSeconds) };

        message.IsExpiredAt(Instant).ShouldBe(expected);
    }
}
