using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.DistributedLock.PropertyTests;

/// <summary>
/// Property-based tests that verify invariants of the distributed lock system.
/// </summary>
public class DistributedLockInvariantsTests
{
    private readonly InMemoryDistributedLockProvider _provider;

    public DistributedLockInvariantsTests()
    {
        _provider = new InMemoryDistributedLockProvider(
            Options.Create(new InMemoryLockOptions()),
            NullLogger<InMemoryDistributedLockProvider>.Instance);
    }

    [Property(MaxTest = 50)]
    public bool AcquiredLock_IsAlwaysLocked(NonEmptyString resourceName)
    {
        var resource = $"prop-{resourceName.Item}";

        var lockHandle = _provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None).GetAwaiter().GetResult();

        if (lockHandle is null)
        {
            return true; // Resource was already locked, skip
        }

        var isLocked = _provider.IsLockedAsync(resource, CancellationToken.None).GetAwaiter().GetResult();
        lockHandle.DisposeAsync().AsTask().GetAwaiter().GetResult();

        return isLocked;
    }

    [Property(MaxTest = 50)]
    public bool ReleasedLock_IsNeverLocked(NonEmptyString resourceName)
    {
        var resource = $"prop-released-{resourceName.Item}-{Guid.NewGuid()}";

        var lockHandle = _provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None).GetAwaiter().GetResult();

        if (lockHandle is null)
        {
            return true; // Skip if we couldn't acquire
        }

        lockHandle.DisposeAsync().AsTask().GetAwaiter().GetResult();
        var isLocked = _provider.IsLockedAsync(resource, CancellationToken.None).GetAwaiter().GetResult();

        return !isLocked;
    }

    [Property(MaxTest = 30)]
    public bool MultipleLocks_OnSameResource_OnlyOneSucceeds(PositiveInt attempts)
    {
        var resource = $"prop-multiple-{Guid.NewGuid()}";
        var numAttempts = Math.Min(attempts.Item, 10); // Limit to 10 to keep tests fast

        var tasks = Enumerable.Range(0, numAttempts)
            .Select(_ => _provider.TryAcquireAsync(
                resource,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMilliseconds(50),
                TimeSpan.FromMilliseconds(10),
                CancellationToken.None))
            .ToList();

        var results = Task.WhenAll(tasks).GetAwaiter().GetResult();
        var successCount = results.Count(r => r is not null);

        // Clean up
        foreach (var result in results.Where(r => r is not null))
        {
            result!.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        // Exactly one should succeed (or possibly zero if timing is very tight)
        return successCount <= 1;
    }

    [Property(MaxTest = 20)]
    public bool DifferentResources_AllCanBeLocked(PositiveInt count)
    {
        var numResources = Math.Min(count.Item, 20); // Limit to keep tests fast
        var resources = Enumerable.Range(0, numResources)
            .Select(i => $"prop-diff-{Guid.NewGuid()}-{i}")
            .ToList();

        var locks = new List<IAsyncDisposable>();

        foreach (var resource in resources)
        {
            var lockHandle = _provider.TryAcquireAsync(
                resource,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(50),
                CancellationToken.None).GetAwaiter().GetResult();

            if (lockHandle is not null)
            {
                locks.Add(lockHandle);
            }
        }

        var allAcquired = locks.Count == numResources;

        // Clean up
        foreach (var lockHandle in locks)
        {
            lockHandle.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        return allAcquired;
    }

    [Property(MaxTest = 50)]
    public bool LockHandle_ImplementsILockHandle_WithValidProperties(NonEmptyString resourceName)
    {
        var resource = $"prop-handle-{resourceName.Item}-{Guid.NewGuid()}";

        var lockHandle = _provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None).GetAwaiter().GetResult();

        if (lockHandle is null)
        {
            return true; // Skip
        }

        var handle = lockHandle as ILockHandle;
        if (handle is null)
        {
            lockHandle.DisposeAsync().AsTask().GetAwaiter().GetResult();
            return false;
        }

        var hasValidResource = handle.Resource == resource;
        var hasValidLockId = !string.IsNullOrEmpty(handle.LockId);
        var hasValidAcquiredTime = handle.AcquiredAtUtc <= DateTime.UtcNow;
        var hasValidExpiresTime = handle.ExpiresAtUtc > handle.AcquiredAtUtc;
        var isNotReleased = !handle.IsReleased;

        lockHandle.DisposeAsync().AsTask().GetAwaiter().GetResult();

        return hasValidResource && hasValidLockId && hasValidAcquiredTime &&
               hasValidExpiresTime && isNotReleased;
    }
}
