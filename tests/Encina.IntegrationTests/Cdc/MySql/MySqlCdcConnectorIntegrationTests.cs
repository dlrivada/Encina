using System.Text.Json;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.MySql;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.IntegrationTests.Cdc.MySql;

/// <summary>
/// Integration tests for <see cref="MySqlCdcConnector"/> and <see cref="MySqlBinlogStatusClient"/> against
/// real MySQL 8.4 (LTS) and 9.x servers with the binary log in ROW format and GTIDs enabled, shared
/// through the <c>CDC-MySQL</c> collection. They prove the position read (<c>SHOW BINARY LOG STATUS</c>,
/// which replaced the removed <c>SHOW MASTER STATUS</c>) and the MySqlCdc streaming path on 8.4+ (#2859).
/// </summary>
[Collection("CDC-MySQL")]
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Trait("Feature", "CDC")]
public sealed class MySqlCdcConnectorIntegrationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(90);

    // Each replication client needs a server id that is unique on the server (MySqlCdcOptions.ServerId).
    private static long _nextServerId = 1000;

    private readonly MySqlCdcFixture _fixture;

    public MySqlCdcConnectorIntegrationTests(MySqlCdcFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<string> Servers => new() { MySqlCdcFixture.Server84, MySqlCdcFixture.Server9 };

    private MySqlCdcConnector CreateConnector(string serverKey)
    {
        var server = _fixture.GetServer(serverKey);
        var options = new MySqlCdcOptions
        {
            ConnectionString = server.ConnectionString,
            Hostname = server.Host,
            Port = server.Port,
            Username = "cdc_user",
            Password = "cdc_Password1!",
            ServerId = Interlocked.Increment(ref _nextServerId),
            AllowLocalEndpoints = true,
        };

        var store = Substitute.For<ICdcPositionStore>();
        store.GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Prelude.Right<EncinaError, Option<CdcPosition>>(Option<CdcPosition>.None)));

        return new MySqlCdcConnector(
            options, store, NullLogger<MySqlCdcConnector>.Instance, new MySqlBinlogStatusClient(options));
    }

    private static MySqlCdcPosition ShouldBePosition(Either<EncinaError, CdcPosition> result)
    {
        result.IsRight.ShouldBeTrue(
            result.Match(_ => string.Empty, Left: e => e.Exception.Match(x => x.ToString(), () => e.Message)));
        return result.Match(p => p, Left: _ => throw new InvalidOperationException("Expected Right"))
            .ShouldBeOfType<MySqlCdcPosition>();
    }

    [Theory]
    [MemberData(nameof(Servers))]
    public async Task GetCurrentPositionAsync_RealServer_ReturnsBinlogFileAndPosition(string serverKey)
    {
        var connector = CreateConnector(serverKey);

        var result = await connector.GetCurrentPositionAsync(TestContext.Current.CancellationToken);

        var position = ShouldBePosition(result);
        position.BinlogFileName.ShouldNotBeNullOrWhiteSpace();
        position.BinlogFileName.ShouldStartWith("mysql-bin.");
        position.BinlogPosition.ShouldBeGreaterThan(0);
    }

    [Theory]
    [MemberData(nameof(Servers))]
    public async Task GetCurrentPositionAsync_AfterCommittedDml_PositionAdvances(string serverKey)
    {
        var ct = TestContext.Current.CancellationToken;
        await _fixture.ResetAsync(serverKey, ct);
        var connector = CreateConnector(serverKey);
        var before = ShouldBePosition(await connector.GetCurrentPositionAsync(ct));

        await _fixture.ExecuteAsync(serverKey, "INSERT INTO cdc_orders (id, name) VALUES (1, 'advance')", ct);

        var after = ShouldBePosition(await connector.GetCurrentPositionAsync(ct));
        after.CompareTo(before).ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetCurrentPositionAsync_BinaryLoggingDisabled_ReturnsLeft()
    {
        var connector = CreateConnector(MySqlCdcFixture.ServerNoBinlog);

        var result = await connector.GetCurrentPositionAsync(TestContext.Current.CancellationToken);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            _ => string.Empty,
            Left: e => e.Message).ShouldContain("Binary logging is not enabled");
    }

    [Theory]
    [MemberData(nameof(Servers))]
    public async Task StreamChangesAsync_RealServer_StreamsInsertedRow(string serverKey)
    {
        var ct = TestContext.Current.CancellationToken;
        await _fixture.ResetAsync(serverKey, ct);
        var connector = CreateConnector(serverKey);

        using var timeout = new CancellationTokenSource(Timeout);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, ct);

        // The stream attaches asynchronously and starts at the end of the binlog, so keep inserting
        // rows until its first insert event arrives instead of sleeping for a guessed time.
        var inserter = Task.Run(async () =>
        {
            for (var id = 1; !cts.IsCancellationRequested; id++)
            {
                await _fixture.ExecuteAsync(
                    serverKey, $"INSERT INTO cdc_orders (id, name) VALUES ({id}, 'stream-{id}')", cts.Token);
                await Task.Delay(500, cts.Token);
            }
        }, CancellationToken.None);

        ChangeEvent? received = null;
        try
        {
            await foreach (var item in connector.StreamChangesAsync(cts.Token))
            {
                item.IsRight.ShouldBeTrue(
                    item.Match(_ => string.Empty, Left: e => e.Exception.Match(x => x.ToString(), () => e.Message)));
                received = item.Match(e => e, Left: _ => throw new InvalidOperationException("Expected Right"));
                break;
            }
        }
        finally
        {
            await cts.CancelAsync();
            try
            {
                await inserter;
            }
            catch (OperationCanceledException)
            {
                // Expected: the inserter stops when the stream is cancelled.
            }
        }

        received.ShouldNotBeNull("no change event arrived before the timeout");
        received.TableName.ShouldBe($"{MySqlCdcFixture.DatabaseName}.cdc_orders");
        received.Operation.ShouldBe(ChangeOperation.Insert);
        received.Before.ShouldBeNull();
        var after = received.After.ShouldBeOfType<JsonElement>();

        // MySqlCdc hands string columns over as byte[], which the connector serializes as base64.
        var name = Convert.FromBase64String(after.GetProperty("column_1").GetString()!);
        System.Text.Encoding.UTF8.GetString(name).ShouldStartWith("stream-");
    }
}
