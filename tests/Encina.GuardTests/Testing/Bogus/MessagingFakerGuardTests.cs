using Encina.Testing.Bogus;

namespace Encina.GuardTests.Testing.Bogus;

/// <summary>
/// Guard tests for <see cref="OutboxMessageFaker"/>, <see cref="InboxMessageFaker"/>,
/// <see cref="ScheduledMessageFaker"/> and <see cref="SagaStateFaker"/>.
/// </summary>
public sealed class MessagingFakerGuardTests
{
    [Fact]
    public void OutboxMessageFaker_WithNotificationType_InvalidValue_ThrowsNamingNotificationType()
    {
        var faker = new OutboxMessageFaker();

        BogusGuardAssert.RejectsNullEmptyAndWhitespace(
            value => faker.WithNotificationType(value), "notificationType");
    }

    [Fact]
    public void OutboxMessageFaker_WithContent_InvalidValue_ThrowsNamingContent()
    {
        var faker = new OutboxMessageFaker();

        BogusGuardAssert.RejectsNullEmptyAndWhitespace(value => faker.WithContent(value), "content");
    }

    [Fact]
    public void InboxMessageFaker_WithMessageId_InvalidValue_ThrowsNamingMessageId()
    {
        var faker = new InboxMessageFaker();

        BogusGuardAssert.RejectsNullEmptyAndWhitespace(value => faker.WithMessageId(value), "messageId");
    }

    [Fact]
    public void InboxMessageFaker_WithRequestType_InvalidValue_ThrowsNamingRequestType()
    {
        var faker = new InboxMessageFaker();

        BogusGuardAssert.RejectsNullEmptyAndWhitespace(value => faker.WithRequestType(value), "requestType");
    }

    [Fact]
    public void ScheduledMessageFaker_WithRequestType_InvalidValue_ThrowsNamingRequestType()
    {
        var faker = new ScheduledMessageFaker();

        BogusGuardAssert.RejectsNullEmptyAndWhitespace(value => faker.WithRequestType(value), "requestType");
    }

    [Fact]
    public void ScheduledMessageFaker_WithContent_InvalidValue_ThrowsNamingContent()
    {
        var faker = new ScheduledMessageFaker();

        BogusGuardAssert.RejectsNullEmptyAndWhitespace(value => faker.WithContent(value), "content");
    }

    [Fact]
    public void SagaStateFaker_AtStep_Zero_IsAccepted()
    {
        var faker = new SagaStateFaker();

        faker.AtStep(0).ShouldBeSameAs(faker);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void SagaStateFaker_AtStep_NegativeStep_ThrowsNamingStep(int step)
    {
        var faker = new SagaStateFaker();

        BogusGuardAssert.Throws<ArgumentOutOfRangeException>(() => faker.AtStep(step), "step");
    }
}
