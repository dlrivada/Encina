using System.Text.Json;
using System.Threading.Channels;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.PostgreSql;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.IntegrationTests.Cdc.PostgreSql;

/// <summary>
/// Integration tests for <see cref="PostgresCdcConnector"/> against a real PostgreSQL server with
/// <c>wal_level=logical</c> (pgoutput logical replication), shared through the
/// <c>CDC-PostgreSQL</c> collection.
/// </summary>
[Collection("CDC-PostgreSQL")]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Trait("Feature", "CDC")]
public sealed class PostgresCdcConnectorIntegrationTests : IAsyncLifetime
{
    private const string PublicationName = "encina_cdc_it_publication";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    private readonly PostgreSqlCdcFixture _fixture;
    private readonly List<CancellationTokenSource> _streams = [];

    public PostgresCdcConnectorIntegrationTests(PostgreSqlCdcFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ResetAsync(PublicationName, TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        foreach (var cts in _streams)
        {
            cts.Cancel();
            cts.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    private PostgresCdcOptions CreateOptions(
        bool createPublication = true,
        bool createSlot = true,
        string[]? tables = null) => new()
    {
        ConnectionString = _fixture.ConnectionString,
        PublicationName = PublicationName,
        CreatePublicationIfNotExists = createPublication,
        CreateSlotIfNotExists = createSlot,
        // Empty means ALL TABLES: the connector emits invalid SQL for an explicit table list (see the
        // follow-up bug issue), so streaming tests rely on the ALL TABLES publication.
        PublicationTables = tables ?? [],
    };

    private static PostgresCdcConnector CreateConnector(PostgresCdcOptions options, ICdcPositionStore? store = null)
    {
        if (store is null)
        {
            store = Substitute.For<ICdcPositionStore>();
            store.GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(
                    Prelude.Right<EncinaError, Option<CdcPosition>>(Option<CdcPosition>.None)));
        }

        return new PostgresCdcConnector(options, store, NullLogger<PostgresCdcConnector>.Instance);
    }

    /// <summary>Starts the stream on a background task and exposes its items through a channel.</summary>
    private (ChannelReader<Either<EncinaError, ChangeEvent>> Reader, CancellationTokenSource Cts, Task Pump)
        StartStreaming(PostgresCdcConnector connector)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        _streams.Add(cts);
        var channel = Channel.CreateUnbounded<Either<EncinaError, ChangeEvent>>();

        var pump = Task.Run(async () =>
        {
            try
            {
                await foreach (var item in connector.StreamChangesAsync(cts.Token))
                {
                    await channel.Writer.WriteAsync(item, CancellationToken.None);
                }

                channel.Writer.TryComplete();
            }
            catch (OperationCanceledException)
            {
                channel.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                channel.Writer.TryComplete(ex);
            }
        }, CancellationToken.None);

        return (channel.Reader, cts, pump);
    }

    private async Task WaitForActiveSlotAsync(ChannelReader<Either<EncinaError, ChangeEvent>> reader)
    {
        using var timeout = new CancellationTokenSource(Timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            timeout.Token, TestContext.Current.CancellationToken);

        while (!await _fixture.IsSlotActiveAsync(linked.Token))
        {
            if (reader.TryPeek(out var early))
            {
                // The stream yielded (typically a Left from the setup) before attaching to the slot.
                throw new InvalidOperationException($"The stream yielded {early} before the slot became active.");
            }

            if (reader.Completion.IsCompleted)
            {
                await reader.Completion;
                throw new InvalidOperationException("The stream completed before the slot became active.");
            }

            await Task.Delay(100, linked.Token);
        }
    }

    private static async Task<ChangeEvent> ReadEventAsync(ChannelReader<Either<EncinaError, ChangeEvent>> reader)
    {
        using var timeout = new CancellationTokenSource(Timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            timeout.Token, TestContext.Current.CancellationToken);

        var item = await reader.ReadAsync(linked.Token);
        item.IsRight.ShouldBeTrue(
            item.Match(_ => string.Empty, Left: e => e.Exception.Match(x => x.ToString(), () => e.Message)));
        return item.Match(e => e, Left: _ => throw new InvalidOperationException("Expected a Right item"));
    }

    private static JsonElement Json(object? value) => value.ShouldBeOfType<JsonElement>();

    #region GetCurrentPositionAsync

    [Fact]
    public async Task GetCurrentPositionAsync_RealServer_ReturnsPostgresPositionWithLsn()
    {
        var connector = CreateConnector(CreateOptions());

        var result = await connector.GetCurrentPositionAsync(TestContext.Current.CancellationToken);

        result.IsRight.ShouldBeTrue();
        var position = result.Match(p => p, Left: _ => throw new InvalidOperationException("Expected Right"));
        var pgPosition = position.ShouldBeOfType<PostgresCdcPosition>();
        ((ulong)pgPosition.Lsn).ShouldBeGreaterThan(0UL);
    }

    #endregion

    #region StreamChangesAsync

    [Fact]
    public async Task StreamChangesAsync_FreshSlot_StreamsInsertAndDeleteWithBeforeAndAfterImages()
    {
        var connector = CreateConnector(CreateOptions());
        var (reader, _, _) = StartStreaming(connector);
        await WaitForActiveSlotAsync(reader);

        await _fixture.ExecuteAsync("INSERT INTO public.cdc_orders (id, name, note) VALUES (1, 'first', NULL)", TestContext.Current.CancellationToken);
        await _fixture.ExecuteAsync("DELETE FROM public.cdc_orders WHERE id = 1", TestContext.Current.CancellationToken);

        var insert = await ReadEventAsync(reader);
        insert.TableName.ShouldBe("public.cdc_orders");
        insert.Operation.ShouldBe(ChangeOperation.Insert);
        insert.Before.ShouldBeNull();
        var insertAfter = Json(insert.After);
        insertAfter.GetProperty("id").GetString().ShouldBe("1");
        insertAfter.GetProperty("name").GetString().ShouldBe("first");
        insertAfter.GetProperty("note").ValueKind.ShouldBe(JsonValueKind.Null);
        insert.Metadata.SourceSchema.ShouldBe("public");
        insert.Metadata.Position.ShouldBeOfType<PostgresCdcPosition>();

        var delete = await ReadEventAsync(reader);
        delete.Operation.ShouldBe(ChangeOperation.Delete);
        delete.After.ShouldBeNull();
        Json(delete.Before).GetProperty("name").GetString().ShouldBe("first");
    }

    [Fact(Skip = "Blocked by a connector defect (follow-up issue 'cdc-postgresql-update-old-image'): the connector reads " +
                 "the new tuple before the old tuple, but pgoutput streams the old tuple first, so every update with " +
                 "REPLICA IDENTITY FULL or a key change yields a StreamInterrupted Left. Enable when the connector is fixed.")]
    public async Task StreamChangesAsync_UpdateWithOldImage_YieldsBeforeAndAfter()
    {
        var connector = CreateConnector(CreateOptions());
        var (reader, _, _) = StartStreaming(connector);
        await WaitForActiveSlotAsync(reader);

        await _fixture.ExecuteAsync("INSERT INTO public.cdc_orders (id, name) VALUES (1, 'first')", TestContext.Current.CancellationToken);
        await _fixture.ExecuteAsync("UPDATE public.cdc_orders SET name = 'renamed' WHERE id = 1", TestContext.Current.CancellationToken);

        (await ReadEventAsync(reader)).Operation.ShouldBe(ChangeOperation.Insert);
        var update = await ReadEventAsync(reader);
        update.Operation.ShouldBe(ChangeOperation.Update);
        Json(update.Before).GetProperty("name").GetString().ShouldBe("first");
        Json(update.After).GetProperty("name").GetString().ShouldBe("renamed");
    }

    [Fact]
    public async Task StreamChangesAsync_DefaultReplicaIdentity_NonKeyUpdateHasNoOldImageAndDeleteCarriesKey()
    {
        var connector = CreateConnector(CreateOptions());
        var (reader, _, _) = StartStreaming(connector);
        await WaitForActiveSlotAsync(reader);

        await _fixture.ExecuteAsync("INSERT INTO public.cdc_plain (id, name) VALUES (5, 'plain')", TestContext.Current.CancellationToken);
        await _fixture.ExecuteAsync("UPDATE public.cdc_plain SET name = 'changed' WHERE id = 5", TestContext.Current.CancellationToken);
        await _fixture.ExecuteAsync("DELETE FROM public.cdc_plain WHERE id = 5", TestContext.Current.CancellationToken);

        (await ReadEventAsync(reader)).Operation.ShouldBe(ChangeOperation.Insert);

        // Non-key update: no old image is sent without REPLICA IDENTITY FULL.
        var plainUpdate = await ReadEventAsync(reader);
        plainUpdate.Operation.ShouldBe(ChangeOperation.Update);
        plainUpdate.Before.ShouldBeNull();
        Json(plainUpdate.After).GetProperty("name").GetString().ShouldBe("changed");

        // Delete: only the key tuple is sent.
        var keyDelete = await ReadEventAsync(reader);
        keyDelete.Operation.ShouldBe(ChangeOperation.Delete);
        Json(keyDelete.Before).GetProperty("id").GetString().ShouldBe("5");
    }

    [Fact]
    public async Task StreamChangesAsync_TruncateIsSkipped_NextInsertIsStillDelivered()
    {
        var connector = CreateConnector(CreateOptions());
        var (reader, _, _) = StartStreaming(connector);
        await WaitForActiveSlotAsync(reader);

        await _fixture.ExecuteAsync("TRUNCATE public.cdc_orders", TestContext.Current.CancellationToken);
        await _fixture.ExecuteAsync("INSERT INTO public.cdc_orders (id, name) VALUES (9, 'after truncate')", TestContext.Current.CancellationToken);

        var next = await ReadEventAsync(reader);
        next.Operation.ShouldBe(ChangeOperation.Insert);
        Json(next.After).GetProperty("id").GetString().ShouldBe("9");
    }

    [Fact]
    public async Task StreamChangesAsync_ExistingSlotAndPublication_ReusesThemAndDeliversBacklog()
    {
        await _fixture.ExecuteAsync(
            $"CREATE PUBLICATION {PublicationName} FOR TABLE public.cdc_orders",
            TestContext.Current.CancellationToken);
        await _fixture.CreateSlotAsync(TestContext.Current.CancellationToken);
        await _fixture.ExecuteAsync(
            "INSERT INTO public.cdc_orders (id, name) VALUES (2, 'backlog')",
            TestContext.Current.CancellationToken);

        // Both create flags are on: the existing publication and the existing slot (SQLSTATE 42710) are tolerated.
        var connector = CreateConnector(CreateOptions());
        var (reader, _, _) = StartStreaming(connector);

        var insert = await ReadEventAsync(reader);
        insert.Operation.ShouldBe(ChangeOperation.Insert);
        Json(insert.After).GetProperty("name").GetString().ShouldBe("backlog");
    }

    [Fact]
    public async Task StreamChangesAsync_SavedPosition_ResumesAndDeliversLaterChanges()
    {
        await _fixture.ExecuteAsync(
            $"CREATE PUBLICATION {PublicationName} FOR TABLE public.cdc_orders",
            TestContext.Current.CancellationToken);
        await _fixture.CreateSlotAsync(TestContext.Current.CancellationToken);
        await _fixture.ExecuteAsync("INSERT INTO public.cdc_orders (id, name) VALUES (3, 'one')", TestContext.Current.CancellationToken);

        var options = CreateOptions(createPublication: false, createSlot: false);
        var (firstReader, firstCts, firstPump) = StartStreaming(CreateConnector(options));
        var first = await ReadEventAsync(firstReader);
        var savedPosition = first.Metadata.Position;
        await firstCts.CancelAsync();
        await firstPump.WaitAsync(Timeout, TestContext.Current.CancellationToken);

        var store = Substitute.For<ICdcPositionStore>();
        store.GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Prelude.Right<EncinaError, Option<CdcPosition>>(Option<CdcPosition>.Some(savedPosition))));

        await _fixture.ExecuteAsync("INSERT INTO public.cdc_orders (id, name) VALUES (4, 'two')", TestContext.Current.CancellationToken);

        var (secondReader, _, _) = StartStreaming(CreateConnector(options, store));
        var seenSecondRow = false;
        while (!seenSecondRow)
        {
            var next = await ReadEventAsync(secondReader);
            seenSecondRow = Json(next.After).GetProperty("id").GetString() == "4";
        }

        await store.Received().GetPositionAsync("encina-cdc-postgresql", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StreamChangesAsync_CancelledWhileStreaming_EndsTheEnumeration()
    {
        var connector = CreateConnector(CreateOptions());
        var (reader, cts, pump) = StartStreaming(connector);
        await WaitForActiveSlotAsync(reader);

        await cts.CancelAsync();

        await pump.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        pump.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task StreamChangesAsync_PublicationForMissingTable_YieldsConnectionFailedWithoutExceptionMessage()
    {
        var connector = CreateConnector(CreateOptions(tables: ["public.no_such_table"]));
        var (reader, _, _) = StartStreaming(connector);

        using var timeout = new CancellationTokenSource(Timeout);
        var item = await reader.ReadAsync(timeout.Token);

        item.IsLeft.ShouldBeTrue();
        var error = item.Match(_ => default!, Left: e => e);
        error.Message.ShouldContain($"Failed to ensure publication '{PublicationName}' exists");
        var exception = error.Exception.IfNone(() => throw new InvalidOperationException("No inner exception"));
        error.Message.ShouldNotContain(exception.Message);
        (await reader.WaitToReadAsync(timeout.Token)).ShouldBeFalse();
    }

    #endregion
}
