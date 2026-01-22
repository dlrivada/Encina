using System.Diagnostics;
using Encina.MongoDB.BulkOperations;
using Encina.TestInfrastructure.Fixtures;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.BulkOperations;

/// <summary>
/// Performance comparison tests for MongoDB Bulk Operations vs standard loop operations.
/// These tests measure actual performance differences using real MongoDB via Testcontainers.
/// </summary>
[Trait("Category", "Performance")]
[Trait("Provider", "MongoDB")]
[Trait("Feature", "BulkOperations")]
[Collection(MongoDbTestCollection.Name)]
public sealed class BulkOperationsMongoDBPerformanceTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private readonly ITestOutputHelper _output;
    private IMongoCollection<MongoPerformanceEntity>? _collection;
    private BulkOperationsMongoDB<MongoPerformanceEntity, string>? _bulkOps;

    public BulkOperationsMongoDBPerformanceTests(MongoDbFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public Task InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            _output.WriteLine("MongoDB container not available - skipping tests");
            return Task.CompletedTask;
        }

        _collection = _fixture.Database!.GetCollection<MongoPerformanceEntity>("PerformanceEntities");
        _bulkOps = new BulkOperationsMongoDB<MongoPerformanceEntity, string>(_collection, e => e.Id);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_fixture.IsAvailable && _fixture.Database is not null)
        {
            await _fixture.Database.DropCollectionAsync("PerformanceEntities");
        }
    }

    private static List<MongoPerformanceEntity> CreateEntities(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new MongoPerformanceEntity
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = $"Entity_{i:D6}",
                Amount = (i + 1) * 10.50m,
                IsActive = i % 2 == 0,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();
    }

    [Fact]
    public async Task PerformanceSummary_AllOperations()
    {
        if (!_fixture.IsAvailable || _collection is null || _bulkOps is null)
        {
            _output.WriteLine("MongoDB container not available - skipping test");
            return;
        }

        const int entityCount = 1000;

        _output.WriteLine("===========================================");
        _output.WriteLine("MONGODB BULK OPERATIONS PERFORMANCE SUMMARY");
        _output.WriteLine($"Entity Count: {entityCount:N0}");
        _output.WriteLine("===========================================");
        _output.WriteLine("");

        // Clear collection
        await _collection.DeleteManyAsync(Builders<MongoPerformanceEntity>.Filter.Empty);

        // Warm up
        var warmupEntities = CreateEntities(10);
        await _bulkOps.BulkInsertAsync(warmupEntities);
        await _collection.DeleteManyAsync(Builders<MongoPerformanceEntity>.Filter.Empty);

        // INSERT
        var insertEntities = CreateEntities(entityCount);

        var bulkInsertSw = Stopwatch.StartNew();
        var bulkInsertResult = await _bulkOps.BulkInsertAsync(insertEntities);
        bulkInsertSw.Stop();

        if (bulkInsertResult.IsLeft)
        {
            bulkInsertResult.IfLeft(e => _output.WriteLine($"Bulk insert failed: {e.Message}"));
            return;
        }

        await _collection.DeleteManyAsync(Builders<MongoPerformanceEntity>.Filter.Empty);

        // Loop insert
        var loopInsertSw = Stopwatch.StartNew();
        foreach (var entity in insertEntities)
        {
            await _collection.InsertOneAsync(entity);
        }
        loopInsertSw.Stop();

        var insertImprovement = (double)loopInsertSw.ElapsedMilliseconds / Math.Max(1, bulkInsertSw.ElapsedMilliseconds);

        _output.WriteLine($"INSERT:");
        _output.WriteLine($"  Bulk:  {bulkInsertSw.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Loop:  {loopInsertSw.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Improvement: {insertImprovement:F1}x");
        _output.WriteLine("");

        // UPDATE
        await _collection.DeleteManyAsync(Builders<MongoPerformanceEntity>.Filter.Empty);
        var updateEntities = CreateEntities(entityCount);
        await _bulkOps.BulkInsertAsync(updateEntities);

        // Modify for bulk update
        foreach (var entity in updateEntities)
        {
            entity.Name = $"BulkUpdated_{entity.Name}";
            entity.Amount += 100;
        }

        var bulkUpdateSw = Stopwatch.StartNew();
        var bulkUpdateResult = await _bulkOps.BulkUpdateAsync(updateEntities);
        bulkUpdateSw.Stop();

        if (bulkUpdateResult.IsLeft)
        {
            bulkUpdateResult.IfLeft(e => _output.WriteLine($"Bulk update failed: {e.Message}"));
        }
        else
        {
            // Modify for loop update
            foreach (var entity in updateEntities)
            {
                entity.Name = $"LoopUpdated_{entity.Name}";
            }

            var loopUpdateSw = Stopwatch.StartNew();
            foreach (var entity in updateEntities)
            {
                var filter = Builders<MongoPerformanceEntity>.Filter.Eq(e => e.Id, entity.Id);
                await _collection.ReplaceOneAsync(filter, entity);
            }
            loopUpdateSw.Stop();

            var updateImprovement = (double)loopUpdateSw.ElapsedMilliseconds / Math.Max(1, bulkUpdateSw.ElapsedMilliseconds);

            _output.WriteLine($"UPDATE:");
            _output.WriteLine($"  Bulk:  {bulkUpdateSw.ElapsedMilliseconds:N0} ms");
            _output.WriteLine($"  Loop:  {loopUpdateSw.ElapsedMilliseconds:N0} ms");
            _output.WriteLine($"  Improvement: {updateImprovement:F1}x");
            _output.WriteLine("");
        }

        // DELETE
        var bulkDeleteSw = Stopwatch.StartNew();
        var bulkDeleteResult = await _bulkOps.BulkDeleteAsync(updateEntities);
        bulkDeleteSw.Stop();

        if (bulkDeleteResult.IsLeft)
        {
            bulkDeleteResult.IfLeft(e => _output.WriteLine($"Bulk delete failed: {e.Message}"));
        }
        else
        {
            // Re-insert for loop delete
            await _bulkOps.BulkInsertAsync(updateEntities);

            var loopDeleteSw = Stopwatch.StartNew();
            foreach (var entity in updateEntities)
            {
                var filter = Builders<MongoPerformanceEntity>.Filter.Eq(e => e.Id, entity.Id);
                await _collection.DeleteOneAsync(filter);
            }
            loopDeleteSw.Stop();

            var deleteImprovement = (double)loopDeleteSw.ElapsedMilliseconds / Math.Max(1, bulkDeleteSw.ElapsedMilliseconds);

            _output.WriteLine($"DELETE:");
            _output.WriteLine($"  Bulk:  {bulkDeleteSw.ElapsedMilliseconds:N0} ms");
            _output.WriteLine($"  Loop:  {loopDeleteSw.ElapsedMilliseconds:N0} ms");
            _output.WriteLine($"  Improvement: {deleteImprovement:F1}x");
            _output.WriteLine("");
        }

        _output.WriteLine("===========================================");
        _output.WriteLine("These numbers can be used to update documentation");
        _output.WriteLine("===========================================");
    }
}

#region Test Entity

/// <summary>
/// Test entity for MongoDB bulk operations performance tests.
/// </summary>
public class MongoPerformanceEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

#endregion
