using Encina.Messaging.Outbox;
using Encina.Testing.Bogus;

namespace Encina.UnitTests.Testing.Bogus;

/// <summary>
/// Unit tests for <see cref="OutboxMessageFaker"/> and <see cref="FakeOutboxMessage"/>.
/// </summary>
public sealed class OutboxMessageFakerTests
{
    #region Default Generation

    [Fact]
    public void Generate_Default_ShouldCreatePendingMessage()
    {
        var faker = new OutboxMessageFaker();
        var msg = faker.Generate();

        msg.ShouldNotBeNull();
        msg.Id.ShouldNotBe(Guid.Empty);
        msg.NotificationType.ShouldNotBeNullOrEmpty();
        msg.Content.ShouldNotBeNullOrEmpty();
        msg.CreatedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        msg.ProcessedAtUtc.ShouldBeNull();
        msg.ErrorMessage.ShouldBeNull();
        msg.RetryCount.ShouldBe(0);
        msg.NextRetryAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Generate_Default_IsNotProcessed()
    {
        var msg = new OutboxMessageFaker().Generate();
        msg.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public void Generate_Default_IsNotDeadLettered()
    {
        var msg = new OutboxMessageFaker().Generate();
        msg.IsDeadLettered(3).ShouldBeFalse();
    }

    [Fact]
    public void Generate_Reproducible_WithSameSeed()
    {
        var msg1 = new OutboxMessageFaker().Generate();
        var msg2 = new OutboxMessageFaker().Generate();
        msg1.Id.ShouldBe(msg2.Id);
        msg1.NotificationType.ShouldBe(msg2.NotificationType);
    }

    [Fact]
    public void GenerateMessage_ReturnsIOutboxMessage()
    {
        var faker = new OutboxMessageFaker();
        var msg = faker.GenerateMessage();
        msg.ShouldBeAssignableTo<IOutboxMessage>();
    }

    #endregion

    #region AsProcessed

    [Fact]
    public void AsProcessed_ShouldSetProcessedAtUtc()
    {
        var msg = new OutboxMessageFaker().AsProcessed().Generate();
        msg.ProcessedAtUtc.ShouldNotBeNull();
        msg.IsProcessed.ShouldBeTrue();
    }

    #endregion

    #region AsFailed

    [Fact]
    public void AsFailed_ShouldSetErrorAndRetry()
    {
        var msg = new OutboxMessageFaker().AsFailed(5).Generate();
        msg.ErrorMessage.ShouldNotBeNullOrEmpty();
        msg.RetryCount.ShouldBe(5);
        msg.NextRetryAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void AsFailed_DefaultRetryCount_Is3()
    {
        var msg = new OutboxMessageFaker().AsFailed().Generate();
        msg.RetryCount.ShouldBe(3);
    }

    [Fact]
    public void AsFailed_IsDeadLettered_WhenRetryCountEqualsMax()
    {
        var msg = new OutboxMessageFaker().AsFailed(3).Generate();
        msg.IsDeadLettered(3).ShouldBeTrue();
    }

    #endregion

    #region WithNotificationType / WithContent

    [Fact]
    public void WithNotificationType_ShouldOverride()
    {
        var msg = new OutboxMessageFaker().WithNotificationType("MyEvent").Generate();
        msg.NotificationType.ShouldBe("MyEvent");
    }

    [Fact]
    public void WithNotificationType_Null_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new OutboxMessageFaker().WithNotificationType(null!));
    }

    [Fact]
    public void WithNotificationType_Empty_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => new OutboxMessageFaker().WithNotificationType("  "));
    }

    [Fact]
    public void WithContent_ShouldOverride()
    {
        var msg = new OutboxMessageFaker().WithContent("{\"test\":1}").Generate();
        msg.Content.ShouldBe("{\"test\":1}");
    }

    [Fact]
    public void WithContent_Null_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new OutboxMessageFaker().WithContent(null!));
    }

    [Fact]
    public void WithContent_Whitespace_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => new OutboxMessageFaker().WithContent("  "));
    }

    #endregion

    #region Method Chaining

    [Fact]
    public void Methods_ReturnSameInstance_ForChaining()
    {
        var faker = new OutboxMessageFaker();
        faker.AsProcessed().ShouldBeSameAs(faker);

        var faker2 = new OutboxMessageFaker();
        faker2.AsFailed().ShouldBeSameAs(faker2);

        var faker3 = new OutboxMessageFaker();
        faker3.WithNotificationType("X").ShouldBeSameAs(faker3);

        var faker4 = new OutboxMessageFaker();
        faker4.WithContent("{}").ShouldBeSameAs(faker4);
    }

    #endregion

    #region Locale

    [Fact]
    public void Constructor_WithLocale_ShouldUseLocale()
    {
        var faker = new OutboxMessageFaker("es");
        faker.Locale.ShouldBe("es");
    }

    #endregion

    #region GenerateBetween

    [Fact]
    public void GenerateBetween_ShouldCreateMultipleMessages()
    {
        var faker = new OutboxMessageFaker();
        var messages = faker.GenerateBetween(3, 5);
        messages.Count.ShouldBeInRange(3, 5);
        messages.ShouldAllBe(m => m.Id != Guid.Empty);
    }

    #endregion
}
