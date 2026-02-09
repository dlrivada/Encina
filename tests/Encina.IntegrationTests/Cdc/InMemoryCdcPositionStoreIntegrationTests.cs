using Encina.Cdc.Abstractions;
using Encina.Cdc.Processing;
using Encina.IntegrationTests.Cdc.Helpers;
using Shouldly;

namespace Encina.IntegrationTests.Cdc;

/// <summary>
/// Integration tests for <see cref="InMemoryCdcPositionStore"/> verifying thread safety
/// and behavior under real concurrent access patterns.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "CDC")]
public sealed class InMemoryCdcPositionStoreIntegrationTests
{
    #region Concurrent Access

    [Fact]
    public async Task ConcurrentSaves_DifferentConnectors_AllSucceed()
    {
        // Arrange
        var store = new InMemoryCdcPositionStore();
        var tasks = new List<Task>();

        // Act — 50 concurrent saves to different connectors
        for (var i = 0; i < 50; i++)
        {
            var connectorId = $"connector-{i}";
            var position = new TestCdcPosition(i);
            tasks.Add(store.SavePositionAsync(connectorId, position));
        }

        await Task.WhenAll(tasks);

        // Assert — All 50 positions should be saved
        for (var i = 0; i < 50; i++)
        {
            var result = await store.GetPositionAsync($"connector-{i}");
            result.IsRight.ShouldBeTrue();
            var option = result.Match(Right: o => o, Left: _ => default);
            option.IsSome.ShouldBeTrue();
            option.IfSome(p =>
            {
                var testPos = p.ShouldBeOfType<TestCdcPosition>();
                testPos.Value.ShouldBe(i);
            });
        }
    }

    [Fact]
    public async Task ConcurrentSaves_SameConnector_LastWriteWins()
    {
        // Arrange
        var store = new InMemoryCdcPositionStore();
        var tasks = new List<Task>();
        var connectorId = "shared-connector";

        // Act — 100 concurrent saves to the same connector
        for (var i = 0; i < 100; i++)
        {
            var position = new TestCdcPosition(i);
            tasks.Add(store.SavePositionAsync(connectorId, position));
        }

        await Task.WhenAll(tasks);

        // Assert — Position should be one of the written values (no corruption)
        var result = await store.GetPositionAsync(connectorId);
        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: o => o, Left: _ => default);
        option.IsSome.ShouldBeTrue();
        option.IfSome(p =>
        {
            var testPos = p.ShouldBeOfType<TestCdcPosition>();
            testPos.Value.ShouldBeInRange(0, 99);
        });
    }

    [Fact]
    public async Task ConcurrentReadsAndWrites_NoExceptions()
    {
        // Arrange
        var store = new InMemoryCdcPositionStore();
        var connectorId = "rw-connector";
        await store.SavePositionAsync(connectorId, new TestCdcPosition(0));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var writeTasks = new List<Task>();
        var readTasks = new List<Task>();

        // Act — Concurrent reads and writes for 2 seconds
        for (var i = 0; i < 50; i++)
        {
            var value = i;
            writeTasks.Add(Task.Run(async () =>
            {
                await store.SavePositionAsync(connectorId, new TestCdcPosition(value));
            }, cts.Token));

            readTasks.Add(Task.Run(async () =>
            {
                var result = await store.GetPositionAsync(connectorId);
                result.IsRight.ShouldBeTrue();
            }, cts.Token));
        }

        // Assert — No exceptions should be thrown
        await Task.WhenAll(writeTasks.Concat(readTasks));
    }

    [Fact]
    public async Task ConcurrentDeleteAndSave_NoExceptions()
    {
        // Arrange
        var store = new InMemoryCdcPositionStore();
        var connectorId = "delete-save-connector";
        await store.SavePositionAsync(connectorId, new TestCdcPosition(0));

        var tasks = new List<Task>();

        // Act — Interleaved delete and save operations
        for (var i = 0; i < 50; i++)
        {
            var value = i;
            tasks.Add(Task.Run(async () =>
            {
                await store.SavePositionAsync(connectorId, new TestCdcPosition(value));
            }));
            tasks.Add(Task.Run(async () =>
            {
                await store.DeletePositionAsync(connectorId);
            }));
        }

        // Assert — Should complete without exceptions
        await Task.WhenAll(tasks);

        // Final state is indeterminate (could be saved or deleted)
        var result = await store.GetPositionAsync(connectorId);
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Large Scale

    [Fact]
    public async Task LargeNumberOfConnectors_AllIndependent()
    {
        // Arrange
        var store = new InMemoryCdcPositionStore();
        var connectorCount = 1000;

        // Act — Save positions for 1000 connectors
        var saveTasks = Enumerable.Range(0, connectorCount)
            .Select(i => store.SavePositionAsync($"connector-{i}", new TestCdcPosition(i * 10)))
            .ToList();

        await Task.WhenAll(saveTasks);

        // Assert — All connectors have correct independent positions
        for (var i = 0; i < connectorCount; i++)
        {
            var result = await store.GetPositionAsync($"connector-{i}");
            result.IsRight.ShouldBeTrue();
            var option = result.Match(Right: o => o, Left: _ => default);
            option.IsSome.ShouldBeTrue();
            option.IfSome(p =>
            {
                var testPos = p.ShouldBeOfType<TestCdcPosition>();
                testPos.Value.ShouldBe(i * 10);
            });
        }
    }

    [Fact]
    public async Task RapidPositionUpdates_MaintainsConsistency()
    {
        // Arrange
        var store = new InMemoryCdcPositionStore();
        var connectorId = "rapid-updates";

        // Act — 1000 sequential position updates
        for (var i = 0; i < 1000; i++)
        {
            var result = await store.SavePositionAsync(connectorId, new TestCdcPosition(i));
            result.IsRight.ShouldBeTrue();
        }

        // Assert — Final position should be the last one saved
        var getResult = await store.GetPositionAsync(connectorId);
        getResult.IsRight.ShouldBeTrue();
        var option = getResult.Match(Right: o => o, Left: _ => default);
        option.IsSome.ShouldBeTrue();
        option.IfSome(p =>
        {
            var testPos = p.ShouldBeOfType<TestCdcPosition>();
            testPos.Value.ShouldBe(999);
        });
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public async Task FullLifecycle_SaveGetDeleteGet_WorksCorrectly()
    {
        // Arrange
        var store = new InMemoryCdcPositionStore();
        var connectorId = "lifecycle-test";

        // Act & Assert — Step 1: No position initially
        var step1 = await store.GetPositionAsync(connectorId);
        step1.IsRight.ShouldBeTrue();
        var opt1 = step1.Match(Right: o => o, Left: _ => default);
        opt1.IsNone.ShouldBeTrue();

        // Step 2: Save position
        var saveResult = await store.SavePositionAsync(connectorId, new TestCdcPosition(42));
        saveResult.IsRight.ShouldBeTrue();

        // Step 3: Get saved position
        var step3 = await store.GetPositionAsync(connectorId);
        step3.IsRight.ShouldBeTrue();
        var opt3 = step3.Match(Right: o => o, Left: _ => default);
        opt3.IsSome.ShouldBeTrue();
        opt3.IfSome(p =>
        {
            p.ShouldBeOfType<TestCdcPosition>().Value.ShouldBe(42);
        });

        // Step 4: Update position
        var updateResult = await store.SavePositionAsync(connectorId, new TestCdcPosition(100));
        updateResult.IsRight.ShouldBeTrue();

        var step4 = await store.GetPositionAsync(connectorId);
        step4.IsRight.ShouldBeTrue();
        var opt4 = step4.Match(Right: o => o, Left: _ => default);
        opt4.IfSome(p =>
        {
            p.ShouldBeOfType<TestCdcPosition>().Value.ShouldBe(100);
        });

        // Step 5: Delete position
        var deleteResult = await store.DeletePositionAsync(connectorId);
        deleteResult.IsRight.ShouldBeTrue();

        // Step 6: Verify deletion
        var step6 = await store.GetPositionAsync(connectorId);
        step6.IsRight.ShouldBeTrue();
        var opt6 = step6.Match(Right: o => o, Left: _ => default);
        opt6.IsNone.ShouldBeTrue();
    }

    #endregion
}
