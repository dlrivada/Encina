#pragma warning disable CA2012 // NSubstitute ValueTask stubbing pattern

using Encina.Audit.Marten;
using Encina.Security.Audit;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="MartenOperationAuditRetentionService"/> background crypto-shredding cycle.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class MartenOperationAuditRetentionServiceTests
{
    private static ServiceProvider BuildServiceProvider(IOperationAuditStore auditStore)
    {
        var services = new ServiceCollection();
        services.AddSingleton(auditStore);
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Constructor_NullServiceProvider_Throws()
    {
        var options = Options.Create(new MartenOperationAuditOptions());
        Should.Throw<ArgumentNullException>(() => new MartenOperationAuditRetentionService(
            null!,
            options,
            TimeProvider.System,
            NullLogger<MartenOperationAuditRetentionService>.Instance));
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        Should.Throw<ArgumentNullException>(() => new MartenOperationAuditRetentionService(
            sp,
            null!,
            TimeProvider.System,
            NullLogger<MartenOperationAuditRetentionService>.Instance));
    }

    [Fact]
    public void Constructor_NullTimeProvider_Throws()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var options = Options.Create(new MartenOperationAuditOptions());
        Should.Throw<ArgumentNullException>(() => new MartenOperationAuditRetentionService(
            sp,
            options,
            null!,
            NullLogger<MartenOperationAuditRetentionService>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var options = Options.Create(new MartenOperationAuditOptions());
        Should.Throw<ArgumentNullException>(() => new MartenOperationAuditRetentionService(
            sp,
            options,
            TimeProvider.System,
            null!));
    }

    [Fact]
    public void Constructor_ValidArgs_DoesNotThrow()
    {
        var auditStore = Substitute.For<IOperationAuditStore>();
        var sp = BuildServiceProvider(auditStore);
        var options = Options.Create(new MartenOperationAuditOptions
        {
            RetentionPeriod = TimeSpan.FromDays(90),
            PurgeIntervalHours = 6
        });

        var sut = new MartenOperationAuditRetentionService(
            sp,
            options,
            TimeProvider.System,
            NullLogger<MartenOperationAuditRetentionService>.Instance);

        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task StartAsync_CancelledImmediately_DoesNotThrow()
    {
        var auditStore = Substitute.For<IOperationAuditStore>();
        auditStore.PurgeEntriesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(Right<EncinaError, int>(0)));

        var sp = BuildServiceProvider(auditStore);
        var options = Options.Create(new MartenOperationAuditOptions
        {
            RetentionPeriod = TimeSpan.FromDays(30),
            PurgeIntervalHours = 24
        });

        var sut = new MartenOperationAuditRetentionService(
            sp,
            options,
            TimeProvider.System,
            NullLogger<MartenOperationAuditRetentionService>.Instance);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Should complete quickly due to cancellation - BackgroundService wraps ExecuteAsync
        await sut.StartAsync(cts.Token);
        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_WithoutStart_DoesNotThrow()
    {
        var auditStore = Substitute.For<IOperationAuditStore>();
        var sp = BuildServiceProvider(auditStore);
        var options = Options.Create(new MartenOperationAuditOptions());

        var sut = new MartenOperationAuditRetentionService(
            sp,
            options,
            new FakeTimeProvider(),
            NullLogger<MartenOperationAuditRetentionService>.Instance);

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_ShortCancellation_StopsCleanly()
    {
        var auditStore = Substitute.For<IOperationAuditStore>();
        auditStore.PurgeEntriesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(Right<EncinaError, int>(5)));

        var sp = BuildServiceProvider(auditStore);
        var options = Options.Create(new MartenOperationAuditOptions
        {
            PurgeIntervalHours = 24,
            RetentionPeriod = TimeSpan.FromDays(365)
        });

        var sut = new MartenOperationAuditRetentionService(
            sp,
            options,
            TimeProvider.System,
            NullLogger<MartenOperationAuditRetentionService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await sut.StartAsync(cts.Token);

        await Task.Delay(100);

        await sut.StopAsync(CancellationToken.None);
    }
}
