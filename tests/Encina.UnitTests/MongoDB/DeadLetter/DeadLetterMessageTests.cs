using Encina.MongoDB.DeadLetter;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Encina.UnitTests.MongoDB.DeadLetter;

public sealed class DeadLetterMessageTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NewMessage_HasEmptyStringDefaultsAndNoReplayState()
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
    public void Properties_RoundTripAssignedValues()
    {
        var id = Guid.NewGuid();

        var message = new DeadLetterMessage
        {
            Id = id,
            RequestType = "Orders.Create",
            RequestContent = "{}",
            ErrorCode = "orders.failed",
            ExceptionType = "System.InvalidOperationException",
            ExceptionStackTrace = "at X",
            CorrelationId = "corr-1",
            SourcePattern = "Outbox",
            SourceMessageId = "src-1",
            TenantId = "tenant-1",
            TotalRetryAttempts = 3,
            FirstFailedAtUtc = Now.AddHours(-1),
            DeadLetteredAtUtc = Now,
            ExpiresAtUtc = Now.AddDays(1),
            ReplayClaimedAtUtc = Now.AddMinutes(1),
            ReplayedAtUtc = Now.AddMinutes(2),
            ReplayResult = "success"
        };

        message.Id.ShouldBe(id);
        message.RequestType.ShouldBe("Orders.Create");
        message.RequestContent.ShouldBe("{}");
        message.ErrorCode.ShouldBe("orders.failed");
        message.ExceptionType.ShouldBe("System.InvalidOperationException");
        message.ExceptionStackTrace.ShouldBe("at X");
        message.CorrelationId.ShouldBe("corr-1");
        message.SourcePattern.ShouldBe("Outbox");
        message.SourceMessageId.ShouldBe("src-1");
        message.TenantId.ShouldBe("tenant-1");
        message.TotalRetryAttempts.ShouldBe(3);
        message.FirstFailedAtUtc.ShouldBe(Now.AddHours(-1));
        message.DeadLetteredAtUtc.ShouldBe(Now);
        message.ExpiresAtUtc.ShouldBe(Now.AddDays(1));
        message.ReplayClaimedAtUtc.ShouldBe(Now.AddMinutes(1));
        message.ReplayedAtUtc.ShouldBe(Now.AddMinutes(2));
        message.ReplayResult.ShouldBe("success");
    }

    [Fact]
    public void IsReplayed_WhenReplayedAtUtcIsSet_ReturnsTrue()
    {
        var message = new DeadLetterMessage { ReplayedAtUtc = Now };

        message.IsReplayed.ShouldBeTrue();
    }

    [Fact]
    public void IsExpiredAt_WithoutExpiry_ReturnsFalse()
    {
        var message = new DeadLetterMessage { ExpiresAtUtc = null };

        message.IsExpiredAt(Now).ShouldBeFalse();
    }

    [Fact]
    public void IsExpiredAt_BeforeExpiry_ReturnsFalse()
    {
        var message = new DeadLetterMessage { ExpiresAtUtc = Now.AddSeconds(1) };

        message.IsExpiredAt(Now).ShouldBeFalse();
    }

    [Fact]
    public void IsExpiredAt_AtExpiry_ReturnsTrue()
    {
        var message = new DeadLetterMessage { ExpiresAtUtc = Now };

        message.IsExpiredAt(Now).ShouldBeTrue();
    }

    [Fact]
    public void IsExpiredAt_AfterExpiry_ReturnsTrue()
    {
        var message = new DeadLetterMessage { ExpiresAtUtc = Now.AddSeconds(-1) };

        message.IsExpiredAt(Now).ShouldBeTrue();
    }

    [Fact]
    public void ToBsonDocument_UsesCamelCaseElementsStringIdAndOmitsNullOptionals()
    {
        var id = Guid.NewGuid();
        var message = new DeadLetterMessage
        {
            Id = id,
            RequestType = "T",
            SourcePattern = "Outbox",
            SourceMessageId = "src-1",
            ReplayedAtUtc = Now
        };

        var document = message.ToBsonDocument();

        document["_id"].ShouldBe(new BsonString(id.ToString()));
        document.Contains("requestType").ShouldBeTrue();
        document.Contains("sourcePattern").ShouldBeTrue();
        document.Contains("sourceMessageId").ShouldBeTrue();
        document.Contains("replayedAtUtc").ShouldBeTrue();
        document.Contains("exceptionType").ShouldBeFalse();
        document.Contains("exceptionStackTrace").ShouldBeFalse();
        document.Contains("correlationId").ShouldBeFalse();
        document.Contains("tenantId").ShouldBeFalse();
        document.Contains("replayResult").ShouldBeFalse();
        document.Contains("isReplayed").ShouldBeFalse();
    }

    [Fact]
    public void BsonRoundTrip_PreservesValuesAndUtcKind()
    {
        var message = new DeadLetterMessage
        {
            Id = Guid.NewGuid(),
            SourcePattern = "Inbox",
            SourceMessageId = "src-2",
            DeadLetteredAtUtc = Now,
            ExpiresAtUtc = Now.AddDays(1),
            ReplayResult = "success"
        };

        var copy = BsonSerializer.Deserialize<DeadLetterMessage>(message.ToBson());

        copy.Id.ShouldBe(message.Id);
        copy.SourcePattern.ShouldBe("Inbox");
        copy.DeadLetteredAtUtc.ShouldBe(Now);
        copy.DeadLetteredAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        copy.ExpiresAtUtc.ShouldBe(Now.AddDays(1));
        copy.ReplayResult.ShouldBe("success");
    }
}
