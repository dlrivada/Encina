using Encina.Hangfire;
using Encina.Hangfire.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Hangfire;

/// <summary>
/// Guard tests for Encina.Hangfire covering constructor and method null guards.
/// </summary>
[Trait("Category", "Guard")]
public sealed class HangfireGuardTests
{
    // ─── HangfireNotificationJobAdapter constructor guards ───

    [Fact]
    public void NotificationAdapter_NullEncina_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new HangfireNotificationJobAdapter<TestNotification>(null!,
                NullLogger<HangfireNotificationJobAdapter<TestNotification>>.Instance));
    }

    [Fact]
    public void NotificationAdapter_NullLogger_Throws()
    {
        var encina = Substitute.For<IEncina>();
        Should.Throw<ArgumentNullException>(() =>
            new HangfireNotificationJobAdapter<TestNotification>(encina, null!));
    }

    [Fact]
    public void NotificationAdapter_ValidArgs_Constructs()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new HangfireNotificationJobAdapter<TestNotification>(encina,
            NullLogger<HangfireNotificationJobAdapter<TestNotification>>.Instance);
        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task NotificationAdapter_PublishAsync_NullNotification_Throws()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new HangfireNotificationJobAdapter<TestNotification>(encina,
            NullLogger<HangfireNotificationJobAdapter<TestNotification>>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.PublishAsync(null!));
    }

    // ─── HangfireRequestJobAdapter constructor guards ───

    [Fact]
    public void RequestAdapter_NullEncina_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new HangfireRequestJobAdapter<TestRequest, TestResponse>(null!,
                NullLogger<HangfireRequestJobAdapter<TestRequest, TestResponse>>.Instance));
    }

    [Fact]
    public void RequestAdapter_NullLogger_Throws()
    {
        var encina = Substitute.For<IEncina>();
        Should.Throw<ArgumentNullException>(() =>
            new HangfireRequestJobAdapter<TestRequest, TestResponse>(encina, null!));
    }

    [Fact]
    public void RequestAdapter_ValidArgs_Constructs()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new HangfireRequestJobAdapter<TestRequest, TestResponse>(encina,
            NullLogger<HangfireRequestJobAdapter<TestRequest, TestResponse>>.Instance);
        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task RequestAdapter_ExecuteAsync_NullRequest_Throws()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new HangfireRequestJobAdapter<TestRequest, TestResponse>(encina,
            NullLogger<HangfireRequestJobAdapter<TestRequest, TestResponse>>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.ExecuteAsync(null!));
    }

    // ─── HangfireHealthCheck ───

    [Fact]
    public void HangfireHealthCheck_Constructs()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var sut = new HangfireHealthCheck(sp, null);
        sut.ShouldNotBeNull();
    }

    // ─── EncinaHangfireOptions ───

    [Fact]
    public void EncinaHangfireOptions_Defaults()
    {
        var options = new EncinaHangfireOptions();
        options.ShouldNotBeNull();
    }

    // ─── Test types ───

    public sealed record TestNotification : INotification;
    public sealed record TestRequest : IRequest<TestResponse>;
    public sealed record TestResponse;
}
