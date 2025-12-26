using Encina.Messaging.DeadLetter;
using Shouldly;

namespace Encina.Tests.DeadLetter;

public sealed class DeadLetterSourcePatternsTests
{
    [Fact]
    public void Recoverability_HasExpectedValue()
    {
        DeadLetterSourcePatterns.Recoverability.ShouldBe("Recoverability");
    }

    [Fact]
    public void Outbox_HasExpectedValue()
    {
        DeadLetterSourcePatterns.Outbox.ShouldBe("Outbox");
    }

    [Fact]
    public void Inbox_HasExpectedValue()
    {
        DeadLetterSourcePatterns.Inbox.ShouldBe("Inbox");
    }

    [Fact]
    public void Scheduling_HasExpectedValue()
    {
        DeadLetterSourcePatterns.Scheduling.ShouldBe("Scheduling");
    }

    [Fact]
    public void Saga_HasExpectedValue()
    {
        DeadLetterSourcePatterns.Saga.ShouldBe("Saga");
    }

    [Fact]
    public void Choreography_HasExpectedValue()
    {
        DeadLetterSourcePatterns.Choreography.ShouldBe("Choreography");
    }
}
