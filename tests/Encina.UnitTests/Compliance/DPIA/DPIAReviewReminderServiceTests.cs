#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.ReadModels;

using Shouldly;

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
        var service = Substitute.For<IDPIAService>();
        var scopeFactory = CreateScopeFactory(service);

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

        await service.DidNotReceive().GetExpiredAssessmentsAsync(
            Arg.Any<CancellationToken>());
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

        var service = Substitute.For<IDPIAService>();
        service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    Array.Empty<DPIAReadModel>() as IReadOnlyList<DPIAReadModel>)));

        var scopeFactory = CreateScopeFactory(service);

        var sut = new DPIAReviewReminderService(
            scopeFactory,
            Options.Create(options),
            new NullLogger<DPIAReviewReminderService>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await sut.StartAsync(cts.Token);
        // Allow enough time for the background service to complete at least one cycle
        // CI environments can be slow, so use a generous delay
        await Task.Delay(1500, CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        await service.Received().GetExpiredAssessmentsAsync(
            Arg.Any<CancellationToken>());
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

        var service = Substitute.For<IDPIAService>();
        service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DPIAReadModel>>>(
                EncinaError.New("Store error")));

        var scopeFactory = CreateScopeFactory(service);

        var sut = new DPIAReviewReminderService(
            scopeFactory,
            Options.Create(options),
            new NullLogger<DPIAReviewReminderService>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await sut.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);

        var act = () => sut.StopAsync(CancellationToken.None);
        await Should.NotThrowAsync(act);
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

        var service = Substitute.For<IDPIAService>();
        service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<DPIAReadModel>>>>(_ =>
                throw new InvalidOperationException("Connection failed"));

        var scopeFactory = CreateScopeFactory(service);

        var sut = new DPIAReviewReminderService(
            scopeFactory,
            Options.Create(options),
            new NullLogger<DPIAReviewReminderService>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await sut.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);

        var act = () => sut.StopAsync(CancellationToken.None);
        await Should.NotThrowAsync(act);
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

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("scopeFactory");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        var act = () => new DPIAReviewReminderService(
            scopeFactory,
            null!,
            new NullLogger<DPIAReviewReminderService>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        var act = () => new DPIAReviewReminderService(
            scopeFactory,
            Options.Create(new DPIAOptions()),
            null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    #endregion

    #region Helpers

    private static IServiceScopeFactory CreateScopeFactory(IDPIAService service)
    {
        var services = new ServiceCollection();
        services.AddSingleton(service);
        services.AddSingleton(TimeProvider.System);

        var provider = services.BuildServiceProvider();

        // Use the real scope factory from the built ServiceProvider
        // so CreateAsyncScope() works correctly
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    #endregion
}
