using Encina.MongoDB;
using Encina.MongoDB.Sagas;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Stores;

/// <summary>
/// Integration tests for <see cref="SagaStoreMongoDB"/> using real MongoDB via Testcontainers.
/// </summary>
[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class SagaStoreMongoDBIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private readonly IOptions<EncinaMongoDbOptions> _options;

    public SagaStoreMongoDBIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _options = Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = MongoDbFixture.DatabaseName,
            UseSagas = true
        });
    }

    public async Task InitializeAsync()
    {
        if (_fixture.IsAvailable)
        {
            var collection = _fixture.Database!.GetCollection<SagaState>(_options.Value.Collections.Sagas);
            await collection.DeleteManyAsync(Builders<SagaState>.Filter.Empty);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [SkippableFact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistSaga()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var store = CreateStore();
        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "OrderSaga",
            Data = "{\"orderId\":123}",
            Status = "Running",
            CurrentStep = 0,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        // Act
        await store.AddAsync(saga);

        // Assert
        var collection = GetCollection();
        var stored = await collection.Find(s => s.SagaId == saga.SagaId).FirstOrDefaultAsync();
        stored.ShouldNotBeNull();
        stored.SagaType.ShouldBe("OrderSaga");
        stored.Data.ShouldBe("{\"orderId\":123}");
        stored.Status.ShouldBe("Running");
    }

    [SkippableFact]
    public async Task GetAsync_ExistingSagaId_ShouldReturnSaga()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var collection = GetCollection();
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{}",
            Status = "Running",
            CurrentStep = 1,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        await collection.InsertOneAsync(saga);

        var store = CreateStore();

        // Act
        var result = await store.GetAsync(sagaId);

        // Assert
        result.ShouldNotBeNull();
        result!.SagaId.ShouldBe(sagaId);
        result.SagaType.ShouldBe("TestSaga");
    }

    [SkippableFact]
    public async Task GetAsync_NonExistentSagaId_ShouldReturnNull()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [SkippableFact]
    public async Task UpdateAsync_ModifyState_ShouldPersistChanges()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var collection = GetCollection();
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{\"step\":1}",
            Status = "Running",
            CurrentStep = 1,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        await collection.InsertOneAsync(saga);

        var store = CreateStore();

        // Act
        saga.Data = "{\"step\":2}";
        saga.CurrentStep = 2;
        await store.UpdateAsync(saga);

        // Assert
        var updated = await collection.Find(s => s.SagaId == sagaId).FirstOrDefaultAsync();
        updated!.Data.ShouldBe("{\"step\":2}");
        updated.CurrentStep.ShouldBe(2);
    }

    [SkippableFact]
    public async Task UpdateAsync_ToCompletedStatus_ShouldSetCompletedAtUtc()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var collection = GetCollection();
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{}",
            Status = "Running",
            CurrentStep = 3,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        await collection.InsertOneAsync(saga);

        var store = CreateStore();

        // Act
        saga.Status = "Completed";
        saga.CompletedAtUtc = DateTime.UtcNow;
        await store.UpdateAsync(saga);

        // Assert
        var updated = await collection.Find(s => s.SagaId == sagaId).FirstOrDefaultAsync();
        updated!.Status.ShouldBe("Completed");
        updated.CompletedAtUtc.ShouldNotBeNull();
    }

    [SkippableFact]
    public async Task UpdateAsync_ToFailedStatus_ShouldSetErrorMessage()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var collection = GetCollection();
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{}",
            Status = "Running",
            CurrentStep = 2,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        await collection.InsertOneAsync(saga);

        var store = CreateStore();

        // Act
        saga.Status = "Failed";
        saga.ErrorMessage = "Test error occurred";
        saga.CompletedAtUtc = DateTime.UtcNow;
        await store.UpdateAsync(saga);

        // Assert
        var updated = await collection.Find(s => s.SagaId == sagaId).FirstOrDefaultAsync();
        updated!.Status.ShouldBe("Failed");
        updated.ErrorMessage.ShouldBe("Test error occurred");
        updated.CompletedAtUtc.ShouldNotBeNull();
    }

    [SkippableFact]
    public async Task GetStuckSagasAsync_ShouldReturnOldUncompletedSagas()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var collection = GetCollection();

        var stuckSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "StuckSaga",
            Data = "{}",
            Status = "Running",
            CurrentStep = 1,
            StartedAtUtc = DateTime.UtcNow.AddHours(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2)
        };

        var activeSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "ActiveSaga",
            Data = "{}",
            Status = "Running",
            CurrentStep = 1,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        var completedSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "CompletedSaga",
            Data = "{}",
            Status = "Completed",
            CurrentStep = 3,
            StartedAtUtc = DateTime.UtcNow.AddHours(-3),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-3),
            CompletedAtUtc = DateTime.UtcNow.AddHours(-3)
        };

        await collection.InsertManyAsync([stuckSaga, activeSaga, completedSaga]);

        var store = CreateStore();

        // Act
        var stuckSagas = await store.GetStuckSagasAsync(TimeSpan.FromHours(1), batchSize: 10);

        // Assert
        var sagaList = stuckSagas.ToList();
        sagaList.ShouldHaveSingleItem();
        sagaList.First().SagaId.ShouldBe(stuckSaga.SagaId);
    }

    [SkippableFact]
    public async Task GetExpiredSagasAsync_ShouldReturnExpiredActiveSagas()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var collection = GetCollection();

        var expiredSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "ExpiredSaga",
            Data = "{}",
            Status = "Running",
            CurrentStep = 1,
            StartedAtUtc = DateTime.UtcNow.AddHours(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2),
            TimeoutAtUtc = DateTime.UtcNow.AddHours(-1)
        };

        var notExpiredSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "NotExpiredSaga",
            Data = "{}",
            Status = "Running",
            CurrentStep = 1,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            TimeoutAtUtc = DateTime.UtcNow.AddHours(1)
        };

        var noTimeoutSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "NoTimeoutSaga",
            Data = "{}",
            Status = "Running",
            CurrentStep = 1,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            TimeoutAtUtc = null
        };

        await collection.InsertManyAsync([expiredSaga, notExpiredSaga, noTimeoutSaga]);

        var store = CreateStore();

        // Act
        var expiredSagas = await store.GetExpiredSagasAsync(batchSize: 10);

        // Assert
        var sagaList = expiredSagas.ToList();
        sagaList.ShouldHaveSingleItem();
        sagaList.First().SagaId.ShouldBe(expiredSaga.SagaId);
    }

    [SkippableFact]
    public async Task StatusTransition_RunningToCompleted_ShouldWork()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var collection = GetCollection();
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{}",
            Status = "Running",
            CurrentStep = 0,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        await collection.InsertOneAsync(saga);

        var store = CreateStore();

        // Act
        saga.Status = "Completed";
        saga.CurrentStep = 3;
        saga.CompletedAtUtc = DateTime.UtcNow;
        await store.UpdateAsync(saga);

        // Assert
        var updated = await collection.Find(s => s.SagaId == sagaId).FirstOrDefaultAsync();
        updated!.Status.ShouldBe("Completed");
        updated.CompletedAtUtc.ShouldNotBeNull();
    }

    [SkippableFact]
    public async Task StatusTransition_RunningToCompensatingToCompensated_ShouldWork()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var collection = GetCollection();
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{}",
            Status = "Running",
            CurrentStep = 2,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        await collection.InsertOneAsync(saga);

        var store = CreateStore();

        // Act - Transition to Compensating
        saga.Status = "Compensating";
        await store.UpdateAsync(saga);

        var compensating = await collection.Find(s => s.SagaId == sagaId).FirstOrDefaultAsync();
        compensating!.Status.ShouldBe("Compensating");

        // Act - Transition to Compensated
        saga.Status = "Compensated";
        saga.CompletedAtUtc = DateTime.UtcNow;
        await store.UpdateAsync(saga);

        // Assert
        var compensated = await collection.Find(s => s.SagaId == sagaId).FirstOrDefaultAsync();
        compensated!.Status.ShouldBe("Compensated");
        compensated.CompletedAtUtc.ShouldNotBeNull();
    }

    [SkippableFact]
    public async Task StatusTransition_RunningToFailed_ShouldWork()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var collection = GetCollection();
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{}",
            Status = "Running",
            CurrentStep = 1,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        await collection.InsertOneAsync(saga);

        var store = CreateStore();

        // Act
        saga.Status = "Failed";
        saga.ErrorMessage = "Critical failure";
        saga.CompletedAtUtc = DateTime.UtcNow;
        await store.UpdateAsync(saga);

        // Assert
        var failed = await collection.Find(s => s.SagaId == sagaId).FirstOrDefaultAsync();
        failed!.Status.ShouldBe("Failed");
        failed.ErrorMessage.ShouldBe("Critical failure");
        failed.CompletedAtUtc.ShouldNotBeNull();
    }

    [SkippableFact]
    public async Task ConcurrentSagaCreation_ShouldNotCorruptData()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange & Act
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            var store = CreateStore();
            var sagaId = Guid.NewGuid();

            var saga = new SagaState
            {
                SagaId = sagaId,
                SagaType = $"ConcurrentSaga{i}",
                Data = $"{{\"index\":{i}}}",
                Status = "Running",
                CurrentStep = 0,
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow
            };

            await store.AddAsync(saga);
            return sagaId;
        });

        var sagaIds = await Task.WhenAll(tasks);

        // Assert
        var collection = GetCollection();
        foreach (var id in sagaIds)
        {
            var stored = await collection.Find(s => s.SagaId == id).FirstOrDefaultAsync();
            stored.ShouldNotBeNull();
        }
    }

    private SagaStoreMongoDB CreateStore()
    {
        return new SagaStoreMongoDB(
            _fixture.Client!,
            _options,
            NullLogger<SagaStoreMongoDB>.Instance);
    }

    private IMongoCollection<SagaState> GetCollection()
    {
        return _fixture.Database!.GetCollection<SagaState>(_options.Value.Collections.Sagas);
    }
}
