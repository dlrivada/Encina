using Encina.Messaging.Recoverability;
using Encina.Messaging.Serialization;
using Shouldly;

namespace Encina.GuardTests.Messaging.Recoverability;

/// <summary>
/// Guard clause tests for <see cref="DelayedRetryScheduler"/>.
/// </summary>
public class DelayedRetrySchedulerGuardTests
{
    private static DelayedRetryScheduler CreateSut() => new(
        Substitute.For<IDelayedRetryStore>(),
        Substitute.For<IDelayedRetryMessageFactory>(),
        NullLogger<DelayedRetryScheduler>.Instance,
        Substitute.For<IMessageSerializer>());

    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new DelayedRetryScheduler(
            null!,
            Substitute.For<IDelayedRetryMessageFactory>(),
            NullLogger<DelayedRetryScheduler>.Instance,
            Substitute.For<IMessageSerializer>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("store");
    }

    [Fact]
    public void Constructor_NullMessageFactory_ThrowsArgumentNullException()
    {
        var act = () => new DelayedRetryScheduler(
            Substitute.For<IDelayedRetryStore>(),
            null!,
            NullLogger<DelayedRetryScheduler>.Instance,
            Substitute.For<IMessageSerializer>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("messageFactory");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DelayedRetryScheduler(
            Substitute.For<IDelayedRetryStore>(),
            Substitute.For<IDelayedRetryMessageFactory>(),
            null!,
            Substitute.For<IMessageSerializer>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullMessageSerializer_ThrowsArgumentNullException()
    {
        var act = () => new DelayedRetryScheduler(
            Substitute.For<IDelayedRetryStore>(),
            Substitute.For<IDelayedRetryMessageFactory>(),
            NullLogger<DelayedRetryScheduler>.Instance,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("messageSerializer");
    }

    [Fact]
    public void Constructor_NullTimeProvider_CreatesInstance()
    {
        var sut = new DelayedRetryScheduler(
            Substitute.For<IDelayedRetryStore>(),
            Substitute.For<IDelayedRetryMessageFactory>(),
            NullLogger<DelayedRetryScheduler>.Instance,
            Substitute.For<IMessageSerializer>(),
            timeProvider: null);

        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task ScheduleRetryAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var act = async () => await sut.ScheduleRetryAsync<object>(
            null!, new RecoverabilityContext(), TimeSpan.FromSeconds(1), 0);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task ScheduleRetryAsync_NullContext_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var act = async () => await sut.ScheduleRetryAsync(
            new object(), null!, TimeSpan.FromSeconds(1), 0);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
    }
}
