using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Sharding;
using Encina.UnitTests.Support;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;

using NSubstitute;

namespace Encina.UnitTests.Cdc.Sharding;

/// <summary>
/// Unit tests for the failure handling of <see cref="ShardedCdcProcessor"/>: a failing cycle is retried with
/// back-off, then reported once the retries are exhausted, and every exception is logged redacted (#1557).
/// </summary>
[Trait("Category", "Unit")]
public sealed class ShardedCdcProcessorFailureTests
{
    private const string Sentinel = "SENTINEL-CDC-CONNECTION-8f1";

    [Fact]
    public async Task ExecuteAsync_CycleKeepsFailing_RetriesThenReportsExhaustionRedacted()
    {
        var logger = new FakeLogger<ShardedCdcProcessor>();
        var options = new CdcOptions
        {
            Enabled = true,
            PollingInterval = TimeSpan.FromMilliseconds(1),
            BaseRetryDelay = TimeSpan.FromMilliseconds(1),
            MaxRetries = 1,
            BatchSize = 10,
        };

        // Cycle 1 fails (retry), cycle 2 fails (retries exhausted), cycle 3 is the signal that the
        // exhaustion path ran and the loop went on.
        var calls = 0;
        var thirdCycle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var connector = Substitute.For<IShardedCdcConnector>();
        connector.GetConnectorId().Returns("test-connector");
        connector.StreamAllShardsAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (Interlocked.Increment(ref calls) >= 3)
            {
                thirdCycle.TrySetResult();
            }

            throw new InvalidOperationException(Sentinel);
        });

        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider.GetService(typeof(IShardedCdcConnector)).Returns(connector);
        scopedProvider.GetService(typeof(ICdcDispatcher)).Returns(Substitute.For<ICdcDispatcher>());
        scopedProvider.GetService(typeof(IShardedCdcPositionStore)).Returns(Substitute.For<IShardedCdcPositionStore>());
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(scopedProvider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        var rootProvider = Substitute.For<IServiceProvider>();
        rootProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var processor = new ShardedCdcProcessor(rootProvider, logger, options);
        using var cts = new CancellationTokenSource();

        await processor.StartAsync(cts.Token);
        try
        {
            await thirdCycle.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await cts.CancelAsync();
            await processor.StopAsync(CancellationToken.None);
        }

        logger.Collector.GetSnapshot().Count(r => r.Exception is not null).ShouldBeGreaterThanOrEqualTo(2);
        RedactedExceptionLogAssert.LoggedOnlyRedacted(logger, Sentinel);
    }
}
