using System.Net;
using System.Reflection;
using Encina.Compliance.GDPR;
using Encina.MongoDB.ProcessingActivity;
using LanguageExt;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Activity = Encina.Compliance.GDPR.ProcessingActivity;

namespace Encina.UnitTests.MongoDB.ProcessingActivity;

/// <summary>
/// Unit tests for <see cref="ProcessingActivityRegistryMongoDB"/> through its internal collection seam. The public
/// constructor (client and unique index creation) needs a server and is covered by the integration tests.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ProcessingActivityRegistryMongoDBTests
{
    private static readonly DateTimeOffset Stamp = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IMongoCollection<ProcessingActivityDocument> _collection =
        Substitute.For<IMongoCollection<ProcessingActivityDocument>>();

    private readonly ProcessingActivityRegistryMongoDB _registry;

    public ProcessingActivityRegistryMongoDBTests()
    {
        _registry = new ProcessingActivityRegistryMongoDB(_collection);
    }

    #region Constructor

    [Fact]
    public void Constructor_WithCollection_DoesNotCreateIndexes()
    {
        _collection.ReceivedCalls().ShouldBeEmpty();
    }

    #endregion

    #region RegisterActivityAsync

    [Fact]
    public async Task RegisterActivityAsync_Valid_InsertsMappedDocumentAndForwardsToken()
    {
        var activity = NewActivity(typeof(string));
        ProcessingActivityDocument? inserted = null;
        using var cts = new CancellationTokenSource();
        _collection.InsertOneAsync(
                Arg.Do<ProcessingActivityDocument>(d => inserted = d),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _registry.RegisterActivityAsync(activity, cts.Token);

        result.IsRight.ShouldBeTrue();
        inserted.ShouldNotBeNull();
        inserted.ToBsonDocument().ShouldBe(
            ProcessingActivityDocument.FromEntity(ProcessingActivityMapper.ToEntity(activity)).ToBsonDocument());
        inserted.RequestTypeName.ShouldBe(typeof(string).AssemblyQualifiedName);
        await _collection.Received(1).InsertOneAsync(
            Arg.Any<ProcessingActivityDocument>(), Arg.Any<InsertOneOptions>(), cts.Token);
    }

    [Fact]
    public async Task RegisterActivityAsync_DuplicateKey_ReturnsLeftDuplicate()
    {
        _collection.InsertOneAsync(
                Arg.Any<ProcessingActivityDocument>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(WriteException(ServerErrorCategory.DuplicateKey));

        var result = await _registry.RegisterActivityAsync(NewActivity(typeof(string)));

        CodeOf(result).ShouldBe(GDPRErrors.ProcessingActivityDuplicateCode);
    }

    [Fact]
    public async Task RegisterActivityAsync_OtherWriteError_ReturnsLeftStoreError()
    {
        _collection.InsertOneAsync(
                Arg.Any<ProcessingActivityDocument>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(WriteException(ServerErrorCategory.ExecutionTimeout));

        var result = await _registry.RegisterActivityAsync(NewActivity(typeof(string)));

        CodeOf(result).ShouldBe(GDPRErrors.ProcessingActivityStoreErrorCode);
    }

    [Fact]
    public async Task RegisterActivityAsync_DriverThrows_ReturnsLeftStoreError()
    {
        _collection.InsertOneAsync(
                Arg.Any<ProcessingActivityDocument>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _registry.RegisterActivityAsync(NewActivity(typeof(string)));

        CodeOf(result).ShouldBe(GDPRErrors.ProcessingActivityStoreErrorCode);
    }

    #endregion

    #region GetAllActivitiesAsync

    [Fact]
    public async Task GetAllActivitiesAsync_ReturnsMappedActivitiesAndSkipsUnresolvableTypes()
    {
        var known = NewActivity(typeof(string));
        var knownDocument = ProcessingActivityDocument.FromEntity(ProcessingActivityMapper.ToEntity(known));
        var unknownDocument = ProcessingActivityDocument.FromEntity(ProcessingActivityMapper.ToEntity(NewActivity(typeof(int))));
        unknownDocument.RequestTypeName = "No.Such.Type, NoSuchAssembly";
        using var cts = new CancellationTokenSource();
        var capture = ArrangeFind(knownDocument, unknownDocument);

        var result = await _registry.GetAllActivitiesAsync(cts.Token);

        var activities = RightOf(result);
        activities.Count.ShouldBe(1);
        AssertSameActivity(activities[0], known);
        Render(capture.Filter!).ShouldBe(new BsonDocument());
        await _collection.Received(1).FindAsync(
            Arg.Any<FilterDefinition<ProcessingActivityDocument>>(),
            Arg.Any<FindOptions<ProcessingActivityDocument, ProcessingActivityDocument>>(),
            cts.Token);
    }

    [Fact]
    public async Task GetAllActivitiesAsync_NoDocuments_ReturnsEmptyList()
    {
        ArrangeFind();

        var result = await _registry.GetAllActivitiesAsync();

        RightOf(result).ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllActivitiesAsync_DriverThrows_ReturnsLeftStoreError()
    {
        _collection.FindAsync(
                Arg.Any<FilterDefinition<ProcessingActivityDocument>>(),
                Arg.Any<FindOptions<ProcessingActivityDocument, ProcessingActivityDocument>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException());

        var result = await _registry.GetAllActivitiesAsync();

        CodeOf(result).ShouldBe(GDPRErrors.ProcessingActivityStoreErrorCode);
    }

    #endregion

    #region GetActivityByRequestTypeAsync

    [Fact]
    public async Task GetActivityByRequestTypeAsync_Found_ReturnsSomeAndFiltersByTypeName()
    {
        var known = NewActivity(typeof(string));
        var document = ProcessingActivityDocument.FromEntity(ProcessingActivityMapper.ToEntity(known));
        using var cts = new CancellationTokenSource();
        var capture = ArrangeFind(document);

        var result = await _registry.GetActivityByRequestTypeAsync(typeof(string), cts.Token);

        var option = RightOf(result);
        option.IsSome.ShouldBeTrue();
        option.IfSome(a => AssertSameActivity(a, known));
        Render(capture.Filter!).ShouldBe(new BsonDocument("request_type_name", typeof(string).AssemblyQualifiedName));
        capture.Options!.Limit.ShouldBe(1);
        await _collection.Received(1).FindAsync(
            Arg.Any<FilterDefinition<ProcessingActivityDocument>>(),
            Arg.Any<FindOptions<ProcessingActivityDocument, ProcessingActivityDocument>>(),
            cts.Token);
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_NotFound_ReturnsNone()
    {
        ArrangeFind();

        var result = await _registry.GetActivityByRequestTypeAsync(typeof(string));

        RightOf(result).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_UnresolvableStoredType_ReturnsNone()
    {
        var document = ProcessingActivityDocument.FromEntity(ProcessingActivityMapper.ToEntity(NewActivity(typeof(string))));
        document.RequestTypeName = "No.Such.Type, NoSuchAssembly";
        ArrangeFind(document);

        var result = await _registry.GetActivityByRequestTypeAsync(typeof(string));

        RightOf(result).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_DriverThrows_ReturnsLeftStoreError()
    {
        _collection.FindAsync(
                Arg.Any<FilterDefinition<ProcessingActivityDocument>>(),
                Arg.Any<FindOptions<ProcessingActivityDocument, ProcessingActivityDocument>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException());

        var result = await _registry.GetActivityByRequestTypeAsync(typeof(string));

        CodeOf(result).ShouldBe(GDPRErrors.ProcessingActivityStoreErrorCode);
    }

    #endregion

    #region UpdateActivityAsync

    [Fact]
    public async Task UpdateActivityAsync_Matched_ReplacesByTypeNameWithoutUpsert()
    {
        var activity = NewActivity(typeof(string));
        FilterDefinition<ProcessingActivityDocument>? filter = null;
        ProcessingActivityDocument? replacement = null;
        ReplaceOptions? options = null;
        using var cts = new CancellationTokenSource();
        _collection.ReplaceOneAsync(
                Arg.Do<FilterDefinition<ProcessingActivityDocument>>(f => filter = f),
                Arg.Do<ProcessingActivityDocument>(d => replacement = d),
                Arg.Do<ReplaceOptions>(o => options = o),
                Arg.Any<CancellationToken>())
            .Returns(new ReplaceOneResult.Acknowledged(1, 1, null));

        var result = await _registry.UpdateActivityAsync(activity, cts.Token);

        result.IsRight.ShouldBeTrue();
        Render(filter!).ShouldBe(new BsonDocument("request_type_name", typeof(string).AssemblyQualifiedName));
        replacement.ShouldNotBeNull();
        replacement.ToBsonDocument().ShouldBe(
            ProcessingActivityDocument.FromEntity(ProcessingActivityMapper.ToEntity(activity)).ToBsonDocument());
        options.ShouldNotBeNull();
        options.IsUpsert.ShouldBeFalse();
        await _collection.Received(1).ReplaceOneAsync(
            Arg.Any<FilterDefinition<ProcessingActivityDocument>>(),
            Arg.Any<ProcessingActivityDocument>(),
            Arg.Any<ReplaceOptions>(),
            cts.Token);
    }

    [Fact]
    public async Task UpdateActivityAsync_NothingMatched_ReturnsLeftNotFound()
    {
        _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<ProcessingActivityDocument>>(),
                Arg.Any<ProcessingActivityDocument>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new ReplaceOneResult.Acknowledged(0, 0, null));

        var result = await _registry.UpdateActivityAsync(NewActivity(typeof(string)));

        CodeOf(result).ShouldBe(GDPRErrors.ProcessingActivityNotFoundCode);
    }

    [Fact]
    public async Task UpdateActivityAsync_DriverThrows_ReturnsLeftStoreError()
    {
        _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<ProcessingActivityDocument>>(),
                Arg.Any<ProcessingActivityDocument>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _registry.UpdateActivityAsync(NewActivity(typeof(string)));

        CodeOf(result).ShouldBe(GDPRErrors.ProcessingActivityStoreErrorCode);
    }

    #endregion

    #region Helpers

    private sealed class FindCapture
    {
        public FilterDefinition<ProcessingActivityDocument>? Filter { get; set; }

        public FindOptions<ProcessingActivityDocument, ProcessingActivityDocument>? Options { get; set; }
    }

    private FindCapture ArrangeFind(params ProcessingActivityDocument[] documents)
    {
        var capture = new FindCapture();
        var cursor = Substitute.For<IAsyncCursor<ProcessingActivityDocument>>();
        var consumed = false;
        cursor.Current.Returns(_ => documents);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            var hasMore = !consumed && documents.Length > 0;
            consumed = true;
            return hasMore;
        });
        _collection.FindAsync(
                Arg.Do<FilterDefinition<ProcessingActivityDocument>>(f => capture.Filter = f),
                Arg.Do<FindOptions<ProcessingActivityDocument, ProcessingActivityDocument>>(o => capture.Options = o),
                Arg.Any<CancellationToken>())
            .Returns(cursor);
        return capture;
    }

    // The record's equality compares the list fields by reference, so compare them by content.
    private static void AssertSameActivity(Activity actual, Activity expected)
    {
        actual.Id.ShouldBe(expected.Id);
        actual.Name.ShouldBe(expected.Name);
        actual.Purpose.ShouldBe(expected.Purpose);
        actual.LawfulBasis.ShouldBe(expected.LawfulBasis);
        actual.CategoriesOfDataSubjects.ShouldBe(expected.CategoriesOfDataSubjects);
        actual.CategoriesOfPersonalData.ShouldBe(expected.CategoriesOfPersonalData);
        actual.Recipients.ShouldBe(expected.Recipients);
        actual.ThirdCountryTransfers.ShouldBe(expected.ThirdCountryTransfers);
        actual.Safeguards.ShouldBe(expected.Safeguards);
        actual.RetentionPeriod.ShouldBe(expected.RetentionPeriod);
        actual.SecurityMeasures.ShouldBe(expected.SecurityMeasures);
        actual.RequestType.ShouldBe(expected.RequestType);
        actual.CreatedAtUtc.ShouldBe(expected.CreatedAtUtc);
        actual.LastUpdatedAtUtc.ShouldBe(expected.LastUpdatedAtUtc);
    }

    private static Activity NewActivity(Type requestType) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Billing",
        Purpose = "Invoice customers",
        LawfulBasis = LawfulBasis.Contract,
        CategoriesOfDataSubjects = ["Customers"],
        CategoriesOfPersonalData = ["Name", "Email"],
        Recipients = ["Payment provider"],
        ThirdCountryTransfers = "None",
        Safeguards = "SCC",
        RetentionPeriod = TimeSpan.FromDays(365),
        SecurityMeasures = "Encryption at rest",
        RequestType = requestType,
        CreatedAtUtc = Stamp,
        LastUpdatedAtUtc = Stamp
    };

    private static BsonDocument Render(FilterDefinition<ProcessingActivityDocument> filter)
    {
        var registry = BsonSerializer.SerializerRegistry;
        return filter.Render(new RenderArgs<ProcessingActivityDocument>(
            registry.GetSerializer<ProcessingActivityDocument>(), registry));
    }

    private static T RightOf<T>(Either<EncinaError, T> result)
        => result.Match(Right: value => value, Left: error => throw new InvalidOperationException($"Expected Right, got {error.GetCode()}"));

    private static string CodeOf<T>(Either<EncinaError, T> result)
        => result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left, got Right"),
            Left: error => error.GetCode().IfNone(string.Empty));

    private static MongoWriteException WriteException(ServerErrorCategory category)
    {
        // WriteError has no public constructor; the driver builds it from a server reply.
        var writeError = (WriteError)Activator.CreateInstance(
            typeof(WriteError),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [category, 11000, "duplicate key", new BsonDocument()],
            culture: null)!;
        var connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)));
        return new MongoWriteException(connectionId, writeError, null, null);
    }

    #endregion
}
