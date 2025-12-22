using Shouldly;

namespace Encina.AspNetCore.PropertyTests;

/// <summary>
/// Property-based tests for <see cref="RequestContextAccessor"/>.
/// Verifies invariants that must hold across all possible inputs.
/// </summary>
[Trait("Category", "Property")]
public sealed class RequestContextAccessorPropertyTests
{
    [Fact]
    public async Task Property_SetThenGet_AlwaysReturnsWhatWasSet()
    {
        // Property: accessor.Get() after accessor.Set(x) ALWAYS returns x
        var accessor = new RequestContextAccessor();
        var contexts = Enumerable.Range(0, 10)
            .Select(i => RequestContext.CreateForTest(correlationId: $"context-{i}"))
            .ToList();

        foreach (var context in contexts)
        {
            accessor.RequestContext = context;
            var retrieved = accessor.RequestContext;

            retrieved.ShouldBe(context);
            retrieved?.CorrelationId.ShouldBe(context.CorrelationId);
        }
    }

    [Fact]
    public async Task Property_SetNull_AlwaysResultsInNull()
    {
        // Property: Set(null) ALWAYS results in Get() == null
        var accessor = new RequestContextAccessor();
        var contexts = Enumerable.Range(0, 5)
            .Select(_ => RequestContext.CreateForTest())
            .ToList();

        foreach (var context in contexts)
        {
            accessor.RequestContext = context;
            accessor.RequestContext.ShouldNotBeNull();

            accessor.RequestContext = null;
            accessor.RequestContext.ShouldBeNull();
        }
    }

    [Fact]
    public async Task Property_AsyncFlowIsolation_NeverCrossTalk()
    {
        // Property: Two async flows NEVER see each other's contexts
        var accessor = new RequestContextAccessor();
        var results = new System.Collections.Concurrent.ConcurrentBag<(string Expected, string? Actual)>();

        var tasks = Enumerable.Range(0, 20).Select(async i =>
        {
            var expected = $"flow-{i}";
            var context = RequestContext.CreateForTest(correlationId: expected);

            accessor.RequestContext = context;
            await Task.Delay(Random.Shared.Next(10, 50));

            var actual = accessor.RequestContext?.CorrelationId;
            results.Add((expected, actual));
        });

        await Task.WhenAll(tasks);

        // All flows must have maintained their own context
        foreach (var (expected, actual) in results)
        {
            actual.ShouldBe(expected);
        }
    }

    [Fact]
    public void Property_ThreadIsolation_NeverCrossTalk()
    {
        // Property: Different threads NEVER see each other's contexts
        var accessor = new RequestContextAccessor();
        var results = new System.Collections.Concurrent.ConcurrentBag<(string Expected, string? Actual)>();

        var threads = Enumerable.Range(0, 10).Select(i =>
        {
            var expected = $"thread-{i}";
            return new Thread(() =>
            {
                var context = RequestContext.CreateForTest(correlationId: expected);
                accessor.RequestContext = context;

                Thread.Sleep(Random.Shared.Next(10, 50));

                var actual = accessor.RequestContext?.CorrelationId;
                results.Add((expected, actual));
            });
        }).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        // Each thread must have its own isolated context
        foreach (var (expected, actual) in results)
        {
            actual.ShouldBe(expected);
        }
    }

    [Fact]
    public async Task Property_LastSetWins_AlwaysOverwritesPrevious()
    {
        // Property: The LAST Set() call ALWAYS determines the current value
        var accessor = new RequestContextAccessor();
        var contexts = Enumerable.Range(0, 20)
            .Select(i => RequestContext.CreateForTest(correlationId: $"context-{i}"))
            .ToList();

        foreach (var context in contexts)
        {
            accessor.RequestContext = context;
        }

        // Only the last one should be visible
        var final = accessor.RequestContext;
        final.ShouldBe(contexts.Last());
        final?.CorrelationId.ShouldBe("context-19");
    }

    [Fact]
    public async Task Property_AsyncBoundaryPreservation_AlwaysMaintainsContext()
    {
        // Property: Context ALWAYS flows across await boundaries
        var accessor = new RequestContextAccessor();
        var testCases = Enumerable.Range(0, 10).Select(i => $"async-{i}").ToList();

        foreach (var expected in testCases)
        {
            accessor.RequestContext = RequestContext.CreateForTest(correlationId: expected);

            await Task.Delay(10);
            var after1 = accessor.RequestContext?.CorrelationId;

            await Task.Yield();
            var after2 = accessor.RequestContext?.CorrelationId;

            await Task.Delay(10);
            var after3 = accessor.RequestContext?.CorrelationId;

            after1.ShouldBe(expected);
            after2.ShouldBe(expected);
            after3.ShouldBe(expected);
        }
    }

    [Fact]
    public async Task Property_MultipleAccessors_AlwaysShareSameAsyncLocal()
    {
        // Property: Multiple accessor instances ALWAYS see the same AsyncLocal value
        var accessor1 = new RequestContextAccessor();
        var accessor2 = new RequestContextAccessor();
        var accessor3 = new RequestContextAccessor();

        var contexts = Enumerable.Range(0, 5)
            .Select(i => RequestContext.CreateForTest(correlationId: $"shared-{i}"))
            .ToList();

        foreach (var context in contexts)
        {
            accessor1.RequestContext = context;
            await Task.Yield();

            accessor2.RequestContext.ShouldBe(context);
            accessor3.RequestContext.ShouldBe(context);

            accessor2.RequestContext?.CorrelationId.ShouldBe(context.CorrelationId);
            accessor3.RequestContext?.CorrelationId.ShouldBe(context.CorrelationId);
        }
    }

    [Fact]
    public async Task Property_NestedAsyncCalls_AlwaysInheritParentContext()
    {
        // Property: Nested async methods ALWAYS inherit parent's context
        var accessor = new RequestContextAccessor();

        async Task<string?> Level3()
        {
            await Task.Delay(5);
            return accessor.RequestContext?.CorrelationId;
        }

        async Task<string?> Level2()
        {
            await Task.Delay(5);
            return await Level3();
        }

        async Task<string?> Level1(string expected)
        {
            accessor.RequestContext = RequestContext.CreateForTest(correlationId: expected);
            await Task.Delay(5);
            return await Level2();
        }

        var testCases = Enumerable.Range(0, 5).Select(i => $"nested-{i}").ToList();

        foreach (var expected in testCases)
        {
            var result = await Level1(expected);
            result.ShouldBe(expected);
        }
    }

    [Fact]
    public void Property_InitialState_AlwaysNull()
    {
        // Property: New accessor ALWAYS starts with null context
        var accessors = Enumerable.Range(0, 10)
            .Select(_ => new RequestContextAccessor())
            .ToList();

        foreach (var accessor in accessors)
        {
            accessor.RequestContext.ShouldBeNull();
        }
    }

    [Fact]
    public async Task Property_ConcurrentSets_EventualConsistency()
    {
        // Property: Even under concurrent writes, accessor ALWAYS returns a valid context (not corrupted)
        var accessor = new RequestContextAccessor();
        var contexts = Enumerable.Range(0, 50)
            .Select(i => RequestContext.CreateForTest(correlationId: $"concurrent-{i}"))
            .ToList();

        var tasks = contexts.Select(async context =>
        {
            accessor.RequestContext = context;
            await Task.Yield();

            var retrieved = accessor.RequestContext;
            retrieved.ShouldNotBeNull();
            retrieved?.CorrelationId.ShouldNotBeNullOrWhiteSpace();
        });

        await Task.WhenAll(tasks);
    }
}
