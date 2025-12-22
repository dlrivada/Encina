using FluentAssertions;
using Xunit;

namespace Encina.AspNetCore.Tests;

public class RequestContextAccessorTests
{
    [Fact]
    public void RequestContext_InitiallyNull()
    {
        // Arrange
        var accessor = new RequestContextAccessor();

        // Act & Assert
        accessor.RequestContext.Should().BeNull();
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
        accessor.RequestContext.Should().Be(context);
    }

    [Fact]
    public void RequestContext_CanBeCleared()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        var context = RequestContext.CreateForTest();
        accessor.RequestContext = context;

        // Act
        accessor.RequestContext = null;

        // Assert
        accessor.RequestContext.Should().BeNull();
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
        capturedCorrelationId1.Should().Be("context-1");
        capturedCorrelationId2.Should().Be("context-2");
    }

    [Fact]
    public async Task RequestContext_FlowsAcrossAwaitPoints()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        var context = RequestContext.CreateForTest(userId: "user-123");

        // Act
        accessor.RequestContext = context;

        await Task.Delay(10);
        var afterFirstAwait = accessor.RequestContext;

        await Task.Delay(10);
        var afterSecondAwait = accessor.RequestContext;

        // Assert
        afterFirstAwait.Should().Be(context);
        afterSecondAwait.Should().Be(context);
        afterSecondAwait?.UserId.Should().NotBeNull();
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
        accessor2.RequestContext.Should().Be(context);
        accessor2.RequestContext?.CorrelationId.Should().Be("shared-context");
    }

    [Fact]
    public async Task RequestContext_NestedAsyncCalls_MaintainContext()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        var context = RequestContext.CreateForTest(userId: "outer-user");

        async Task<string?> InnerAsyncMethod()
        {
            await Task.Delay(10);
            return accessor.RequestContext?.UserId;
        }

        async Task<string?> OuterAsyncMethod()
        {
            accessor.RequestContext = context;
            await Task.Delay(10);
            return await InnerAsyncMethod();
        }

        // Act
        var result = await OuterAsyncMethod();

        // Assert
        result.Should().Be("outer-user");
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
        results.Should().HaveCount(5);
        results.Should().Contain("thread-0");
        results.Should().Contain("thread-1");
        results.Should().Contain("thread-2");
        results.Should().Contain("thread-3");
        results.Should().Contain("thread-4");
    }
}
