using Encina.Messaging.Sagas;
using Encina.Messaging.Sagas.LowCeremony;
using Encina.Messaging.Serialization;
using Shouldly;

namespace Encina.GuardTests.Messaging.Sagas;

/// <summary>
/// Guard clause tests for SagaRunner constructor and RunAsync method parameters.
/// </summary>
public class SagaRunnerGuardTests
{
    private readonly SagaOrchestrator _orchestrator;
    private readonly IRequestContextAccessor _requestContextAccessor = Substitute.For<IRequestContextAccessor>();
    private readonly ILogger<SagaRunner> _logger = NullLogger<SagaRunner>.Instance;

    public SagaRunnerGuardTests()
    {
        _orchestrator = new SagaOrchestrator(
            Substitute.For<ISagaStore>(),
            new SagaOptions(),
            NullLogger<SagaOrchestrator>.Instance,
            Substitute.For<ISagaStateFactory>(),
            new JsonMessageSerializer());
        _requestContextAccessor.RequestContext.Returns(Substitute.For<IRequestContext>());
    }

    private SagaRunner CreateSut() => new(_orchestrator, _requestContextAccessor, _logger);

    #region Constructor Guards

    [Fact]
    public void Constructor_NullOrchestrator_ThrowsArgumentNullException()
    {
        var act = () => new SagaRunner(null!, _requestContextAccessor, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("orchestrator");
    }

    [Fact]
    public void Constructor_NullRequestContextAccessor_ThrowsArgumentNullException()
    {
        var act = () => new SagaRunner(_orchestrator, null!, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("requestContextAccessor");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SagaRunner(_orchestrator, _requestContextAccessor, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        var sut = CreateSut();
        sut.ShouldNotBeNull();
    }

    #endregion

    #region RunAsync Guards

    [Fact]
    public async Task RunAsync_NullDefinition_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.RunAsync<TestSagaData>(null!, new TestSagaData()).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("definition");
    }

    [Fact]
    public async Task RunAsync_NullInitialData_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var definition = CreateMinimalDefinition();
        var act = () => sut.RunAsync(definition, (TestSagaData)null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("initialData");
    }

    [Fact]
    public async Task RunAsync_WithDefaultData_NullDefinition_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.RunAsync<TestSagaData>((BuiltSagaDefinition<TestSagaData>)null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("definition");
    }

    #endregion

    private static BuiltSagaDefinition<TestSagaData> CreateMinimalDefinition()
    {
        return SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Step1")
            .Execute((data, _, _) => ValueTask.FromResult(
                LanguageExt.Prelude.Right<EncinaError, TestSagaData>(data)))
            .Build();
    }

    private sealed class TestSagaData
    {
        public string? Value { get; set; }
    }
}
