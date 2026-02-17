using Encina.Messaging.ReadWriteSeparation;
using Encina.MongoDB;
using Encina.MongoDB.ReadWriteSeparation;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.ReadWriteSeparation;

/// <summary>
/// Integration tests for the <see cref="ReadWriteRoutingPipelineBehavior{TRequest, TResponse}"/>
/// verifying that database routing intent is correctly set based on request type.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the routing pipeline behavior correctly determines the
/// <see cref="DatabaseIntent"/> based on whether the request is a command, query,
/// or query with <see cref="ForceWriteDatabaseAttribute"/>.
/// </para>
/// </remarks>
[Collection(MongoDbReplicaSetCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
[Trait("Feature", "ReadWriteSeparation")]
[Trait("Feature", "Pipeline")]
public sealed class RoutingPipelineMongoDBIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbReplicaSetFixture _fixture;

    public RoutingPipelineMongoDBIntegrationTests(MongoDbReplicaSetFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
    {
        // Always clear routing context after tests
        DatabaseRoutingContext.Clear();
        return ValueTask.CompletedTask;
    }

    #region Test Request Types

    /// <summary>
    /// Test command for pipeline behavior tests.
    /// </summary>
    public sealed record TestCommand(string Data) : ICommand<string>;

    /// <summary>
    /// Test query for pipeline behavior tests.
    /// </summary>
    public sealed record TestQuery(string Filter) : IQuery<string>;

    /// <summary>
    /// Test query with ForceWriteDatabase attribute.
    /// </summary>
    [ForceWriteDatabase(Reason = "Test - requires read-after-write consistency")]
    public sealed record TestForceWriteQuery(string Filter) : IQuery<string>;

    /// <summary>
    /// Test handler that captures the current DatabaseIntent during execution.
    /// </summary>
    public sealed class IntentCapturingHandler
    {
        public DatabaseIntent? CapturedIntent { get; private set; }
        public bool? WasEnabled { get; private set; }

        public void CaptureCurrentContext()
        {
            CapturedIntent = DatabaseRoutingContext.CurrentIntent;
            WasEnabled = DatabaseRoutingContext.IsEnabled;
        }
    }

    #endregion

    #region DatabaseRoutingScope Tests

    [Fact]
    public void DatabaseRoutingScope_WithReadIntent_ShouldSetCorrectIntent()
    {

        // Arrange
        DatabaseRoutingContext.Clear();

        // Act
        using (var scope = new DatabaseRoutingScope(DatabaseIntent.Read))
        {
            // Assert - Inside scope
            DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);
            DatabaseRoutingContext.IsEnabled.ShouldBeTrue();
        }

        // Assert - After scope
        DatabaseRoutingContext.CurrentIntent.ShouldBeNull();
        DatabaseRoutingContext.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void DatabaseRoutingScope_WithWriteIntent_ShouldSetCorrectIntent()
    {

        // Arrange
        DatabaseRoutingContext.Clear();

        // Act
        using (var scope = new DatabaseRoutingScope(DatabaseIntent.Write))
        {
            // Assert - Inside scope
            DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Write);
            DatabaseRoutingContext.IsEnabled.ShouldBeTrue();
        }

        // Assert - After scope
        DatabaseRoutingContext.CurrentIntent.ShouldBeNull();
    }

    [Fact]
    public void DatabaseRoutingScope_WithForceWriteIntent_ShouldSetCorrectIntent()
    {

        // Arrange
        DatabaseRoutingContext.Clear();

        // Act
        using (var scope = new DatabaseRoutingScope(DatabaseIntent.ForceWrite))
        {
            // Assert - Inside scope
            DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.ForceWrite);
            DatabaseRoutingContext.IsEnabled.ShouldBeTrue();
        }

        // Assert - After scope
        DatabaseRoutingContext.CurrentIntent.ShouldBeNull();
    }

    [Fact]
    public void DatabaseRoutingScope_NestedScopes_ShouldRestorePreviousIntent()
    {

        // Arrange
        DatabaseRoutingContext.Clear();

        // Act & Assert
        using (var outerScope = new DatabaseRoutingScope(DatabaseIntent.Read))
        {
            DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);

            using (var innerScope = new DatabaseRoutingScope(DatabaseIntent.ForceWrite))
            {
                // Inner scope overrides
                DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.ForceWrite);
            }

            // Restored to outer scope
            DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);
        }

        // Restored to original state
        DatabaseRoutingContext.CurrentIntent.ShouldBeNull();
    }

    [Fact]
    public void DatabaseRoutingScope_ForRead_ShouldCreateReadScope()
    {

        // Arrange
        DatabaseRoutingContext.Clear();

        // Act
        using var scope = DatabaseRoutingScope.ForRead();

        // Assert
        scope.Intent.ShouldBe(DatabaseIntent.Read);
        DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);
    }

    [Fact]
    public void DatabaseRoutingScope_ForWrite_ShouldCreateWriteScope()
    {

        // Arrange
        DatabaseRoutingContext.Clear();

        // Act
        using var scope = DatabaseRoutingScope.ForWrite();

        // Assert
        scope.Intent.ShouldBe(DatabaseIntent.Write);
        DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Write);
    }

    [Fact]
    public void DatabaseRoutingScope_ForForceWrite_ShouldCreateForceWriteScope()
    {

        // Arrange
        DatabaseRoutingContext.Clear();

        // Act
        using var scope = DatabaseRoutingScope.ForForceWrite();

        // Assert
        scope.Intent.ShouldBe(DatabaseIntent.ForceWrite);
        DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.ForceWrite);
    }

    #endregion

    #region DatabaseRoutingContext Tests

    [Fact]
    public void DatabaseRoutingContext_IsReadIntent_ShouldReturnTrueOnlyForRead()
    {

        // Test Read
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;
        DatabaseRoutingContext.IsReadIntent.ShouldBeTrue();

        // Test Write
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Write;
        DatabaseRoutingContext.IsReadIntent.ShouldBeFalse();

        // Test ForceWrite
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.ForceWrite;
        DatabaseRoutingContext.IsReadIntent.ShouldBeFalse();

        // Test null
        DatabaseRoutingContext.CurrentIntent = null;
        DatabaseRoutingContext.IsReadIntent.ShouldBeFalse();
    }

    [Fact]
    public void DatabaseRoutingContext_IsWriteIntent_ShouldReturnTrueForWriteOrForceWriteOrNull()
    {

        // Test Write
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Write;
        DatabaseRoutingContext.IsWriteIntent.ShouldBeTrue();

        // Test ForceWrite
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.ForceWrite;
        DatabaseRoutingContext.IsWriteIntent.ShouldBeTrue();

        // Test null (defaults to write for safety)
        DatabaseRoutingContext.CurrentIntent = null;
        DatabaseRoutingContext.IsWriteIntent.ShouldBeTrue();

        // Test Read
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;
        DatabaseRoutingContext.IsWriteIntent.ShouldBeFalse();
    }

    [Fact]
    public void DatabaseRoutingContext_EffectiveIntent_ShouldDefaultToWrite()
    {

        // Test null defaults to Write
        DatabaseRoutingContext.CurrentIntent = null;
        DatabaseRoutingContext.EffectiveIntent.ShouldBe(DatabaseIntent.Write);

        // Test explicit values are preserved
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;
        DatabaseRoutingContext.EffectiveIntent.ShouldBe(DatabaseIntent.Read);

        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.ForceWrite;
        DatabaseRoutingContext.EffectiveIntent.ShouldBe(DatabaseIntent.ForceWrite);
    }

    [Fact]
    public void DatabaseRoutingContext_HasIntent_ShouldReturnTrueWhenSet()
    {

        // Test with no intent
        DatabaseRoutingContext.CurrentIntent = null;
        DatabaseRoutingContext.HasIntent.ShouldBeFalse();

        // Test with intent set
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;
        DatabaseRoutingContext.HasIntent.ShouldBeTrue();
    }

    [Fact]
    public void DatabaseRoutingContext_Clear_ShouldResetAllState()
    {

        // Arrange - Set state
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;
        DatabaseRoutingContext.IsEnabled = true;

        // Act
        DatabaseRoutingContext.Clear();

        // Assert
        DatabaseRoutingContext.CurrentIntent.ShouldBeNull();
        DatabaseRoutingContext.IsEnabled.ShouldBeFalse();
    }

    #endregion

    #region Pipeline Behavior Intent Determination Tests

    [Fact]
    public async Task PipelineBehavior_WithCommand_ShouldSetWriteIntent()
    {

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var behavior = CreatePipelineBehavior<TestCommand, string>(serviceProvider);
        var command = new TestCommand("test data");
        var context = CreateRequestContext();

        DatabaseIntent? capturedIntent = null;

        // Act
        await behavior.Handle(
            command,
            context,
            () =>
            {
                capturedIntent = DatabaseRoutingContext.CurrentIntent;
                return ValueTask.FromResult(Either<EncinaError, string>.Right("result"));
            },
            CancellationToken.None);

        // Assert
        capturedIntent.ShouldBe(DatabaseIntent.Write);
    }

    [Fact]
    public async Task PipelineBehavior_WithQuery_ShouldSetReadIntent()
    {

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var behavior = CreatePipelineBehavior<TestQuery, string>(serviceProvider);
        var query = new TestQuery("filter");
        var context = CreateRequestContext();

        DatabaseIntent? capturedIntent = null;

        // Act
        await behavior.Handle(
            query,
            context,
            () =>
            {
                capturedIntent = DatabaseRoutingContext.CurrentIntent;
                return ValueTask.FromResult(Either<EncinaError, string>.Right("result"));
            },
            CancellationToken.None);

        // Assert
        capturedIntent.ShouldBe(DatabaseIntent.Read);
    }

    [Fact]
    public async Task PipelineBehavior_WithForceWriteQuery_ShouldSetForceWriteIntent()
    {

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var behavior = CreatePipelineBehavior<TestForceWriteQuery, string>(serviceProvider);
        var query = new TestForceWriteQuery("filter");
        var context = CreateRequestContext();

        DatabaseIntent? capturedIntent = null;

        // Act
        await behavior.Handle(
            query,
            context,
            () =>
            {
                capturedIntent = DatabaseRoutingContext.CurrentIntent;
                return ValueTask.FromResult(Either<EncinaError, string>.Right("result"));
            },
            CancellationToken.None);

        // Assert
        capturedIntent.ShouldBe(DatabaseIntent.ForceWrite);
    }

    [Fact]
    public async Task PipelineBehavior_ShouldRestoreContextAfterExecution()
    {

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var behavior = CreatePipelineBehavior<TestQuery, string>(serviceProvider);
        var query = new TestQuery("filter");
        var context = CreateRequestContext();

        // Set initial state
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Write;
        var initialIntent = DatabaseRoutingContext.CurrentIntent;

        // Act
        await behavior.Handle(
            query,
            context,
            () => ValueTask.FromResult(Either<EncinaError, string>.Right("result")),
            CancellationToken.None);

        // Assert - Previous state should be restored
        DatabaseRoutingContext.CurrentIntent.ShouldBe(initialIntent);
    }

    [Fact]
    public async Task PipelineBehavior_ShouldRestoreContextEvenOnException()
    {

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var behavior = CreatePipelineBehavior<TestQuery, string>(serviceProvider);
        var query = new TestQuery("filter");
        var context = CreateRequestContext();

        // Set initial state
        DatabaseRoutingContext.Clear();

        // Act & Assert
        try
        {
            await behavior.Handle(
                query,
                context,
                () => throw new InvalidOperationException("Test exception"),
                CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Context should be restored even after exception
        // Note: The scope is disposed in finally block, restoring previous state
        DatabaseRoutingContext.CurrentIntent.ShouldBeNull();
    }

    #endregion

    #region Collection Factory Integration with Routing Context Tests

    [Fact]
    public async Task CollectionFactory_WithReadContext_ShouldUseConfiguredReadPreference()
    {

        // Arrange
        var factory = CreateCollectionFactory();
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;

        // Act
        var collection = await factory.GetCollectionAsync<BsonDocument>("test");

        // Assert - Should use configured read preference (SecondaryPreferred)
        collection.Settings.ReadPreference.ReadPreferenceMode
            .ShouldBe(ReadPreferenceMode.SecondaryPreferred);
    }

    [Fact]
    public async Task CollectionFactory_WithWriteContext_ShouldUsePrimaryReadPreference()
    {

        // Arrange
        var factory = CreateCollectionFactory();
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Write;

        // Act
        var collection = await factory.GetCollectionAsync<BsonDocument>("test");

        // Assert - Should use Primary
        collection.Settings.ReadPreference.ShouldBe(ReadPreference.Primary);
    }

    [Fact]
    public async Task CollectionFactory_WithForceWriteContext_ShouldUsePrimaryReadPreference()
    {

        // Arrange
        var factory = CreateCollectionFactory();
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.ForceWrite;

        // Act
        var collection = await factory.GetCollectionAsync<BsonDocument>("test");

        // Assert - Should use Primary
        collection.Settings.ReadPreference.ShouldBe(ReadPreference.Primary);
    }

    #endregion

    #region Async Context Flow Tests

    [Fact]
    public async Task DatabaseRoutingContext_ShouldFlowAcrossAsyncBoundaries()
    {

        // Arrange
        DatabaseRoutingContext.Clear();
        DatabaseIntent? capturedIntent = null;

        // Act
        using (var scope = new DatabaseRoutingScope(DatabaseIntent.Read))
        {
            await Task.Yield(); // Cross async boundary
            capturedIntent = DatabaseRoutingContext.CurrentIntent;
        }

        // Assert
        capturedIntent.ShouldBe(DatabaseIntent.Read);
    }

    [Fact]
    public async Task DatabaseRoutingContext_ShouldIsolateAcrossTasks()
    {

        // Arrange
        DatabaseRoutingContext.Clear();
        var task1Intent = new TaskCompletionSource<DatabaseIntent?>();
        var task2Intent = new TaskCompletionSource<DatabaseIntent?>();

        // Act - Start two concurrent tasks with different intents
        var task1 = Task.Run(async () =>
        {
            using var scope = new DatabaseRoutingScope(DatabaseIntent.Read);
            await Task.Delay(50); // Small delay to ensure overlap
            task1Intent.SetResult(DatabaseRoutingContext.CurrentIntent);
        });

        var task2 = Task.Run(async () =>
        {
            using var scope = new DatabaseRoutingScope(DatabaseIntent.Write);
            await Task.Delay(50);
            task2Intent.SetResult(DatabaseRoutingContext.CurrentIntent);
        });

        await Task.WhenAll(task1, task2);

        // Assert - Each task should have its own intent
        (await task1Intent.Task).ShouldBe(DatabaseIntent.Read);
        (await task2Intent.Task).ShouldBe(DatabaseIntent.Write);
    }

    #endregion

    #region ForceWriteDatabaseAttribute Tests

    [Fact]
    public void ForceWriteDatabaseAttribute_ShouldBeDetectedOnQueryClass()
    {

        // Arrange
        var queryType = typeof(TestForceWriteQuery);

        // Act
        var hasAttribute = queryType
            .GetCustomAttributes(typeof(ForceWriteDatabaseAttribute), inherit: true)
            .Length > 0;

        // Assert
        hasAttribute.ShouldBeTrue();
    }

    [Fact]
    public void ForceWriteDatabaseAttribute_ShouldNotBeOnRegularQuery()
    {

        // Arrange
        var queryType = typeof(TestQuery);

        // Act
        var hasAttribute = queryType
            .GetCustomAttributes(typeof(ForceWriteDatabaseAttribute), inherit: true)
            .Length > 0;

        // Assert
        hasAttribute.ShouldBeFalse();
    }

    [Fact]
    public void ForceWriteDatabaseAttribute_Reason_ShouldBeAccessible()
    {

        // Arrange
        var queryType = typeof(TestForceWriteQuery);

        // Act
        var attribute = (ForceWriteDatabaseAttribute?)queryType
            .GetCustomAttributes(typeof(ForceWriteDatabaseAttribute), inherit: true)
            .FirstOrDefault();

        // Assert
        attribute.ShouldNotBeNull();
        attribute!.Reason.ShouldBe("Test - requires read-after-write consistency");
    }

    #endregion

    #region Helper Methods

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddSingleton(_fixture.Client!);

        var options = new EncinaMongoDbOptions
        {
            ConnectionString = _fixture.ConnectionString,
            DatabaseName = MongoDbReplicaSetFixture.DatabaseName,
            UseReadWriteSeparation = true
        };
        services.Configure<EncinaMongoDbOptions>(opt =>
        {
            opt.ConnectionString = options.ConnectionString;
            opt.DatabaseName = options.DatabaseName;
            opt.UseReadWriteSeparation = true;
        });

        return services.BuildServiceProvider();
    }

    private static ReadWriteRoutingPipelineBehavior<TRequest, TResponse> CreatePipelineBehavior<TRequest, TResponse>(
        IServiceProvider serviceProvider)
        where TRequest : IRequest<TResponse>
    {
        var logger = serviceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<ReadWriteRoutingPipelineBehavior<TRequest, TResponse>>();

        return new ReadWriteRoutingPipelineBehavior<TRequest, TResponse>(logger);
    }

    private static TestRequestContext CreateRequestContext()
    {
        return new TestRequestContext();
    }

    private ReadWriteMongoCollectionFactory CreateCollectionFactory()
    {
        var options = new EncinaMongoDbOptions
        {
            ConnectionString = _fixture.ConnectionString,
            DatabaseName = MongoDbReplicaSetFixture.DatabaseName,
            UseReadWriteSeparation = true
        };

        return new ReadWriteMongoCollectionFactory(
            _fixture.Client!,
            Options.Create(options));
    }

    /// <summary>
    /// Simple test implementation of IRequestContext.
    /// </summary>
    private sealed class TestRequestContext : IRequestContext
    {
        public string CorrelationId { get; } = Guid.NewGuid().ToString();
        public string? UserId { get; private set; } = "test-user";
        public string? IdempotencyKey { get; private set; }
        public string? TenantId { get; private set; }
        public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
        public IReadOnlyDictionary<string, object?> Metadata { get; private set; } =
            new Dictionary<string, object?>();

        public IRequestContext WithMetadata(string key, object? value)
        {
            var newMetadata = new Dictionary<string, object?>(Metadata) { [key] = value };
            return new TestRequestContext { Metadata = newMetadata };
        }

        public IRequestContext WithUserId(string? userId) =>
            new TestRequestContext { UserId = userId };

        public IRequestContext WithIdempotencyKey(string? idempotencyKey) =>
            new TestRequestContext { IdempotencyKey = idempotencyKey };

        public IRequestContext WithTenantId(string? tenantId) =>
            new TestRequestContext { TenantId = tenantId };
    }

    #endregion
}
