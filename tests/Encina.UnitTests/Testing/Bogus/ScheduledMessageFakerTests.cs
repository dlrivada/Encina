using Encina.Messaging.Scheduling;
using Encina.Testing.Bogus;

namespace Encina.UnitTests.Testing.Bogus;

/// <summary>
/// Unit tests for <see cref="ScheduledMessageFaker"/> and <see cref="FakeScheduledMessage"/>.
/// </summary>
public sealed class ScheduledMessageFakerTests
{
    #region Default Generation

    [Fact]
    public void Generate_Default_ShouldCreatePendingOneTimeMessage()
    {
        var msg = new ScheduledMessageFaker().Generate();

        msg.ShouldNotBeNull();
        msg.Id.ShouldNotBe(Guid.Empty);
        msg.RequestType.ShouldNotBeNullOrEmpty();
        msg.Content.ShouldNotBeNullOrEmpty();
        msg.ScheduledAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        msg.CreatedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        msg.ProcessedAtUtc.ShouldBeNull();
        msg.ErrorMessage.ShouldBeNull();
        msg.RetryCount.ShouldBe(0);
        msg.IsRecurring.ShouldBeFalse();
        msg.CronExpression.ShouldBeNull();
        msg.LastExecutedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Generate_Default_IsNotProcessed()
    {
        var msg = new ScheduledMessageFaker().Generate();
        msg.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public void GenerateMessage_ReturnsIScheduledMessage()
    {
        var msg = new ScheduledMessageFaker().GenerateMessage();
        msg.ShouldBeAssignableTo<IScheduledMessage>();
    }

    [Fact]
    public void Generate_Reproducible_WithSameSeed()
    {
        var msg1 = new ScheduledMessageFaker().Generate();
        var msg2 = new ScheduledMessageFaker().Generate();
        msg1.Id.ShouldBe(msg2.Id);
    }

    #endregion

    #region AsProcessed

    [Fact]
    public void AsProcessed_ShouldSetProcessedAtUtc()
    {
        var msg = new ScheduledMessageFaker().AsProcessed().Generate();
        msg.ProcessedAtUtc.ShouldNotBeNull();
        msg.IsProcessed.ShouldBeTrue();
    }

    #endregion

    #region AsFailed

    [Fact]
    public void AsFailed_ShouldSetErrorAndRetry()
    {
        var msg = new ScheduledMessageFaker().AsFailed(3).Generate();
        msg.ErrorMessage.ShouldNotBeNullOrEmpty();
        msg.RetryCount.ShouldBe(3);
        msg.NextRetryAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void AsFailed_DefaultRetryCount_Is2()
    {
        var msg = new ScheduledMessageFaker().AsFailed().Generate();
        msg.RetryCount.ShouldBe(2);
    }

    [Fact]
    public void AsFailed_IsDeadLettered()
    {
        var msg = new ScheduledMessageFaker().AsFailed(5).Generate();
        msg.IsDeadLettered(5).ShouldBeTrue();
        msg.IsDeadLettered(6).ShouldBeFalse();
    }

    #endregion

    #region AsDue

    [Fact]
    public void AsDue_ShouldScheduleInPast()
    {
        var msg = new ScheduledMessageFaker().AsDue().Generate();
        msg.ScheduledAtUtc.ShouldBeLessThan(DateTime.UtcNow);
    }

    [Fact]
    public void AsDue_IsDue_ReturnsTrue()
    {
        var msg = new ScheduledMessageFaker().AsDue().Generate();
        msg.IsDue(DateTime.UtcNow).ShouldBeTrue();
    }

    #endregion

    #region AsRecurring

    [Fact]
    public void AsRecurring_ShouldSetRecurringAndCron()
    {
        var msg = new ScheduledMessageFaker().AsRecurring().Generate();
        msg.IsRecurring.ShouldBeTrue();
        msg.CronExpression.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void AsRecurring_WithCustomCron_ShouldUseIt()
    {
        var msg = new ScheduledMessageFaker().AsRecurring("0 0 * * *").Generate();
        msg.CronExpression.ShouldBe("0 0 * * *");
    }

    [Fact]
    public void AsRecurring_IsProcessed_IsFalse_EvenWhenProcessedAtSet()
    {
        // Recurring messages are never "processed" even with ProcessedAtUtc set
        var msg = new ScheduledMessageFaker().AsRecurring().AsProcessed().Generate();
        msg.ProcessedAtUtc.ShouldNotBeNull();
        msg.IsRecurring.ShouldBeTrue();
        msg.IsProcessed.ShouldBeFalse(); // IsProcessed = ProcessedAtUtc.HasValue && !IsRecurring
    }

    #endregion

    #region AsRecurringExecuted

    [Fact]
    public void AsRecurringExecuted_ShouldSetLastExecutedAndCron()
    {
        var msg = new ScheduledMessageFaker().AsRecurringExecuted().Generate();
        msg.IsRecurring.ShouldBeTrue();
        msg.CronExpression.ShouldNotBeNullOrEmpty();
        msg.LastExecutedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void AsRecurringExecuted_WithCustomCron()
    {
        var msg = new ScheduledMessageFaker().AsRecurringExecuted("*/5 * * * *").Generate();
        msg.CronExpression.ShouldBe("*/5 * * * *");
    }

    #endregion

    #region WithRequestType / WithContent / ScheduledAt

    [Fact]
    public void WithRequestType_ShouldOverride()
    {
        var msg = new ScheduledMessageFaker().WithRequestType("SendReminder").Generate();
        msg.RequestType.ShouldBe("SendReminder");
    }

    [Fact]
    public void WithRequestType_Null_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new ScheduledMessageFaker().WithRequestType(null!));
    }

    [Fact]
    public void WithRequestType_Whitespace_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => new ScheduledMessageFaker().WithRequestType("  "));
    }

    [Fact]
    public void WithContent_ShouldOverride()
    {
        var msg = new ScheduledMessageFaker().WithContent("{\"remind\":true}").Generate();
        msg.Content.ShouldBe("{\"remind\":true}");
    }

    [Fact]
    public void WithContent_Null_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new ScheduledMessageFaker().WithContent(null!));
    }

    [Fact]
    public void ScheduledAt_UtcTime_ShouldUseAsIs()
    {
        var time = new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var msg = new ScheduledMessageFaker().ScheduledAt(time).Generate();
        msg.ScheduledAtUtc.ShouldBe(time);
    }

    [Fact]
    public void ScheduledAt_LocalTime_ShouldConvertToUtc()
    {
        var localTime = new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Local);
        var msg = new ScheduledMessageFaker().ScheduledAt(localTime).Generate();
        msg.ScheduledAtUtc.ShouldBe(localTime.ToUniversalTime());
    }

    #endregion

    #region IsDue Overload

    [Fact]
    public void IsDue_WithAsOf_WhenBeforeScheduled_ReturnsFalse()
    {
        var future = DateTime.UtcNow.AddDays(30);
        var msg = new ScheduledMessageFaker().ScheduledAt(future).Generate();
        msg.IsDue(DateTime.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void IsDue_WithAsOf_WhenAfterScheduled_ReturnsTrue()
    {
        var past = DateTime.UtcNow.AddDays(-1);
        var msg = new ScheduledMessageFaker().ScheduledAt(past).Generate();
        msg.IsDue(DateTime.UtcNow).ShouldBeTrue();
    }

    #endregion

    #region Method Chaining

    [Fact]
    public void Methods_ReturnSameInstance_ForChaining()
    {
        var f1 = new ScheduledMessageFaker();
        f1.AsProcessed().ShouldBeSameAs(f1);

        var f2 = new ScheduledMessageFaker();
        f2.AsFailed().ShouldBeSameAs(f2);

        var f3 = new ScheduledMessageFaker();
        f3.AsDue().ShouldBeSameAs(f3);

        var f4 = new ScheduledMessageFaker();
        f4.AsRecurring().ShouldBeSameAs(f4);

        var f5 = new ScheduledMessageFaker();
        f5.WithRequestType("X").ShouldBeSameAs(f5);

        var f6 = new ScheduledMessageFaker();
        f6.WithContent("{}").ShouldBeSameAs(f6);

        var f7 = new ScheduledMessageFaker();
        f7.ScheduledAt(DateTime.UtcNow).ShouldBeSameAs(f7);
    }

    #endregion

    #region Locale

    [Fact]
    public void Constructor_WithLocale_ShouldUseLocale()
    {
        var faker = new ScheduledMessageFaker("fr");
        faker.Locale.ShouldBe("fr");
    }

    #endregion
}
