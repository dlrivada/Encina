#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DPIAReviewReminderService"/>.
/// </summary>
public class DPIAReviewReminderServiceTests
{
    #region ExecuteAsync - Disabled

    [Fact]
    public async Task ExecuteAsync_MonitoringDisabled_ReturnsImmediately()
    {
        var options = new DPIAOptions { EnableExpirationMonitoring = false };
        var store = Substitute.For<IDPIAStore>();
        var scopeFactory = CreateScopeFactory(store);

        var sut = new DPIAReviewReminderService(
            scopeFactory,
            Options.Create(options),
            new NullLogger<DPIAReviewReminderService>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // StartAsync calls ExecuteAsync internally for BackgroundService
        await sut.StartAsync(cts.Token);
        // Give it a moment to execute
        await Task.Delay(100, CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        await store.DidNotReceive().GetExpiredAssessmentsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExecuteAsync - First Cycle

    [Fact]
    public async Task ExecuteAsync_MonitoringEnabled_ExecutesFirstCycleImmediately()
    {
        var options = new DPIAOptions
        {
            EnableExpirationMonitoring = true,
            ExpirationCheckInterval = TimeSpan.FromHours(1),
            PublishNotifications = false
        };

        var store = Substitute.For<IDPIAStore>();
        store.GetExpiredAssessmentsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAAssessment>>([])));

        var scopeFactory = CreateScopeFactory(store);

        var sut = new DPIAReviewReminderService(
            scopeFactory,
            Options.Create(options),
            new NullLogger<DPIAReviewReminderService>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await sut.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        await store.Received(1).GetExpiredAssessmentsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExecuteAsync - Error Handling

    [Fact]
    public async Task ExecuteAsync_StoreReturnsError_DoesNotThrow()
    {
        var options = new DPIAOptions
        {
            EnableExpirationMonitoring = true,
            ExpirationCheckInterval = TimeSpan.FromHours(1),
            PublishNotifications = false
        };

        var store = Substitute.For<IDPIAStore>();
        store.GetExpiredAssessmentsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DPIAAssessment>>>(
                EncinaError.New("Store error")));

        var scopeFactory = CreateScopeFactory(store);

        var sut = new DPIAReviewReminderService(
            scopeFactory,
            Options.Create(options),
            new NullLogger<DPIAReviewReminderService>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await sut.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);

        var act = () => sut.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_StoreThrows_DoesNotCrash()
    {
        var options = new DPIAOptions
        {
            EnableExpirationMonitoring = true,
            ExpirationCheckInterval = TimeSpan.FromHours(1),
            PublishNotifications = false
        };

        var store = Substitute.For<IDPIAStore>();
        store.GetExpiredAssessmentsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<DPIAAssessment>>>>(_ =>
                throw new InvalidOperationException("Connection failed"));

        var scopeFactory = CreateScopeFactory(store);

        var sut = new DPIAReviewReminderService(
            scopeFactory,
            Options.Create(options),
            new NullLogger<DPIAReviewReminderService>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await sut.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);

        var act = () => sut.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullScopeFactory_ThrowsArgumentNullException()
    {
        var act = () => new DPIAReviewReminderService(
            null!,
            Options.Create(new DPIAOptions()),
            new NullLogger<DPIAReviewReminderService>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("scopeFactory");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        var act = () => new DPIAReviewReminderService(
            scopeFactory,
            null!,
            new NullLogger<DPIAReviewReminderService>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        var act = () => new DPIAReviewReminderService(
            scopeFactory,
            Options.Create(new DPIAOptions()),
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Helpers

    private static IServiceScopeFactory CreateScopeFactory(IDPIAStore store)
    {
        var services = new ServiceCollection();
        services.AddSingleton(store);
        services.AddSingleton(TimeProvider.System);

        var provider = services.BuildServiceProvider();

        // Use the real scope factory from the built ServiceProvider
        // so CreateAsyncScope() works correctly
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    #endregion
}
