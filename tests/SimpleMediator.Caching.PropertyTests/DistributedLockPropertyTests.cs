using FsCheck;
using FsCheck.Xunit;

namespace SimpleMediator.Caching.PropertyTests;

/// <summary>
/// Property-based tests for IDistributedLockProvider that verify invariants hold for all inputs.
/// </summary>
public sealed class DistributedLockPropertyTests
{
    private readonly MemoryDistributedLockProvider _provider;

    public DistributedLockPropertyTests()
    {
        _provider = new MemoryDistributedLockProvider(NullLogger<MemoryDistributedLockProvider>.Instance);
    }

    #region Mutual Exclusion Invariants

    [Property(MaxTest = 50)]
    public bool OnlyOneAcquireSucceedsForSameResource(PositiveInt attemptsSeed)
    {
        var resource = $"mutex-test-{Guid.NewGuid():N}";
        var attemptCount = Math.Min(attemptsSeed.Get % 20 + 1, 20);
        var successCount = 0;
        var handles = new List<IAsyncDisposable?>();

        for (var i = 0; i < attemptCount; i++)
        {
            var handle = _provider.TryAcquireAsync(
                resource,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMilliseconds(50),
                TimeSpan.FromMilliseconds(10),
                CancellationToken.None).GetAwaiter().GetResult();

            if (handle != null)
            {
                successCount++;
            }
            handles.Add(handle);
        }

        // Cleanup
        foreach (var handle in handles.Where(h => h != null))
        {
            handle!.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        return successCount == 1;
    }

    [Property(MaxTest = 50)]
    public bool LockCanBeReacquiredAfterRelease(PositiveInt iterationsSeed)
    {
        var resource = $"reacquire-{Guid.NewGuid():N}";
        var count = Math.Min(iterationsSeed.Get % 10 + 1, 10);

        for (var i = 0; i < count; i++)
        {
            var handle = _provider.AcquireAsync(
                resource,
                TimeSpan.FromMinutes(5),
                CancellationToken.None).GetAwaiter().GetResult();

            handle.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        return true;
    }

    #endregion

    #region IsLocked Invariants

    [Property(MaxTest = 100)]
    public bool IsLocked_TrueWhileHeld_FalseWhenReleased(PositiveInt resourceSeed)
    {
        var resource = $"islocked-{Guid.NewGuid():N}-{resourceSeed.Get}";

        var handle = _provider.AcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None).GetAwaiter().GetResult();

        var lockedWhileHeld = _provider.IsLockedAsync(resource, CancellationToken.None)
            .GetAwaiter().GetResult();

        handle.DisposeAsync().AsTask().GetAwaiter().GetResult();

        var lockedAfterRelease = _provider.IsLockedAsync(resource, CancellationToken.None)
            .GetAwaiter().GetResult();

        return lockedWhileHeld && !lockedAfterRelease;
    }

    [Property(MaxTest = 100)]
    public bool IsLocked_FalseForNonExistentResource(PositiveInt resourceSeed)
    {
        var resource = $"nonexistent-{Guid.NewGuid():N}-{resourceSeed.Get}";

        var isLocked = _provider.IsLockedAsync(resource, CancellationToken.None)
            .GetAwaiter().GetResult();

        return !isLocked;
    }

    #endregion

    #region Extend Invariants

    [Property(MaxTest = 100)]
    public bool Extend_SucceedsOnlyWhenHeld(PositiveInt resourceSeed)
    {
        var resource = $"extend-{Guid.NewGuid():N}-{resourceSeed.Get}";

        // Should fail when not held
        var extendedBeforeAcquire = _provider.ExtendAsync(resource, TimeSpan.FromSeconds(30), CancellationToken.None)
            .GetAwaiter().GetResult();

        var handle = _provider.AcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None).GetAwaiter().GetResult();

        // Should succeed when held
        var extendedWhileHeld = _provider.ExtendAsync(resource, TimeSpan.FromSeconds(30), CancellationToken.None)
            .GetAwaiter().GetResult();

        handle.DisposeAsync().AsTask().GetAwaiter().GetResult();

        // Should fail after release
        var extendedAfterRelease = _provider.ExtendAsync(resource, TimeSpan.FromSeconds(30), CancellationToken.None)
            .GetAwaiter().GetResult();

        return !extendedBeforeAcquire && extendedWhileHeld && !extendedAfterRelease;
    }

    #endregion

    #region Different Resources are Independent

    [Property(MaxTest = 50)]
    public bool DifferentResourcesAreIndependent(PositiveInt seed1, PositiveInt seed2)
    {
        var resource1 = $"independent-{Guid.NewGuid():N}-a";
        var resource2 = $"independent-{Guid.NewGuid():N}-b";

        var handle1 = _provider.AcquireAsync(
            resource1,
            TimeSpan.FromMinutes(5),
            CancellationToken.None).GetAwaiter().GetResult();

        // Should be able to acquire different resource
        var handle2 = _provider.TryAcquireAsync(
            resource2,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(10),
            CancellationToken.None).GetAwaiter().GetResult();

        var bothAcquired = handle2 != null;

        // Cleanup
        handle1.DisposeAsync().AsTask().GetAwaiter().GetResult();
        handle2?.DisposeAsync().AsTask().GetAwaiter().GetResult();

        return bothAcquired;
    }

    #endregion
}
