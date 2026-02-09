using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Messaging;
using Encina.Cdc.Processing;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Cdc;

/// <summary>
/// Unit tests for <see cref="CdcDispatcher"/>.
/// </summary>
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Mock setup pattern for NSubstitute")]
public sealed class CdcDispatcherTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

    #region Test Helpers

    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestHandler : IChangeEventHandler<TestEntity>
    {
        public List<(string Operation, TestEntity? Before, TestEntity? After)> Invocations { get; } = [];

        public ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(TestEntity entity, ChangeContext context)
        {
            Invocations.Add(("Insert", null, entity));
            return new(Right(unit));
        }

        public ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(TestEntity before, TestEntity after, ChangeContext context)
        {
            Invocations.Add(("Update", before, after));
            return new(Right(unit));
        }

        public ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(TestEntity entity, ChangeContext context)
        {
            Invocations.Add(("Delete", entity, null));
            return new(Right(unit));
        }
    }

    private sealed class FailingHandler : IChangeEventHandler<TestEntity>
    {
        public ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(TestEntity entity, ChangeContext context)
            => throw new InvalidOperationException("Handler failed");

        public ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(TestEntity before, TestEntity after, ChangeContext context)
            => throw new InvalidOperationException("Handler failed");

        public ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(TestEntity entity, ChangeContext context)
            => throw new InvalidOperationException("Handler failed");
    }

    private static (CdcDispatcher Dispatcher, TestHandler Handler) CreateTestFixture()
    {
        var handler = new TestHandler();
        var config = new CdcConfiguration();
        config.WithTableMapping<TestEntity>("TestEntities");
        config.AddHandler<TestEntity, TestHandler>();

        var services = new ServiceCollection();
        services.AddSingleton<IChangeEventHandler<TestEntity>>(handler);
        var serviceProvider = services.BuildServiceProvider();

        var logger = NullLogger<CdcDispatcher>.Instance;
        var dispatcher = new CdcDispatcher(serviceProvider, logger, config);

        return (dispatcher, handler);
    }

    private static ChangeEvent CreateChangeEvent(
        string tableName,
        ChangeOperation operation,
        object? before = null,
        object? after = null)
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);
        return new ChangeEvent(tableName, operation, before, after, metadata);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var config = new CdcConfiguration();
        var logger = NullLogger<CdcDispatcher>.Instance;

        Should.Throw<ArgumentNullException>(() =>
            new CdcDispatcher(null!, logger, config));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var config = new CdcConfiguration();
        var sp = new ServiceCollection().BuildServiceProvider();

        Should.Throw<ArgumentNullException>(() =>
            new CdcDispatcher(sp, null!, config));
    }

    [Fact]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var logger = NullLogger<CdcDispatcher>.Instance;

        Should.Throw<ArgumentNullException>(() =>
            new CdcDispatcher(sp, logger, null!));
    }

    #endregion

    #region DispatchAsync - Insert

    [Fact]
    public async Task DispatchAsync_InsertEvent_CallsHandleInsertAsync()
    {
        var (dispatcher, handler) = CreateTestFixture();
        var after = JsonSerializer.SerializeToElement(new TestEntity { Id = 1, Name = "Test" });
        var evt = CreateChangeEvent("TestEntities", ChangeOperation.Insert, after: after);

        var result = await dispatcher.DispatchAsync(evt);

        result.IsRight.ShouldBeTrue();
        handler.Invocations.ShouldHaveSingleItem();
        handler.Invocations[0].Operation.ShouldBe("Insert");
        handler.Invocations[0].After!.Id.ShouldBe(1);
    }

    [Fact]
    public async Task DispatchAsync_SnapshotEvent_CallsHandleInsertAsync()
    {
        var (dispatcher, handler) = CreateTestFixture();
        var after = JsonSerializer.SerializeToElement(new TestEntity { Id = 2, Name = "Snapshot" });
        var evt = CreateChangeEvent("TestEntities", ChangeOperation.Snapshot, after: after);

        var result = await dispatcher.DispatchAsync(evt);

        result.IsRight.ShouldBeTrue();
        handler.Invocations.ShouldHaveSingleItem();
        handler.Invocations[0].Operation.ShouldBe("Insert");
    }

    [Fact]
    public async Task DispatchAsync_InsertEvent_NullAfter_ReturnsError()
    {
        var (dispatcher, _) = CreateTestFixture();
        var evt = CreateChangeEvent("TestEntities", ChangeOperation.Insert, after: null);

        var result = await dispatcher.DispatchAsync(evt);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region DispatchAsync - Update

    [Fact]
    public async Task DispatchAsync_UpdateEvent_CallsHandleUpdateAsync()
    {
        var (dispatcher, handler) = CreateTestFixture();
        var before = JsonSerializer.SerializeToElement(new TestEntity { Id = 1, Name = "Old" });
        var after = JsonSerializer.SerializeToElement(new TestEntity { Id = 1, Name = "New" });
        var evt = CreateChangeEvent("TestEntities", ChangeOperation.Update, before, after);

        var result = await dispatcher.DispatchAsync(evt);

        result.IsRight.ShouldBeTrue();
        handler.Invocations.ShouldHaveSingleItem();
        handler.Invocations[0].Operation.ShouldBe("Update");
        handler.Invocations[0].Before!.Name.ShouldBe("Old");
        handler.Invocations[0].After!.Name.ShouldBe("New");
    }

    [Fact]
    public async Task DispatchAsync_UpdateEvent_NullBefore_ReturnsError()
    {
        var (dispatcher, _) = CreateTestFixture();
        var after = JsonSerializer.SerializeToElement(new TestEntity { Id = 1 });
        var evt = CreateChangeEvent("TestEntities", ChangeOperation.Update, null, after);

        var result = await dispatcher.DispatchAsync(evt);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchAsync_UpdateEvent_NullAfter_ReturnsError()
    {
        var (dispatcher, _) = CreateTestFixture();
        var before = JsonSerializer.SerializeToElement(new TestEntity { Id = 1 });
        var evt = CreateChangeEvent("TestEntities", ChangeOperation.Update, before, null);

        var result = await dispatcher.DispatchAsync(evt);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region DispatchAsync - Delete

    [Fact]
    public async Task DispatchAsync_DeleteEvent_CallsHandleDeleteAsync()
    {
        var (dispatcher, handler) = CreateTestFixture();
        var before = JsonSerializer.SerializeToElement(new TestEntity { Id = 1, Name = "Deleted" });
        var evt = CreateChangeEvent("TestEntities", ChangeOperation.Delete, before: before);

        var result = await dispatcher.DispatchAsync(evt);

        result.IsRight.ShouldBeTrue();
        handler.Invocations.ShouldHaveSingleItem();
        handler.Invocations[0].Operation.ShouldBe("Delete");
        handler.Invocations[0].Before!.Name.ShouldBe("Deleted");
    }

    [Fact]
    public async Task DispatchAsync_DeleteEvent_NullBefore_ReturnsError()
    {
        var (dispatcher, _) = CreateTestFixture();
        var evt = CreateChangeEvent("TestEntities", ChangeOperation.Delete, before: null);

        var result = await dispatcher.DispatchAsync(evt);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region DispatchAsync - No Handler

    [Fact]
    public async Task DispatchAsync_UnmappedTable_ReturnsSuccess()
    {
        var (dispatcher, handler) = CreateTestFixture();
        var evt = CreateChangeEvent("UnknownTable", ChangeOperation.Insert, after: "data");

        var result = await dispatcher.DispatchAsync(evt);

        result.IsRight.ShouldBeTrue();
        handler.Invocations.ShouldBeEmpty();
    }

    #endregion

    #region DispatchAsync - Case Insensitive

    [Fact]
    public async Task DispatchAsync_TableName_IsCaseInsensitive()
    {
        var (dispatcher, handler) = CreateTestFixture();
        var after = JsonSerializer.SerializeToElement(new TestEntity { Id = 1, Name = "Test" });
        var evt = CreateChangeEvent("TESTENTITIES", ChangeOperation.Insert, after: after);

        var result = await dispatcher.DispatchAsync(evt);

        result.IsRight.ShouldBeTrue();
        handler.Invocations.ShouldHaveSingleItem();
    }

    #endregion

    #region DispatchAsync - Handler Exception

    [Fact]
    public async Task DispatchAsync_HandlerThrows_ReturnsError()
    {
        var config = new CdcConfiguration();
        config.WithTableMapping<TestEntity>("Failing");
        config.AddHandler<TestEntity, FailingHandler>();

        var services = new ServiceCollection();
        services.AddSingleton<IChangeEventHandler<TestEntity>>(new FailingHandler());
        var sp = services.BuildServiceProvider();

        var dispatcher = new CdcDispatcher(sp, NullLogger<CdcDispatcher>.Instance, config);
        var after = JsonSerializer.SerializeToElement(new TestEntity { Id = 1 });
        var evt = CreateChangeEvent("Failing", ChangeOperation.Insert, after: after);

        var result = await dispatcher.DispatchAsync(evt);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region DispatchAsync - Null Event

    [Fact]
    public async Task DispatchAsync_NullEvent_ThrowsArgumentNullException()
    {
        var (dispatcher, _) = CreateTestFixture();

        await Should.ThrowAsync<ArgumentNullException>(
            () => dispatcher.DispatchAsync(null!).AsTask());
    }

    #endregion

    #region DispatchAsync - Interceptors

    [Fact]
    public async Task DispatchAsync_WithInterceptor_InvokesInterceptorOnSuccess()
    {
        var interceptor = Substitute.For<ICdcEventInterceptor>();
        interceptor.OnEventDispatchedAsync(Arg.Any<ChangeEvent>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right(unit)));

        var handler = new TestHandler();
        var config = new CdcConfiguration();
        config.WithTableMapping<TestEntity>("Test");
        config.AddHandler<TestEntity, TestHandler>();

        var services = new ServiceCollection();
        services.AddSingleton<IChangeEventHandler<TestEntity>>(handler);
        services.AddSingleton(interceptor);
        var sp = services.BuildServiceProvider();

        var dispatcher = new CdcDispatcher(sp, NullLogger<CdcDispatcher>.Instance, config);
        var after = JsonSerializer.SerializeToElement(new TestEntity { Id = 1 });
        var evt = CreateChangeEvent("Test", ChangeOperation.Insert, after: after);

        await dispatcher.DispatchAsync(evt);

        await interceptor.Received(1).OnEventDispatchedAsync(
            Arg.Is<ChangeEvent>(e => e.TableName == "Test"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_InterceptorThrows_DoesNotFailDispatch()
    {
        var interceptor = Substitute.For<ICdcEventInterceptor>();
        interceptor.OnEventDispatchedAsync(Arg.Any<ChangeEvent>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException("interceptor error"));

        var handler = new TestHandler();
        var config = new CdcConfiguration();
        config.WithTableMapping<TestEntity>("Test");
        config.AddHandler<TestEntity, TestHandler>();

        var services = new ServiceCollection();
        services.AddSingleton<IChangeEventHandler<TestEntity>>(handler);
        services.AddSingleton(interceptor);
        var sp = services.BuildServiceProvider();

        var dispatcher = new CdcDispatcher(sp, NullLogger<CdcDispatcher>.Instance, config);
        var after = JsonSerializer.SerializeToElement(new TestEntity { Id = 1 });
        var evt = CreateChangeEvent("Test", ChangeOperation.Insert, after: after);

        var result = await dispatcher.DispatchAsync(evt);

        // Dispatch still succeeds even though interceptor threw
        result.IsRight.ShouldBeTrue();
        handler.Invocations.ShouldHaveSingleItem();
    }

    #endregion
}
