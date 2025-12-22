using Shouldly;

namespace Encina.AspNetCore.ContractTests;

/// <summary>
/// Contract tests for <see cref="IRequestContextAccessor"/> and <see cref="RequestContextAccessor"/>.
/// Verifies that the accessor follows AsyncLocal semantics and isolation guarantees.
/// </summary>
[Trait("Category", "Contract")]
public sealed class RequestContextAccessorContractTests
{
    [Fact]
    public void Contract_MustImplementIRequestContextAccessor()
    {
        // Arrange & Act
        var accessor = new RequestContextAccessor();

        // Assert
        accessor.ShouldBeAssignableTo<IRequestContextAccessor>();
    }

    [Fact]
    public void Contract_InitialStateMustBeNull()
    {
        // Arrange & Act
        var accessor = new RequestContextAccessor();

        // Assert
        accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public void Contract_SetAndGetMustBeConsistent()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        var context = RequestContext.CreateForTest(correlationId: "test-correlation");

        // Act
        accessor.RequestContext = context;
        var retrieved = accessor.RequestContext;

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.ShouldBe(context);
        retrieved!.CorrelationId.ShouldBe("test-correlation");
    }

    [Fact]
    public void Contract_SetNullMustClearContext()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        accessor.RequestContext = RequestContext.CreateForTest();

        // Act
        accessor.RequestContext = null;

        // Assert
        accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task Contract_MustFlowAcrossAsyncBoundaries()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        var context = RequestContext.CreateForTest(correlationId: "async-flow");

        // Act
        accessor.RequestContext = context;

        await Task.Delay(10);
        var afterFirstAwait = accessor.RequestContext;

        await Task.Delay(10);
        var afterSecondAwait = accessor.RequestContext;

        // Assert
        afterFirstAwait.ShouldBe(context);
        afterSecondAwait.ShouldBe(context);
        afterSecondAwait?.CorrelationId.ShouldBe("async-flow");
    }

    [Fact]
    public async Task Contract_MustIsolateAsyncFlows()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        var context1 = RequestContext.CreateForTest(correlationId: "flow-1");
        var context2 = RequestContext.CreateForTest(correlationId: "flow-2");

        string? captured1 = null;
        string? captured2 = null;

        // Act
        var task1 = Task.Run(async () =>
        {
            accessor.RequestContext = context1;
            await Task.Delay(50);
            captured1 = accessor.RequestContext?.CorrelationId;
        });

        var task2 = Task.Run(async () =>
        {
            accessor.RequestContext = context2;
            await Task.Delay(50);
            captured2 = accessor.RequestContext?.CorrelationId;
        });

        await Task.WhenAll(task1, task2);

        // Assert
        captured1.ShouldBe("flow-1");
        captured2.ShouldBe("flow-2");
    }

    [Fact]
    public void Contract_MustIsolateThreads()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        var results = new System.Collections.Concurrent.ConcurrentBag<string?>();

        // Act
        var threads = Enumerable.Range(0, 5).Select(i => new Thread(() =>
        {
            var context = RequestContext.CreateForTest(correlationId: $"thread-{i}");
            accessor.RequestContext = context;
            Thread.Sleep(50);
            results.Add(accessor.RequestContext?.CorrelationId);
        })).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        // Assert
        results.ShouldContain("thread-0");
        results.ShouldContain("thread-1");
        results.ShouldContain("thread-2");
        results.ShouldContain("thread-3");
        results.ShouldContain("thread-4");
        results.Count.ShouldBe(5);
    }

    [Fact]
    public async Task Contract_MultipleAccessorsMustShareAsyncLocal()
    {
        // Arrange
        var accessor1 = new RequestContextAccessor();
        var accessor2 = new RequestContextAccessor();
        var context = RequestContext.CreateForTest(correlationId: "shared");

        // Act
        accessor1.RequestContext = context;
        await Task.Yield();

        // Assert
        accessor2.RequestContext.ShouldBe(context);
        accessor2.RequestContext?.CorrelationId.ShouldBe("shared");
    }

    [Fact]
    public async Task Contract_NestedCallsMustMaintainContext()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        var outerContext = RequestContext.CreateForTest(userId: "outer-user");

        async Task<string?> InnerAsync()
        {
            await Task.Delay(10);
            return accessor.RequestContext?.UserId;
        }

        async Task<string?> OuterAsync()
        {
            accessor.RequestContext = outerContext;
            await Task.Delay(10);
            return await InnerAsync();
        }

        // Act
        var result = await OuterAsync();

        // Assert
        result.ShouldBe("outer-user");
    }

    [Fact]
    public void Contract_MultipleSetsMustOverwritePrevious()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        var context1 = RequestContext.CreateForTest(correlationId: "first");
        var context2 = RequestContext.CreateForTest(correlationId: "second");

        // Act
        accessor.RequestContext = context1;
        accessor.RequestContext = context2;

        // Assert
        accessor.RequestContext.ShouldBe(context2);
        accessor.RequestContext?.CorrelationId.ShouldBe("second");
    }

    [Fact]
    public void Contract_NullContextMustBeAllowed()
    {
        // Arrange
        var accessor = new RequestContextAccessor();

        // Act
        accessor.RequestContext = null;
        var retrieved = accessor.RequestContext;

        // Assert
        retrieved.ShouldBeNull();
    }
}
