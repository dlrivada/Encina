using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.PostgreSql;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NpgsqlTypes;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Cdc.PostgreSql;

/// <summary>
/// Unit tests for the paths of <see cref="PostgresCdcConnector"/> that do not need a live
/// logical-replication server: construction, the failure paths against a refused local port,
/// cancellation and the position-restore logic. The streaming paths are covered by the
/// integration tests against a PostgreSQL container with <c>wal_level=logical</c>.
/// </summary>
public sealed class PostgresCdcConnectorTests
{
    // Port 1 is never listening; the connection is refused immediately.
    private const string RefusedConnectionString =
        "Host=127.0.0.1;Port=1;Username=u;Password=p;Database=d;Timeout=2;Command Timeout=2;Pooling=false";

    private static PostgresCdcOptions CreateOptions(bool createPublication = false) => new()
    {
        ConnectionString = RefusedConnectionString,
        CreatePublicationIfNotExists = createPublication,
        CreateSlotIfNotExists = false,
    };

    private static PostgresCdcConnector CreateConnector(
        PostgresCdcOptions options,
        ICdcPositionStore? store = null) =>
        new(
            options,
            store ?? Substitute.For<ICdcPositionStore>(),
            NullLogger<PostgresCdcConnector>.Instance,
            new NpgsqlReplicationClient(options));

    private static ICdcPositionStore StoreReturning(Either<EncinaError, Option<CdcPosition>> result)
    {
        var store = Substitute.For<ICdcPositionStore>();
        store.GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(result));
        return store;
    }

    #region Constructor

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new PostgresCdcConnector(
            null!,
            Substitute.For<ICdcPositionStore>(),
            NullLogger<PostgresCdcConnector>.Instance,
            new NpgsqlReplicationClient(new PostgresCdcOptions())));
    }

    [Fact]
    public void Constructor_NullPositionStore_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new PostgresCdcConnector(
            new PostgresCdcOptions(),
            null!,
            NullLogger<PostgresCdcConnector>.Instance,
            new NpgsqlReplicationClient(new PostgresCdcOptions())));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new PostgresCdcConnector(
            new PostgresCdcOptions(),
            Substitute.For<ICdcPositionStore>(),
            null!,
            new NpgsqlReplicationClient(new PostgresCdcOptions())));
    }

    [Fact]
    public void Constructor_NullReplicationClient_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new PostgresCdcConnector(
            new PostgresCdcOptions(),
            Substitute.For<ICdcPositionStore>(),
            NullLogger<PostgresCdcConnector>.Instance,
            null!));
    }

    [Fact]
    public void ConnectorId_ReturnsStableIdentifier()
    {
        CreateConnector(CreateOptions()).ConnectorId.ShouldBe("encina-cdc-postgresql");
    }

    #endregion

    #region GetCurrentPositionAsync

    [Fact]
    public async Task GetCurrentPositionAsync_ServerUnreachable_ReturnsConnectionFailedWithoutExceptionMessage()
    {
        var connector = CreateConnector(CreateOptions());

        var result = await connector.GetCurrentPositionAsync(TestContext.Current.CancellationToken);

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(_ => default!, Left: e => e);
        error.Message.ShouldContain("CDC connection failed");
        var exception = error.Exception.IfNone(() => throw new InvalidOperationException("No inner exception"));
        exception.Message.ShouldNotBeNullOrWhiteSpace();
        error.Message.ShouldNotContain(exception.Message);
    }

    [Fact]
    public async Task GetCurrentPositionAsync_CancelledToken_ThrowsOperationCanceled()
    {
        var connector = CreateConnector(CreateOptions());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => connector.GetCurrentPositionAsync(cts.Token));
    }

    #endregion

    #region StreamChangesAsync

    // The three tests below that expect NpgsqlException pin the current behaviour tracked in #2779
    // (a failure to open the replication connection escapes the iterator instead of yielding a Left);
    // they must expect a Left when that issue is fixed.

    [Fact]
    public async Task StreamChangesAsync_PublicationSetupFails_YieldsSingleLeftAndStops()
    {
        var connector = CreateConnector(CreateOptions(createPublication: true), StoreReturning(Option<CdcPosition>.None));

        var items = new List<Either<EncinaError, ChangeEvent>>();
        await foreach (var item in connector.StreamChangesAsync(TestContext.Current.CancellationToken))
        {
            items.Add(item);
        }

        items.Count.ShouldBe(1);
        items[0].IsLeft.ShouldBeTrue();
        var error = items[0].Match(_ => default!, Left: e => e);
        error.Message.ShouldContain("Failed to ensure publication 'encina_cdc_publication' exists");
        var exception = error.Exception.IfNone(() => throw new InvalidOperationException("No inner exception"));
        error.Message.ShouldNotContain(exception.Message);
    }

    [Fact]
    public async Task StreamChangesAsync_SavedPostgresPosition_IsRestoredBeforeConnecting()
    {
        var saved = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(0x16B3748));
        var store = StoreReturning(Option<CdcPosition>.Some(saved));
        var connector = CreateConnector(CreateOptions(), store);

        // No publication step and no server: opening the replication connection fails.
        await Should.ThrowAsync<NpgsqlException>(async () =>
        {
            await foreach (var _ in connector.StreamChangesAsync(TestContext.Current.CancellationToken))
            {
            }
        });

        await store.Received(1).GetPositionAsync("encina-cdc-postgresql", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StreamChangesAsync_PositionStoreReturnsLeft_StartsWithoutSavedPosition()
    {
        var store = StoreReturning(Either<EncinaError, Option<CdcPosition>>.Left(EncinaError.New("store down")));
        var connector = CreateConnector(CreateOptions(), store);

        await Should.ThrowAsync<NpgsqlException>(async () =>
        {
            await foreach (var _ in connector.StreamChangesAsync(TestContext.Current.CancellationToken))
            {
            }
        });

        await store.Received(1).GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StreamChangesAsync_NonPostgresSavedPosition_IsIgnored()
    {
        var foreign = Substitute.For<CdcPosition>();
        var store = StoreReturning(Option<CdcPosition>.Some(foreign));
        var connector = CreateConnector(CreateOptions(), store);

        await Should.ThrowAsync<NpgsqlException>(async () =>
        {
            await foreach (var _ in connector.StreamChangesAsync(TestContext.Current.CancellationToken))
            {
            }
        });

        await store.Received(1).GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
