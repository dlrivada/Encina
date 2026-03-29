using Encina.Messaging.Inbox;
using Encina.Testing.Bogus;

namespace Encina.UnitTests.Testing.Bogus;

/// <summary>
/// Unit tests for <see cref="InboxMessageFaker"/> and <see cref="FakeInboxMessage"/>.
/// </summary>
public sealed class InboxMessageFakerTests
{
    #region Default Generation

    [Fact]
    public void Generate_Default_ShouldCreatePendingMessage()
    {
        var msg = new InboxMessageFaker().Generate();

        msg.ShouldNotBeNull();
        msg.MessageId.ShouldNotBeNullOrEmpty();
        msg.RequestType.ShouldNotBeNullOrEmpty();
        msg.Response.ShouldBeNull();
        msg.ErrorMessage.ShouldBeNull();
        msg.ReceivedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        msg.ProcessedAtUtc.ShouldBeNull();
        msg.RetryCount.ShouldBe(0);
        msg.NextRetryAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Generate_Default_IsNotProcessed()
    {
        var msg = new InboxMessageFaker().Generate();
        msg.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public void GenerateMessage_ReturnsIInboxMessage()
    {
        var msg = new InboxMessageFaker().GenerateMessage();
        msg.ShouldBeAssignableTo<IInboxMessage>();
    }

    [Fact]
    public void Generate_Reproducible_WithSameSeed()
    {
        var msg1 = new InboxMessageFaker().Generate();
        var msg2 = new InboxMessageFaker().Generate();
        msg1.MessageId.ShouldBe(msg2.MessageId);
    }

    #endregion

    #region AsProcessed

    [Fact]
    public void AsProcessed_ShouldSetProcessedAtUtcAndResponse()
    {
        var msg = new InboxMessageFaker().AsProcessed().Generate();
        msg.ProcessedAtUtc.ShouldNotBeNull();
        msg.Response.ShouldNotBeNull();
        msg.IsProcessed.ShouldBeTrue();
    }

    [Fact]
    public void AsProcessed_WithCustomResponse_ShouldUseIt()
    {
        var msg = new InboxMessageFaker().AsProcessed("{\"ok\":true}").Generate();
        msg.Response.ShouldBe("{\"ok\":true}");
    }

    #endregion

    #region AsFailed

    [Fact]
    public void AsFailed_ShouldSetErrorAndRetry()
    {
        var msg = new InboxMessageFaker().AsFailed(4).Generate();
        msg.ErrorMessage.ShouldNotBeNullOrEmpty();
        msg.RetryCount.ShouldBe(4);
        msg.NextRetryAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void AsFailed_DefaultRetryCount_Is2()
    {
        var msg = new InboxMessageFaker().AsFailed().Generate();
        msg.RetryCount.ShouldBe(2);
    }

    #endregion

    #region AsExpired

    [Fact]
    public void AsExpired_ShouldSetExpiresInPast()
    {
        var msg = new InboxMessageFaker().AsExpired().Generate();
        msg.ExpiresAtUtc.ShouldBeLessThan(DateTime.UtcNow);
        msg.IsExpired().ShouldBeTrue();
    }

    #endregion

    #region WithMessageId / WithRequestType

    [Fact]
    public void WithMessageId_ShouldOverride()
    {
        var msg = new InboxMessageFaker().WithMessageId("msg-123").Generate();
        msg.MessageId.ShouldBe("msg-123");
    }

    [Fact]
    public void WithMessageId_Null_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new InboxMessageFaker().WithMessageId(null!));
    }

    [Fact]
    public void WithMessageId_Whitespace_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => new InboxMessageFaker().WithMessageId("  "));
    }

    [Fact]
    public void WithRequestType_ShouldOverride()
    {
        var msg = new InboxMessageFaker().WithRequestType("ProcessOrder").Generate();
        msg.RequestType.ShouldBe("ProcessOrder");
    }

    [Fact]
    public void WithRequestType_Null_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new InboxMessageFaker().WithRequestType(null!));
    }

    [Fact]
    public void WithRequestType_Whitespace_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => new InboxMessageFaker().WithRequestType(" "));
    }

    #endregion

    #region Method Chaining

    [Fact]
    public void Methods_ReturnSameInstance_ForChaining()
    {
        var faker = new InboxMessageFaker();
        faker.AsProcessed().ShouldBeSameAs(faker);

        var faker2 = new InboxMessageFaker();
        faker2.AsFailed().ShouldBeSameAs(faker2);

        var faker3 = new InboxMessageFaker();
        faker3.AsExpired().ShouldBeSameAs(faker3);

        var faker4 = new InboxMessageFaker();
        faker4.WithMessageId("x").ShouldBeSameAs(faker4);

        var faker5 = new InboxMessageFaker();
        faker5.WithRequestType("y").ShouldBeSameAs(faker5);
    }

    #endregion

    #region Locale

    [Fact]
    public void Constructor_WithLocale_ShouldUseLocale()
    {
        var faker = new InboxMessageFaker("de");
        faker.Locale.ShouldBe("de");
    }

    #endregion
}
