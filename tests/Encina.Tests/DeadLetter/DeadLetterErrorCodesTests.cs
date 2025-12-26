using Encina.Messaging.DeadLetter;
using Shouldly;

namespace Encina.Tests.DeadLetter;

public sealed class DeadLetterErrorCodesTests
{
    [Fact]
    public void NotFound_HasExpectedValue()
    {
        DeadLetterErrorCodes.NotFound.ShouldBe("dlq.not_found");
    }

    [Fact]
    public void AlreadyReplayed_HasExpectedValue()
    {
        DeadLetterErrorCodes.AlreadyReplayed.ShouldBe("dlq.already_replayed");
    }

    [Fact]
    public void Expired_HasExpectedValue()
    {
        DeadLetterErrorCodes.Expired.ShouldBe("dlq.expired");
    }

    [Fact]
    public void DeserializationFailed_HasExpectedValue()
    {
        DeadLetterErrorCodes.DeserializationFailed.ShouldBe("dlq.deserialization_failed");
    }

    [Fact]
    public void ReplayFailed_HasExpectedValue()
    {
        DeadLetterErrorCodes.ReplayFailed.ShouldBe("dlq.replay_failed");
    }

    [Fact]
    public void StoreFailed_HasExpectedValue()
    {
        DeadLetterErrorCodes.StoreFailed.ShouldBe("dlq.store_failed");
    }

    [Fact]
    public void AllCodes_StartWithDlqPrefix()
    {
        // Verify all codes follow naming convention
        DeadLetterErrorCodes.NotFound.ShouldStartWith("dlq.");
        DeadLetterErrorCodes.AlreadyReplayed.ShouldStartWith("dlq.");
        DeadLetterErrorCodes.Expired.ShouldStartWith("dlq.");
        DeadLetterErrorCodes.DeserializationFailed.ShouldStartWith("dlq.");
        DeadLetterErrorCodes.ReplayFailed.ShouldStartWith("dlq.");
        DeadLetterErrorCodes.StoreFailed.ShouldStartWith("dlq.");
    }
}
