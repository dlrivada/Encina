using Encina.Dapper.SqlServer.DeadLetter;

namespace Encina.UnitTests.Dapper.SqlServer.DeadLetter;

/// <summary>
/// Unit tests for the Dapper SQL Server <see cref="DeadLetterMessage"/> data holder (#2879).
/// </summary>
public sealed class DeadLetterMessageTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NewMessage_HasEmptyStringsAndNoReplayState()
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
    public void Properties_RoundTripEveryAssignedValue()
    {
        var id = Guid.NewGuid();

        var message = new DeadLetterMessage
        {
            Id = id,
            RequestType = "Orders.PlaceOrder",
            RequestContent = "{}",
            ErrorCode = "orders.failed",
            ExceptionType = "System.InvalidOperationException",
            ExceptionStackTrace = "at X",
            CorrelationId = "corr-1",
            SourcePattern = "outbox",
            SourceMessageId = "src-1",
            TenantId = "tenant-1",
            TotalRetryAttempts = 5,
            FirstFailedAtUtc = Now.AddHours(-2),
            DeadLetteredAtUtc = Now.AddHours(-1),
            ExpiresAtUtc = Now.AddDays(1),
            ReplayClaimedAtUtc = Now,
            ReplayedAtUtc = Now.AddMinutes(1),
            ReplayResult = "success"
        };

        message.Id.ShouldBe(id);
        message.RequestType.ShouldBe("Orders.PlaceOrder");
        message.RequestContent.ShouldBe("{}");
        message.ErrorCode.ShouldBe("orders.failed");
        message.ExceptionType.ShouldBe("System.InvalidOperationException");
        message.ExceptionStackTrace.ShouldBe("at X");
        message.CorrelationId.ShouldBe("corr-1");
        message.SourcePattern.ShouldBe("outbox");
        message.SourceMessageId.ShouldBe("src-1");
        message.TenantId.ShouldBe("tenant-1");
        message.TotalRetryAttempts.ShouldBe(5);
        message.FirstFailedAtUtc.ShouldBe(Now.AddHours(-2));
        message.DeadLetteredAtUtc.ShouldBe(Now.AddHours(-1));
        message.ExpiresAtUtc.ShouldBe(Now.AddDays(1));
        message.ReplayClaimedAtUtc.ShouldBe(Now);
        message.ReplayedAtUtc.ShouldBe(Now.AddMinutes(1));
        message.ReplayResult.ShouldBe("success");
    }

    [Fact]
    public void IsReplayed_IsTrueOnlyWhenReplayedAtUtcIsSet()
    {
        var message = new DeadLetterMessage();

        message.IsReplayed.ShouldBeFalse();

        message.ReplayedAtUtc = Now;

        message.IsReplayed.ShouldBeTrue();
    }

    [Fact]
    public void IsExpiredAt_WithoutExpiry_IsFalse()
    {
        new DeadLetterMessage().IsExpiredAt(Now).ShouldBeFalse();
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void IsExpiredAt_ComparesExpiryWithTheSuppliedInstant(int expiryOffsetSeconds, bool expected)
    {
        var message = new DeadLetterMessage { ExpiresAtUtc = Now.AddSeconds(expiryOffsetSeconds) };

        message.IsExpiredAt(Now).ShouldBe(expected);
    }
}
