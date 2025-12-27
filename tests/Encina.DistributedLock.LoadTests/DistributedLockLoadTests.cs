using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.DistributedLock.LoadTests;

/// <summary>
/// Load tests for distributed lock providers.
/// </summary>
[Trait("Category", "Load")]
public class DistributedLockLoadTests
{
    private readonly InMemoryDistributedLockProvider _provider;

    public DistributedLockLoadTests()
    {
        _provider = new InMemoryDistributedLockProvider(
            Options.Create(new InMemoryLockOptions()),
            NullLogger<InMemoryDistributedLockProvider>.Instance);
    }

    [Fact]
    public async Task HighConcurrency_1000Locks_AllSucceed()
    {
        // Arrange
        const int numLocks = 1000;
        var resources = Enumerable.Range(0, numLocks)
            .Select(i => $"load-test-{Guid.NewGuid()}-{i}")
            .ToList();

        // Act
        var stopwatch = Stopwatch.StartNew();

        var tasks = resources.Select(resource => _provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None))
            .ToList();

        var results = await Task.WhenAll(tasks);

        stopwatch.Stop();

        // Assert
        var successCount = results.Count(r => r is not null);
        successCount.Should().Be(numLocks);

        // Performance assertion - should complete in reasonable time
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // 10 seconds max

        // Clean up
        foreach (var result in results.Where(r => r is not null))
        {
            await result!.DisposeAsync();
        }
    }

    [Fact]
    public async Task HighContention_100Concurrent_OnSameResource_OnlyOneSucceeds()
    {
        // Arrange
        const int numAttempts = 100;
        var resource = $"contention-{Guid.NewGuid()}";

        // Act
        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, numAttempts)
            .Select(_ => _provider.TryAcquireAsync(
                resource,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(10),
                CancellationToken.None))
            .ToList();

        var results = await Task.WhenAll(tasks);

        stopwatch.Stop();

        // Assert
        var successCount = results.Count(r => r is not null);
        successCount.Should().Be(1);

        // Performance assertion
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);

        // Clean up
        foreach (var result in results.Where(r => r is not null))
        {
            await result!.DisposeAsync();
        }
    }

    [Fact]
    public async Task RapidAcquireRelease_1000Iterations_AllSucceed()
    {
        // Arrange
        const int iterations = 1000;
        var resource = $"rapid-{Guid.NewGuid()}";

        // Act
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < iterations; i++)
        {
            var lockHandle = await _provider.TryAcquireAsync(
                resource,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(50),
                CancellationToken.None);

            lockHandle.Should().NotBeNull();
            await lockHandle!.DisposeAsync();
        }

        stopwatch.Stop();

        // Assert - should complete all iterations
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000);
    }

    [Fact]
    public async Task MixedOperations_IsLockedAndExtend_UnderLoad()
    {
        // Arrange
        const int numResources = 100;
        var resources = Enumerable.Range(0, numResources)
            .Select(i => $"mixed-{Guid.NewGuid()}-{i}")
            .ToList();

        // Act - Acquire all locks
        var locks = new List<IAsyncDisposable>();
        foreach (var resource in resources)
        {
            var lockHandle = await _provider.TryAcquireAsync(
                resource,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(50),
                CancellationToken.None);

            locks.Add(lockHandle!);
        }

        // Perform mixed operations concurrently
        var isLockedTasks = resources.Select(r =>
            _provider.IsLockedAsync(r, CancellationToken.None)).ToList();
        var extendTasks = resources.Select(r =>
            _provider.ExtendAsync(r, TimeSpan.FromMinutes(5), CancellationToken.None)).ToList();

        var isLockedResults = await Task.WhenAll(isLockedTasks);
        var extendResults = await Task.WhenAll(extendTasks);

        // Assert
        isLockedResults.Should().AllBeEquivalentTo(true);
        extendResults.Should().AllBeEquivalentTo(true);

        // Clean up
        foreach (var lockHandle in locks)
        {
            await lockHandle.DisposeAsync();
        }
    }
}
