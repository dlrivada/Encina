using Encina.Cdc.Abstractions;
using Encina.Cdc.MySql;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Cdc.MySql;

/// <summary>
/// Unit tests for <see cref="MySqlCdcConnector.GetCurrentPositionAsync"/> driven through a
/// hand-written <see cref="IMySqlBinlogStatusClient"/>. The streaming path needs a live binlog and
/// is covered by the integration tests against MySQL 8.4 and 9.x.
/// </summary>
public sealed class MySqlCdcConnectorTests
{
    private sealed class FakeBinlogStatusClient : IMySqlBinlogStatusClient
    {
        public Func<CancellationToken, Task<(string File, long Position)?>> Handler { get; set; } =
            _ => Task.FromResult<(string File, long Position)?>(null);

        public int Calls { get; private set; }

        public Task<(string File, long Position)?> GetBinlogStatusAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Handler(cancellationToken);
        }
    }

    private static MySqlCdcConnector CreateConnector(FakeBinlogStatusClient client) =>
        new(
            new MySqlCdcOptions(),
            Substitute.For<ICdcPositionStore>(),
            NullLogger<MySqlCdcConnector>.Instance,
            client);

    [Fact]
    public async Task GetCurrentPositionAsync_RowReturned_ReturnsPositionWithFileAndOffset()
    {
        var client = new FakeBinlogStatusClient
        {
            Handler = _ => Task.FromResult<(string File, long Position)?>(("binlog.000007", 1234L)),
        };

        var result = await CreateConnector(client).GetCurrentPositionAsync();

        result.IsRight.ShouldBeTrue();
        var position = result.Match(p => p, _ => throw new InvalidOperationException()).ShouldBeOfType<MySqlCdcPosition>();
        position.BinlogFileName.ShouldBe("binlog.000007");
        position.BinlogPosition.ShouldBe(1234L);
        client.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task GetCurrentPositionAsync_EmptyResult_ReturnsLeftWithServerStartAdvice()
    {
        var client = new FakeBinlogStatusClient();

        var result = await CreateConnector(client).GetCurrentPositionAsync();

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(_ => throw new InvalidOperationException(), e => e);
        error.Message.ShouldContain("at server start");
        error.Message.ShouldContain("--log-bin");
        error.Message.ShouldContain("cannot be switched on with SET GLOBAL");
        error.Message.ShouldNotContain("SET GLOBAL log_bin = ON");
    }

    [Fact]
    public async Task GetCurrentPositionAsync_ClientThrows_ReturnsLeftWithInnerException()
    {
        var failure = new InvalidOperationException("server unreachable");
        var client = new FakeBinlogStatusClient { Handler = _ => throw failure };

        var result = await CreateConnector(client).GetCurrentPositionAsync();

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(_ => throw new InvalidOperationException(), e => e);
        error.Message.ShouldContain("Failed to get MySQL binlog position");
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex => ex.ShouldBeSameAs(failure));
    }

    [Fact]
    public async Task GetCurrentPositionAsync_ClientThrowsOperationCanceled_Propagates()
    {
        var client = new FakeBinlogStatusClient { Handler = _ => throw new OperationCanceledException() };

        await Should.ThrowAsync<OperationCanceledException>(
            () => CreateConnector(client).GetCurrentPositionAsync());
    }

    [Fact]
    public async Task GetCurrentPositionAsync_PreCancelledToken_PassesTokenAndPropagates()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        CancellationToken seen = default;
        var client = new FakeBinlogStatusClient
        {
            Handler = token =>
            {
                seen = token;
                token.ThrowIfCancellationRequested();
                return Task.FromResult<(string File, long Position)?>(null);
            },
        };

        await Should.ThrowAsync<OperationCanceledException>(
            () => CreateConnector(client).GetCurrentPositionAsync(cts.Token));

        seen.ShouldBe(cts.Token);
    }

    [Fact]
    public void ConnectorId_IsStable()
    {
        CreateConnector(new FakeBinlogStatusClient()).ConnectorId.ShouldBe("encina-cdc-mysql");
    }
}
