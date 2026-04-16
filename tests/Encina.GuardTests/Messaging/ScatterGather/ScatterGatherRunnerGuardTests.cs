using Encina.Messaging.ScatterGather;
using Shouldly;

namespace Encina.GuardTests.Messaging.ScatterGather;

/// <summary>
/// Guard clause tests for ScatterGatherRunner constructor and ExecuteAsync method parameters.
/// </summary>
public class ScatterGatherRunnerGuardTests
{
    private readonly ScatterGatherOptions _options = new();
    private readonly ILogger<ScatterGatherRunner> _logger = NullLogger<ScatterGatherRunner>.Instance;

    private ScatterGatherRunner CreateSut() => new(_options, _logger);

    #region Constructor Guards

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ScatterGatherRunner(null!, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ScatterGatherRunner(_options, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullTimeProvider_UsesSystemDefault()
    {
        var act = () => new ScatterGatherRunner(_options, _logger, timeProvider: null);
        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        var sut = CreateSut();
        sut.ShouldNotBeNull();
    }

    #endregion

    #region ExecuteAsync Guards

    [Fact]
    public async Task ExecuteAsync_NullDefinition_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.ExecuteAsync<TestRequest, TestResponse>(null!, new TestRequest()).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("definition");
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var definition = CreateMinimalDefinition();
        var act = () => sut.ExecuteAsync(definition, (TestRequest)null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task ExecuteAsync_ValidParameters_DoesNotThrow()
    {
        var sut = CreateSut();
        var definition = CreateMinimalDefinition();

        // Should not throw guards - will execute the pipeline
        var result = await sut.ExecuteAsync(definition, new TestRequest());
        // The result depends on handler logic, but guards should not throw
        // Just verify execution completed without exception (Either was returned)
        _ = result;
    }

    #endregion

    private static BuiltScatterGatherDefinition<TestRequest, TestResponse> CreateMinimalDefinition()
    {
        return ScatterGatherBuilder.Create<TestRequest, TestResponse>("TestOp")
            .ScatterTo("Handler1", (req, _) =>
                ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, TestResponse>(new TestResponse())))
            .GatherAll()
            .TakeFirst()
            .Build();
    }

    private sealed class TestRequest
    {
        public string? Value { get; set; }
    }

    private sealed class TestResponse
    {
        public string? Result { get; set; }
    }
}
