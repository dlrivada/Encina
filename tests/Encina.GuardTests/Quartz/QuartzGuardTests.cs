using Encina.Quartz;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Quartz;

using Shouldly;

namespace Encina.GuardTests.Quartz;

/// <summary>
/// Guard tests for Encina.Quartz covering constructor and method null guards.
/// </summary>
[Trait("Category", "Guard")]
public sealed class QuartzGuardTests
{
    // ─── QuartzNotificationJob constructor guards ───

    [Fact]
    public void NotificationJob_NullEncina_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new QuartzNotificationJob<TestNotification>(null!,
                NullLogger<QuartzNotificationJob<TestNotification>>.Instance));
    }

    [Fact]
    public void NotificationJob_NullLogger_Throws()
    {
        var encina = Substitute.For<IEncina>();
        Should.Throw<ArgumentNullException>(() =>
            new QuartzNotificationJob<TestNotification>(encina, null!));
    }

    [Fact]
    public void NotificationJob_ValidArgs_Constructs()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new QuartzNotificationJob<TestNotification>(encina,
            NullLogger<QuartzNotificationJob<TestNotification>>.Instance);
        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task NotificationJob_Execute_NullContext_Throws()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new QuartzNotificationJob<TestNotification>(encina,
            NullLogger<QuartzNotificationJob<TestNotification>>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.Execute(null!));
    }

    // ─── QuartzRequestJob constructor guards ───

    [Fact]
    public void RequestJob_NullEncina_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new QuartzRequestJob<TestRequest, TestResponse>(null!,
                NullLogger<QuartzRequestJob<TestRequest, TestResponse>>.Instance));
    }

    [Fact]
    public void RequestJob_NullLogger_Throws()
    {
        var encina = Substitute.For<IEncina>();
        Should.Throw<ArgumentNullException>(() =>
            new QuartzRequestJob<TestRequest, TestResponse>(encina, null!));
    }

    [Fact]
    public void RequestJob_ValidArgs_Constructs()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new QuartzRequestJob<TestRequest, TestResponse>(encina,
            NullLogger<QuartzRequestJob<TestRequest, TestResponse>>.Instance);
        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task RequestJob_Execute_NullContext_Throws()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new QuartzRequestJob<TestRequest, TestResponse>(encina,
            NullLogger<QuartzRequestJob<TestRequest, TestResponse>>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.Execute(null!));
    }

    // ─── EncinaQuartzOptions ───

    [Fact]
    public void EncinaQuartzOptions_Defaults()
    {
        var options = new EncinaQuartzOptions();
        options.ShouldNotBeNull();
    }

    public sealed record TestNotification : INotification;
    public sealed record TestRequest : IRequest<TestResponse>;
    public sealed record TestResponse;
}
