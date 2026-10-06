using Encina.Testing;
using Encina.Testing.Identity;
using Encina.UnitTests.Core.Identity;
using Shouldly;
using Xunit;

#pragma warning disable S2925 // "Thread.Sleep" should not be used in tests - Required for thread synchronization simulation in non-async threads

namespace Encina.UnitTests.Core;

public class RequestContextAccessorTests
{
    [Fact]
    public void RequestContext_InitiallyNull()
    {
        // Arrange
        var accessor = new RequestContextAccessor();

        // Act & Assert
        accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public void RequestContext_CanBeSet()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        var context = RequestContext.CreateForTest();

        // Act
        accessor.RequestContext = context;

        // Assert
        accessor.RequestContext.ShouldBe(context);
    }

    [Fact]
    public void RequestContext_CannotBeCleared_TheSetterThrowsAndKeepsTheContext()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        var context = RequestContext.CreateForTest();
        accessor.RequestContext = context;

        // Act
        Should.Throw<InvalidOperationException>(() => accessor.RequestContext = null);

        // Assert
        accessor.RequestContext.ShouldBe(context);
    }

    [Fact]
    public async Task RequestContext_IsolatedBetweenAsyncFlows()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        var context1 = RequestContext.CreateForTest(correlationId: "context-1");
        var context2 = RequestContext.CreateForTest(correlationId: "context-2");

        string? capturedCorrelationId1 = null;
        string? capturedCorrelationId2 = null;

        // Act - Start two async flows
        var task1 = Task.Run(async () =>
        {
            accessor.RequestContext = context1;
            await Task.Delay(50);
            capturedCorrelationId1 = accessor.RequestContext?.CorrelationId;
        });

        var task2 = Task.Run(async () =>
        {
            accessor.RequestContext = context2;
            await Task.Delay(50);
            capturedCorrelationId2 = accessor.RequestContext?.CorrelationId;
        });

        await Task.WhenAll(task1, task2);

        // Assert - Each flow should maintain its own context
        capturedCorrelationId1.ShouldBe("context-1");
        capturedCorrelationId2.ShouldBe("context-2");
    }

    [Fact]
    public async Task RequestContext_FlowsAcrossAwaitPoints()
    {
        // Arrange: the identity is bound through the scope factory
        var host = new Identity.ScopeTestHost();
        var accessor = new RequestContextAccessor();
        IRequestContext? context = null;
        IRequestContext? afterFirstAwait = null;
        IRequestContext? afterSecondAwait = null;

        // Act
        await host.InUserScope("user-123", async scoped =>
        {
            context = scoped;
            await Task.Delay(10);
            afterFirstAwait = accessor.RequestContext;

            await Task.Delay(10);
            afterSecondAwait = accessor.RequestContext;
            return 0;
        });

        // Assert
        afterFirstAwait.ShouldBe(context);
        afterSecondAwait.ShouldBe(context);
        afterSecondAwait.Issued().UserId.ShouldBe("user-123");
    }

    [Fact]
    public async Task RequestContext_MultipleAccessors_ShareAsyncLocalStorage()
    {
        // Arrange
        var accessor1 = new RequestContextAccessor();
        var accessor2 = new RequestContextAccessor();
        var context = RequestContext.CreateForTest(correlationId: "shared-context");

        // Act
        accessor1.RequestContext = context;
        await Task.Yield(); // Force async continuation

        // Assert - Both accessors should see the same AsyncLocal value
        accessor2.RequestContext.ShouldBe(context);
        accessor2.RequestContext?.CorrelationId.ShouldBe("shared-context");
    }

    [Fact]
    public async Task RequestContext_NestedAsyncCalls_MaintainContext()
    {
        // Arrange: the identity is bound through the scope factory
        var host = new Identity.ScopeTestHost();
        var accessor = new RequestContextAccessor();

        async Task<string?> InnerAsyncMethod()
        {
            await Task.Delay(10);
            return accessor.RequestContext?.UserId;
        }

        Task<string?> OuterAsyncMethod() =>
            host.InUserScope("outer-user", async _ =>
            {
                await Task.Delay(10);
                return await InnerAsyncMethod();
            });

        // Act
        var result = await OuterAsyncMethod();

        // Assert
        result.ShouldBe("outer-user");
    }

    [Fact]
    public void RequestContext_MultipleThreads_DoNotShareContext()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        var results = new System.Collections.Concurrent.ConcurrentBag<string?>();

        // Act - Create multiple threads, each with its own context
        var threads = Enumerable.Range(0, 5).Select(i => new Thread(() =>
        {
            var context = RequestContext.CreateForTest(correlationId: $"thread-{i}");
            accessor.RequestContext = context;
            Thread.Sleep(50); // Simulate work
            results.Add(accessor.RequestContext?.CorrelationId);
        })).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        // Assert - Each thread should have its own correlation ID
        results.Count.ShouldBe(5);
        results.ShouldContain("thread-0");
        results.ShouldContain("thread-1");
        results.ShouldContain("thread-2");
        results.ShouldContain("thread-3");
        results.ShouldContain("thread-4");
    }
}
