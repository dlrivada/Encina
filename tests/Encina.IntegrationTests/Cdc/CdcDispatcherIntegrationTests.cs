using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Messaging;
using Encina.IntegrationTests.Cdc.Helpers;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Cdc;

/// <summary>
/// Integration tests for <see cref="CdcDispatcher"/> verifying the full dispatch pipeline
/// with real DI, real handler resolution, and real JSON deserialization.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "CDC")]
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Integration test assertions on ValueTask results")]
public sealed class CdcDispatcherIntegrationTests
{
    #region Test Helpers

    private sealed record DispatchFixture(
        ServiceProvider ServiceProvider,
        ICdcDispatcher Dispatcher,
        TrackingChangeHandler Handler,
        TrackingInterceptor? Interceptor = null) : IDisposable
    {
        public void Dispose() => ServiceProvider.Dispose();
    }

    private static DispatchFixture CreateFixture(
        bool withInterceptor = false,
        Action<CdcConfiguration>? configure = null)
    {
        var handler = new TrackingChangeHandler();
        var interceptor = withInterceptor ? new TrackingInterceptor() : null;

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        if (interceptor is not null)
        {
            services.AddSingleton<ICdcEventInterceptor>(interceptor);
        }

        services.AddEncinaCdc(config =>
        {
            config.AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities");

            configure?.Invoke(config);
        });
        // Register singleton AFTER AddEncinaCdc to override the scoped registration
        services.AddSingleton<IChangeEventHandler<TestEntity>>(handler);

        var sp = services.BuildServiceProvider();
        var dispatcher = sp.GetRequiredService<ICdcDispatcher>();

        return new DispatchFixture(sp, dispatcher, handler, interceptor);
    }

    #endregion

    #region Insert Dispatch

    [Fact]
    public async Task DispatchAsync_InsertEvent_RoutesToHandler_WithDeserializedEntity()
    {
        // Arrange
        using var fixture = CreateFixture();
        var evt = CdcTestFixtures.CreateInsertEvent(id: 42, name: "Integration");

        // Act
        var result = await fixture.Dispatcher.DispatchAsync(evt);

        // Assert
        result.IsRight.ShouldBeTrue();
        fixture.Handler.Invocations.ShouldHaveSingleItem();
        var invocation = fixture.Handler.Invocations[0];
        invocation.Operation.ShouldBe("Insert");
        invocation.After.ShouldNotBeNull();
        invocation.After!.Id.ShouldBe(42);
        invocation.After.Name.ShouldBe("Integration");
    }

    [Fact]
    public async Task DispatchAsync_InsertEvent_PassesCorrectContext()
    {
        // Arrange
        using var fixture = CreateFixture();
        var evt = CdcTestFixtures.CreateInsertEvent(tableName: "TestEntities");

        // Act
        await fixture.Dispatcher.DispatchAsync(evt);

        // Assert
        fixture.Handler.Invocations.ShouldHaveSingleItem();
        var context = fixture.Handler.Invocations[0].Context;
        context.TableName.ShouldBe("TestEntities");
        context.Metadata.ShouldNotBeNull();
        context.Metadata.Position.ShouldNotBeNull();
    }

    #endregion

    #region Update Dispatch

    [Fact]
    public async Task DispatchAsync_UpdateEvent_DeserializesBothBeforeAndAfter()
    {
        // Arrange
        using var fixture = CreateFixture();
        var evt = CdcTestFixtures.CreateUpdateEvent(
            id: 1, oldName: "Before", newName: "After");

        // Act
        var result = await fixture.Dispatcher.DispatchAsync(evt);

        // Assert
        result.IsRight.ShouldBeTrue();
        fixture.Handler.Invocations.ShouldHaveSingleItem();
        var invocation = fixture.Handler.Invocations[0];
        invocation.Operation.ShouldBe("Update");
        invocation.Before.ShouldNotBeNull();
        invocation.Before!.Name.ShouldBe("Before");
        invocation.After.ShouldNotBeNull();
        invocation.After!.Name.ShouldBe("After");
    }

    #endregion

    #region Delete Dispatch

    [Fact]
    public async Task DispatchAsync_DeleteEvent_DeserializesBeforeEntity()
    {
        // Arrange
        using var fixture = CreateFixture();
        var evt = CdcTestFixtures.CreateDeleteEvent(id: 99, name: "Deleted");

        // Act
        var result = await fixture.Dispatcher.DispatchAsync(evt);

        // Assert
        result.IsRight.ShouldBeTrue();
        fixture.Handler.Invocations.ShouldHaveSingleItem();
        var invocation = fixture.Handler.Invocations[0];
        invocation.Operation.ShouldBe("Delete");
        invocation.Before.ShouldNotBeNull();
        invocation.Before!.Id.ShouldBe(99);
        invocation.Before.Name.ShouldBe("Deleted");
    }

    #endregion

    #region Table Routing

    [Fact]
    public async Task DispatchAsync_UnmappedTable_SkipsGracefully()
    {
        // Arrange
        using var fixture = CreateFixture();
        var evt = CdcTestFixtures.CreateInsertEvent(tableName: "UnknownTable");

        // Act
        var result = await fixture.Dispatcher.DispatchAsync(evt);

        // Assert
        result.IsRight.ShouldBeTrue();
        fixture.Handler.Invocations.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_CaseInsensitiveTableName_StillRoutes()
    {
        // Arrange
        using var fixture = CreateFixture();
        var evt = CdcTestFixtures.CreateInsertEvent(tableName: "TESTENTITIES");

        // Act
        var result = await fixture.Dispatcher.DispatchAsync(evt);

        // Assert
        result.IsRight.ShouldBeTrue();
        fixture.Handler.Invocations.ShouldHaveSingleItem();
    }

    #endregion

    #region Multiple Events

    [Fact]
    public async Task DispatchAsync_MultipleEvents_AllRoutedCorrectly()
    {
        // Arrange
        using var fixture = CreateFixture();
        var events = new[]
        {
            CdcTestFixtures.CreateInsertEvent(id: 1, name: "A", positionValue: 1),
            CdcTestFixtures.CreateUpdateEvent(id: 2, oldName: "B", newName: "C", positionValue: 2),
            CdcTestFixtures.CreateDeleteEvent(id: 3, name: "D", positionValue: 3)
        };

        // Act
        foreach (var evt in events)
        {
            var result = await fixture.Dispatcher.DispatchAsync(evt);
            result.IsRight.ShouldBeTrue();
        }

        // Assert
        fixture.Handler.Invocations.Count.ShouldBe(3);
        fixture.Handler.Invocations[0].Operation.ShouldBe("Insert");
        fixture.Handler.Invocations[1].Operation.ShouldBe("Update");
        fixture.Handler.Invocations[2].Operation.ShouldBe("Delete");
    }

    #endregion

    #region Interceptors

    [Fact]
    public async Task DispatchAsync_WithInterceptor_InvokesAfterSuccessfulDispatch()
    {
        // Arrange
        using var fixture = CreateFixture(withInterceptor: true);
        var evt = CdcTestFixtures.CreateInsertEvent();

        // Act
        var result = await fixture.Dispatcher.DispatchAsync(evt);

        // Assert
        result.IsRight.ShouldBeTrue();
        fixture.Interceptor!.InterceptedEvents.ShouldHaveSingleItem();
        fixture.Interceptor.InterceptedEvents[0].TableName.ShouldBe("TestEntities");
    }

    [Fact]
    public async Task DispatchAsync_WithInterceptor_NotInvokedOnDispatchFailure()
    {
        // Arrange
        using var fixture = CreateFixture(withInterceptor: true);
        // Create an event with null After data for Insert — should fail deserialization
        var evt = CdcTestFixtures.CreateChangeEvent("TestEntities", ChangeOperation.Insert, after: null);

        // Act
        var result = await fixture.Dispatcher.DispatchAsync(evt);

        // Assert
        result.IsLeft.ShouldBeTrue();
        fixture.Interceptor!.InterceptedEvents.ShouldBeEmpty();
    }

    #endregion

    #region Snapshot Operation

    [Fact]
    public async Task DispatchAsync_SnapshotEvent_RoutedAsInsert()
    {
        // Arrange
        using var fixture = CreateFixture();
        var entity = CdcTestFixtures.CreateTestEntity(id: 7, name: "Snapshot");
        var evt = CdcTestFixtures.CreateJsonChangeEvent(
            "TestEntities", ChangeOperation.Snapshot, after: entity);

        // Act
        var result = await fixture.Dispatcher.DispatchAsync(evt);

        // Assert
        result.IsRight.ShouldBeTrue();
        fixture.Handler.Invocations.ShouldHaveSingleItem();
        fixture.Handler.Invocations[0].Operation.ShouldBe("Insert");
        fixture.Handler.Invocations[0].After!.Name.ShouldBe("Snapshot");
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task DispatchAsync_HandlerReturnsError_ReturnsLeftWithoutInterceptor()
    {
        // Arrange
        using var fixture = CreateFixture(withInterceptor: true);
        fixture.Handler.SetFailure(() => Left(EncinaError.New("handler failed")));
        var evt = CdcTestFixtures.CreateInsertEvent();

        // Act
        var result = await fixture.Dispatcher.DispatchAsync(evt);

        // Assert
        result.IsLeft.ShouldBeTrue();
        fixture.Interceptor!.InterceptedEvents.ShouldBeEmpty();
    }

    #endregion
}
