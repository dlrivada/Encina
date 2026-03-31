using Encina.MongoDB.Inbox;

namespace Encina.UnitTests.MongoDB.Inbox;

public sealed class InboxMessageTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var msg = new InboxMessage();
        msg.MessageId.ShouldBeEmpty();
        msg.RequestType.ShouldBeEmpty();
        msg.Response.ShouldBeNull();
        msg.ErrorMessage.ShouldBeNull();
        msg.ProcessedAtUtc.ShouldBeNull();
        msg.RetryCount.ShouldBe(0);
        msg.NextRetryAtUtc.ShouldBeNull();
    }

    [Fact]
    public void IsProcessed_WithProcessedAtAndResponse_ReturnsTrue()
    {
        var msg = new InboxMessage { ProcessedAtUtc = DateTime.UtcNow, Response = "{}" };
        msg.IsProcessed.ShouldBeTrue();
    }

    [Fact]
    public void IsProcessed_WithProcessedAtButNoResponse_ReturnsFalse()
    {
        var msg = new InboxMessage { ProcessedAtUtc = DateTime.UtcNow, Response = null };
        msg.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public void IsProcessed_WithNoProcessedAt_ReturnsFalse()
    {
        var msg = new InboxMessage { ProcessedAtUtc = null };
        msg.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public void IsExpired_PastDate_ReturnsTrue()
    {
        var msg = new InboxMessage { ExpiresAtUtc = DateTime.UtcNow.AddDays(-1) };
        msg.IsExpired().ShouldBeTrue();
    }

    [Fact]
    public void IsExpired_FutureDate_ReturnsFalse()
    {
        var msg = new InboxMessage { ExpiresAtUtc = DateTime.UtcNow.AddDays(1) };
        msg.IsExpired().ShouldBeFalse();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var now = DateTime.UtcNow;
        var msg = new InboxMessage
        {
            MessageId = "msg-1",
            RequestType = "ProcessOrder",
            Response = "{\"ok\":true}",
            ErrorMessage = null,
            ReceivedAtUtc = now,
            ProcessedAtUtc = now,
            ExpiresAtUtc = now.AddHours(1),
            RetryCount = 3,
            NextRetryAtUtc = now.AddMinutes(5)
        };

        msg.MessageId.ShouldBe("msg-1");
        msg.RequestType.ShouldBe("ProcessOrder");
        msg.RetryCount.ShouldBe(3);
    }
}
