using System.Data;
using Encina.ADO.SqlServer.Anonymization;
using Encina.ADO.SqlServer.Auditing;
using Encina.ADO.SqlServer.ProcessingActivity;
using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;
using Encina.Compliance.GDPR;
using Encina.Dapper.SqlServer.Anonymization;
using Encina.Dapper.SqlServer.Auditing;
using Encina.Dapper.SqlServer.ProcessingActivity;
using Encina.EntityFrameworkCore.Anonymization;
using Encina.EntityFrameworkCore.Auditing;
using Encina.EntityFrameworkCore.ProcessingActivity;
using Encina.MongoDB;
using Encina.MongoDB.Anonymization;
using Encina.MongoDB.Auditing;
using Encina.MongoDB.ProcessingActivity;
using Encina.Security.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Encina.UnitTests.Compliance.StoreFailures;

/// <summary>
/// Proves that a failing operation audit, read audit, token mapping or processing activity store of every provider
/// family (ADO.NET, Dapper, EF Core, MongoDB) returns an error that carries the exception and its type name but never
/// the exception's message text (#1322, #2445).
/// </summary>
[Trait("Category", "Unit")]
public sealed class StoreFailureDoesNotLeakExceptionMessageTests
{
    private const string Sentinel = "SECRET-42";

    /// <summary>A connection string that fails fast (refused connection) without any server.</summary>
    private const string UnreachableSqlServer = "Server=127.0.0.1,1;Database=x;User Id=u;Password=p;Connect Timeout=1;Encrypt=False";

    private static InvalidOperationException Failure() => new(Sentinel);

    #region ADO.NET

    [Fact]
    public async Task OperationAuditStoreADO_WhenTheConnectionFails_DoesNotLeakTheExceptionMessage()
    {
        var store = new OperationAuditStoreADO(FailingConnection());

        var result = await store.RecordAsync(AuditEntry());

        AssertNoLeak(result, OperationAuditErrors.StoreErrorCode);
    }

    [Fact]
    public async Task ReadAuditStoreADO_WhenTheConnectionFails_DoesNotLeakTheExceptionMessage()
    {
        var store = new ReadAuditStoreADO(FailingConnection());

        var result = await store.LogReadAsync(ReadEntry());

        AssertNoLeak(result, ReadAuditErrors.StoreErrorCode);
    }

    [Fact]
    public async Task TokenMappingStoreADO_WhenTheConnectionFails_DoesNotLeakTheExceptionMessage()
    {
        var store = new TokenMappingStoreADO(FailingConnection());

        var result = await store.GetByTokenAsync("token");

        AssertNoLeak(result, AnonymizationErrors.StoreErrorCode);
    }

    [Fact]
    public async Task ProcessingActivityRegistryADO_WhenTheDatabaseIsUnreachable_DoesNotLeakTheExceptionMessage()
    {
        var registry = new ProcessingActivityRegistryADO(UnreachableSqlServer);

        var result = await registry.GetAllActivitiesAsync();

        AssertNoLeak(result, GDPRErrors.ProcessingActivityStoreErrorCode);
    }

    #endregion

    #region Dapper

    [Fact]
    public async Task OperationAuditStoreDapper_WhenTheConnectionFails_DoesNotLeakTheExceptionMessage()
    {
        var store = new OperationAuditStoreDapper(FailingConnection());

        var result = await store.RecordAsync(AuditEntry());

        AssertNoLeak(result, OperationAuditErrors.StoreErrorCode);
    }

    [Fact]
    public async Task ReadAuditStoreDapper_WhenTheConnectionFails_DoesNotLeakTheExceptionMessage()
    {
        var store = new ReadAuditStoreDapper(FailingConnection());

        var result = await store.LogReadAsync(ReadEntry());

        AssertNoLeak(result, ReadAuditErrors.StoreErrorCode);
    }

    [Fact]
    public async Task TokenMappingStoreDapper_WhenTheConnectionFails_DoesNotLeakTheExceptionMessage()
    {
        var store = new TokenMappingStoreDapper(FailingConnection());

        var result = await store.GetByTokenAsync("token");

        AssertNoLeak(result, AnonymizationErrors.StoreErrorCode);
    }

    [Fact]
    public async Task ProcessingActivityRegistryDapper_WhenTheDatabaseIsUnreachable_DoesNotLeakTheExceptionMessage()
    {
        var registry = new ProcessingActivityRegistryDapper(UnreachableSqlServer);

        var result = await registry.GetAllActivitiesAsync();

        AssertNoLeak(result, GDPRErrors.ProcessingActivityStoreErrorCode);
    }

    #endregion

    #region EF Core

    [Fact]
    public async Task OperationAuditStoreEF_WhenTheSaveFails_DoesNotLeakTheExceptionMessage()
    {
        var context = Substitute.For<DbContext>();
        context.Set<OperationAuditEntryEntity>().Returns(Substitute.For<DbSet<OperationAuditEntryEntity>>());
        context.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException(Sentinel, new InvalidOperationException(Sentinel)));

        var result = await new OperationAuditStoreEF(context).RecordAsync(AuditEntry());

        AssertNoLeak(result, OperationAuditErrors.StoreErrorCode);
    }

    [Fact]
    public async Task ReadAuditStoreEF_WhenTheSaveFails_DoesNotLeakTheExceptionMessage()
    {
        var context = Substitute.For<DbContext>();
        context.Set<ReadAuditEntryEntity>().Returns(Substitute.For<DbSet<ReadAuditEntryEntity>>());
        context.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException(Sentinel, new InvalidOperationException(Sentinel)));

        var result = await new ReadAuditStoreEF(context).LogReadAsync(ReadEntry());

        AssertNoLeak(result, ReadAuditErrors.StoreErrorCode);
    }

    [Fact]
    public async Task TokenMappingStoreEF_WhenTheContextFails_DoesNotLeakTheExceptionMessage()
    {
        var context = Substitute.For<DbContext>();
        context.Set<TokenMappingEntity>().Returns(_ => throw Failure());

        var result = await new TokenMappingStoreEF(context).GetByTokenAsync("token");

        AssertNoLeak(result, AnonymizationErrors.StoreErrorCode);
    }

    [Fact]
    public async Task ProcessingActivityRegistryEF_WhenTheContextFails_DoesNotLeakTheExceptionMessage()
    {
        var context = Substitute.For<DbContext>();
        context.Set<ProcessingActivityEntity>().Returns(_ => throw Failure());

        var result = await new ProcessingActivityRegistryEF(context).GetAllActivitiesAsync();

        AssertNoLeak(result, GDPRErrors.ProcessingActivityStoreErrorCode);
    }

    [Fact]
    public async Task ProcessingActivityRegistryEF_WhenCancelled_ReturnsTheCancellationAsAttachedException()
    {
        var context = Substitute.For<DbContext>();
        context.Set<ProcessingActivityEntity>().Returns(Substitute.For<DbSet<ProcessingActivityEntity>>());
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException(Sentinel));

        var result = await new ProcessingActivityRegistryEF(context).RegisterActivityAsync(Activity());

        AssertNoLeak(result, GDPRErrors.ProcessingActivityStoreErrorCode);
    }

    #endregion

    #region MongoDB

    [Fact]
    public async Task OperationAuditStoreMongoDB_WhenTheInsertFails_DoesNotLeakTheExceptionMessage()
    {
        var collection = Substitute.For<IMongoCollection<OperationAuditEntryDocument>>();
        collection.InsertOneAsync(Arg.Any<OperationAuditEntryDocument>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(Failure());
        var store = new OperationAuditStoreMongoDB(ClientFor(collection), Options.Create(new EncinaMongoDbOptions()), NullLogger<OperationAuditStoreMongoDB>.Instance);

        var result = await store.RecordAsync(AuditEntry());

        AssertNoLeak(result, OperationAuditErrors.StoreErrorCode);
    }

    [Fact]
    public async Task ReadAuditStoreMongoDB_WhenTheInsertFails_DoesNotLeakTheExceptionMessage()
    {
        var collection = Substitute.For<IMongoCollection<ReadAuditEntryDocument>>();
        collection.InsertOneAsync(Arg.Any<ReadAuditEntryDocument>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(Failure());
        var store = new ReadAuditStoreMongoDB(ClientFor(collection), Options.Create(new EncinaMongoDbOptions()), NullLogger<ReadAuditStoreMongoDB>.Instance);

        var result = await store.LogReadAsync(ReadEntry());

        AssertNoLeak(result, ReadAuditErrors.StoreErrorCode);
    }

    [Fact]
    public async Task TokenMappingStoreMongoDB_WhenTheReplaceFails_DoesNotLeakTheExceptionMessage()
    {
        var collection = Substitute.For<IMongoCollection<TokenMappingDocument>>();
        collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<TokenMappingDocument>>(),
                Arg.Any<TokenMappingDocument>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(Failure());
        var store = new TokenMappingStoreMongoDB(ClientFor(collection), Options.Create(new EncinaMongoDbOptions()), NullLogger<TokenMappingStoreMongoDB>.Instance);

        var result = await store.StoreAsync(TokenMappingFactory());

        AssertNoLeak(result, AnonymizationErrors.StoreErrorCode);
    }

    // ProcessingActivityRegistryMongoDB has no unit test here: its constructor builds the MongoClient and creates
    // its indexes eagerly, so it cannot be constructed without a reachable server, and it exposes no seam to inject
    // a failing collection. Its four catch sites have the same shape as the three MongoDB stores tested above.

    #endregion

    private static void AssertNoLeak<T>(LanguageExt.Either<EncinaError, T> result, string expectedCode)
    {
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Single();
        error.GetCode().IfNone(string.Empty).ShouldBe(expectedCode);
        error.Exception.IsSome.ShouldBeTrue();
        var exception = error.Exception.IfNone(() => throw new InvalidOperationException("missing exception"));
        exception.Message.ShouldNotBeNullOrWhiteSpace();
        error.Message.ShouldNotContain(exception.Message);
        error.Message.ShouldNotContain(Sentinel);
        error.Message.ShouldContain(exception.GetType().Name);
    }

    private static IDbConnection FailingConnection()
    {
        var connection = Substitute.For<IDbConnection>();
        connection.State.Returns(ConnectionState.Closed);
        connection.CreateCommand().Returns(_ => throw Failure());
        connection.When(c => c.Open()).Do(_ => throw Failure());
        return connection;
    }

    private static IMongoClient ClientFor<TDocument>(IMongoCollection<TDocument> collection)
    {
        var database = Substitute.For<IMongoDatabase>();
        database.GetCollection<TDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>()).Returns(collection);
        var client = Substitute.For<IMongoClient>();
        client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(database);
        return client;
    }

    private static OperationAuditEntry AuditEntry()
    {
        var now = DateTimeOffset.UnixEpoch.AddYears(56);
        return new OperationAuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "corr-1322",
            UserId = "user-1322",
            Action = "Create",
            EntityType = "Order",
            EntityId = "order-1",
            Outcome = AuditOutcome.Success,
            TimestampUtc = now.UtcDateTime,
            StartedAtUtc = now,
            CompletedAtUtc = now.AddMilliseconds(10),
            Metadata = new Dictionary<string, object?>()
        };
    }

    private static ReadAuditEntry ReadEntry() => new()
    {
        Id = Guid.NewGuid(),
        EntityType = "Patient",
        EntityId = "patient-1",
        UserId = "user-1322",
        AccessedAtUtc = DateTimeOffset.UnixEpoch.AddYears(56),
        AccessMethod = ReadAccessMethod.Repository,
        EntityCount = 1
    };

    private static TokenMapping TokenMappingFactory() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Token = "token",
        OriginalValueHash = "hash",
        EncryptedOriginalValue = [1, 2, 3],
        KeyId = "key-1",
        CreatedAtUtc = DateTimeOffset.UnixEpoch.AddYears(56)
    };

    private static ProcessingActivity Activity() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Activity",
        Purpose = "Testing",
        LawfulBasis = LawfulBasis.Contract,
        CategoriesOfDataSubjects = ["Customers"],
        CategoriesOfPersonalData = ["Name"],
        Recipients = ["Internal"],
        RetentionPeriod = TimeSpan.FromDays(30),
        SecurityMeasures = "Encryption",
        RequestType = typeof(string),
        CreatedAtUtc = DateTimeOffset.UnixEpoch.AddYears(56),
        LastUpdatedAtUtc = DateTimeOffset.UnixEpoch.AddYears(56)
    };
}
