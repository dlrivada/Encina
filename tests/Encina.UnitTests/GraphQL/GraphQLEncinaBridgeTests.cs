using Encina.GraphQL;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.GraphQL;

/// <summary>
/// Unit tests for <see cref="GraphQLEncinaBridge"/>.
/// </summary>
public sealed class GraphQLEncinaBridgeTests
{
    private readonly IEncina _encina = Substitute.For<IEncina>();
    private readonly ILogger<GraphQLEncinaBridge> _logger = NullLogger<GraphQLEncinaBridge>.Instance;
    private readonly EncinaGraphQLOptions _options = new();

    private GraphQLEncinaBridge CreateBridge() =>
        new(_encina, _logger, Options.Create(_options));

    #region Constructor Guards

    [Fact]
    public void Constructor_NullEncina_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() =>
            new GraphQLEncinaBridge(null!, _logger, Options.Create(_options)));
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() =>
            new GraphQLEncinaBridge(_encina, null!, Options.Create(_options)));
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() =>
            new GraphQLEncinaBridge(_encina, _logger, null!));
    }

    #endregion

    #region QueryAsync

    [Fact]
    public async Task QueryAsync_NullQuery_ShouldThrow()
    {
        var bridge = CreateBridge();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await bridge.QueryAsync<TestQuery, string>(null!));
    }

    [Fact]
    public async Task QueryAsync_Success_ShouldReturnRight()
    {
        _encina.Send(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, string>("result"));

        var bridge = CreateBridge();
        var result = await bridge.QueryAsync<TestQuery, string>(new TestQuery());

        result.IsRight.ShouldBeTrue();
        ((string)result).ShouldBe("result");
    }

    [Fact]
    public async Task QueryAsync_EncinaReturnsError_ShouldReturnLeft()
    {
        _encina.Send(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, string>(EncinaError.New("failed")));

        var bridge = CreateBridge();
        var result = await bridge.QueryAsync<TestQuery, string>(new TestQuery());

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task QueryAsync_EncinaThrows_ShouldReturnLeft()
    {
        _encina.Send(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>())
            .Returns<Either<EncinaError, string>>(_ => throw new InvalidOperationException("boom"));

        var bridge = CreateBridge();
        var result = await bridge.QueryAsync<TestQuery, string>(new TestQuery());

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region MutateAsync

    [Fact]
    public async Task MutateAsync_NullMutation_ShouldThrow()
    {
        var bridge = CreateBridge();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await bridge.MutateAsync<TestMutation, string>(null!));
    }

    [Fact]
    public async Task MutateAsync_Success_ShouldReturnRight()
    {
        _encina.Send(Arg.Any<TestMutation>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, string>("mutated"));

        var bridge = CreateBridge();
        var result = await bridge.MutateAsync<TestMutation, string>(new TestMutation());

        result.IsRight.ShouldBeTrue();
        ((string)result).ShouldBe("mutated");
    }

    [Fact]
    public async Task MutateAsync_EncinaThrows_ShouldReturnLeft()
    {
        _encina.Send(Arg.Any<TestMutation>(), Arg.Any<CancellationToken>())
            .Returns<Either<EncinaError, string>>(_ => throw new InvalidOperationException("mutation error"));

        var bridge = CreateBridge();
        var result = await bridge.MutateAsync<TestMutation, string>(new TestMutation());

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region SubscribeAsync

    [Fact]
    public async Task SubscribeAsync_NullSubscription_ShouldThrow()
    {
        var bridge = CreateBridge();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
        {
            await foreach (var _ in bridge.SubscribeAsync<TestSubscription, string>(null!))
            {
            }
        });
    }

    [Fact]
    public async Task SubscribeAsync_SubscriptionsDisabled_ShouldReturnError()
    {
        _options.EnableSubscriptions = false;
        var bridge = CreateBridge();

        var results = new List<Either<EncinaError, string>>();
        await foreach (var item in bridge.SubscribeAsync<TestSubscription, string>(new TestSubscription()))
        {
            results.Add(item);
        }

        results.Count.ShouldBe(1);
        results[0].IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task SubscribeAsync_Enabled_ShouldReturnNotImplementedError()
    {
        _options.EnableSubscriptions = true;
        var bridge = CreateBridge();

        var results = new List<Either<EncinaError, string>>();
        await foreach (var item in bridge.SubscribeAsync<TestSubscription, string>(new TestSubscription()))
        {
            results.Add(item);
        }

        // Should return at least one error (not implemented)
        results.Count.ShouldBeGreaterThan(0);
        results[0].IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Test Types

    private sealed class TestQuery : IRequest<string>;

    private sealed class TestMutation : IRequest<string>;

    private sealed class TestSubscription;

    #endregion
}
