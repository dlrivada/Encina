using Encina.EntityFrameworkCore.DeadLetter;
using Encina.Messaging.DeadLetter;

namespace Encina.UnitTests.EntityFrameworkCore.DeadLetter;

/// <summary>
/// Unit tests for <see cref="DeadLetterMessage"/>: defaults, property round trip and the derived members.
/// </summary>
public sealed class DeadLetterMessageTests
{
    private static readonly DateTime Instant = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NewMessage_HasEmptyStringsAndNoOptionalValues()
    {
        var message = new DeadLetterMessage();

        message.Id.ShouldBe(Guid.Empty);
        message.RequestType.ShouldBe(string.Empty);
        message.RequestContent.ShouldBe(string.Empty);
        message.ErrorCode.ShouldBe(string.Empty);
        message.SourcePattern.ShouldBe(string.Empty);
        message.SourceMessageId.ShouldBe(string.Empty);
        message.ExceptionType.ShouldBeNull();
        message.ExceptionStackTrace.ShouldBeNull();
        message.CorrelationId.ShouldBeNull();
        message.TenantId.ShouldBeNull();
        message.ExpiresAtUtc.ShouldBeNull();
        message.ReplayClaimedAtUtc.ShouldBeNull();
        message.ReplayedAtUtc.ShouldBeNull();
        message.ReplayResult.ShouldBeNull();
        message.TotalRetryAttempts.ShouldBe(0);
        message.IsReplayed.ShouldBeFalse();
    }

    [Fact]
    public void Properties_RoundTripEveryAssignedValue()
    {
        var id = Guid.NewGuid();

        var message = new DeadLetterMessage
        {
            Id = id,
            RequestType = "Orders.PlaceOrder",
            RequestContent = "{\"id\":1}",
            ErrorCode = "orders.failed",
            ExceptionType = "System.InvalidOperationException",
            ExceptionStackTrace = "at Foo",
            CorrelationId = "corr-1",
            SourcePattern = "Outbox",
            SourceMessageId = "src-1",
            TenantId = "tenant-1",
            TotalRetryAttempts = 5,
            FirstFailedAtUtc = Instant.AddHours(-2),
            DeadLetteredAtUtc = Instant,
            ExpiresAtUtc = Instant.AddDays(7),
            ReplayClaimedAtUtc = Instant.AddMinutes(1),
            ReplayedAtUtc = Instant.AddMinutes(2),
            ReplayResult = "success"
        };

        message.Id.ShouldBe(id);
        message.RequestType.ShouldBe("Orders.PlaceOrder");
        message.RequestContent.ShouldBe("{\"id\":1}");
        message.ErrorCode.ShouldBe("orders.failed");
        message.ExceptionType.ShouldBe("System.InvalidOperationException");
        message.ExceptionStackTrace.ShouldBe("at Foo");
        message.CorrelationId.ShouldBe("corr-1");
        message.SourcePattern.ShouldBe("Outbox");
        message.SourceMessageId.ShouldBe("src-1");
        message.TenantId.ShouldBe("tenant-1");
        message.TotalRetryAttempts.ShouldBe(5);
        message.FirstFailedAtUtc.ShouldBe(Instant.AddHours(-2));
        message.DeadLetteredAtUtc.ShouldBe(Instant);
        message.ExpiresAtUtc.ShouldBe(Instant.AddDays(7));
        message.ReplayClaimedAtUtc.ShouldBe(Instant.AddMinutes(1));
        message.ReplayedAtUtc.ShouldBe(Instant.AddMinutes(2));
        message.ReplayResult.ShouldBe("success");
    }

    [Fact]
    public void IsReplayed_FollowsReplayedAtUtc()
    {
        var message = new DeadLetterMessage();
        message.IsReplayed.ShouldBeFalse();

        message.ReplayedAtUtc = Instant;
        message.IsReplayed.ShouldBeTrue();

        message.ReplayedAtUtc = null;
        message.IsReplayed.ShouldBeFalse();
    }

    [Fact]
    public void IsExpiredAt_NoExpiry_IsNeverExpired()
    {
        var message = new DeadLetterMessage { ExpiresAtUtc = null };

        message.IsExpiredAt(DateTime.MaxValue).ShouldBeFalse();
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void IsExpiredAt_ComparesAgainstTheSuppliedInstant(int secondsFromExpiry, bool expected)
    {
        var message = new DeadLetterMessage { ExpiresAtUtc = Instant };

        message.IsExpiredAt(Instant.AddSeconds(secondsFromExpiry)).ShouldBe(expected);
    }
}
