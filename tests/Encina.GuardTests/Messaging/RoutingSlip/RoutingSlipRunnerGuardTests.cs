using Encina.Messaging.RoutingSlip;
using FluentAssertions;

namespace Encina.GuardTests.Messaging.RoutingSlip;

/// <summary>
/// Guard clause tests for RoutingSlipRunner constructor and RunAsync method parameters.
/// </summary>
public class RoutingSlipRunnerGuardTests
{
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly RoutingSlipOptions _options = new();
    private readonly ILogger<RoutingSlipRunner> _logger = NullLogger<RoutingSlipRunner>.Instance;

    private RoutingSlipRunner CreateSut() => new(_requestContext, _options, _logger);

    #region Constructor Guards

    [Fact]
    public void Constructor_NullRequestContext_ThrowsArgumentNullException()
    {
        var act = () => new RoutingSlipRunner(null!, _options, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("requestContext");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new RoutingSlipRunner(_requestContext, null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new RoutingSlipRunner(_requestContext, _options, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullTimeProvider_UsesSystemDefault()
    {
        var act = () => new RoutingSlipRunner(_requestContext, _options, _logger, timeProvider: null);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        var sut = CreateSut();
        sut.Should().NotBeNull();
    }

    #endregion

    #region RunAsync Guards

    [Fact]
    public async Task RunAsync_NullDefinition_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.RunAsync<TestSlipData>(null!, new TestSlipData()).AsTask();
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("definition");
    }

    [Fact]
    public async Task RunAsync_NullInitialData_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var definition = CreateMinimalDefinition();
        var act = () => sut.RunAsync(definition, (TestSlipData)null!).AsTask();
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("initialData");
    }

    [Fact]
    public async Task RunAsync_WithDefaultData_NullDefinition_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.RunAsync<TestSlipData>((BuiltRoutingSlipDefinition<TestSlipData>)null!).AsTask();
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("definition");
    }

    [Fact]
    public async Task RunAsync_ValidParameters_Succeeds()
    {
        var sut = CreateSut();
        var definition = CreateMinimalDefinition();

        var result = await sut.RunAsync(definition, new TestSlipData());

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public void Build_EmptyStepList_ThrowsInvalidOperationException()
    {
        var act = () => RoutingSlipBuilder.Create<TestSlipData>("EmptySlip").Build();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one step*");
    }

    [Fact]
    public async Task RunAsync_StepFails_ReturnsError()
    {
        var sut = CreateSut();
        var definition = RoutingSlipBuilder.Create<TestSlipData>("FailSlip")
            .Step("FailStep")
            .Execute((_, _, _) => ValueTask.FromResult(
                LanguageExt.Prelude.Left<EncinaError, TestSlipData>(EncinaError.New("step failed"))))
            .Build();

        var result = await sut.RunAsync(definition, new TestSlipData());

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region RoutingSlipOptions Defaults

    [Fact]
    public void RoutingSlipOptions_DefaultTimeout_IsPositive()
    {
        var options = new RoutingSlipOptions();
        options.DefaultTimeout.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void RoutingSlipOptions_DefaultBatchSize_IsPositive()
    {
        var options = new RoutingSlipOptions();
        options.BatchSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RoutingSlipOptions_ContinueCompensationOnFailure_DefaultTrue()
    {
        var options = new RoutingSlipOptions();
        options.ContinueCompensationOnFailure.Should().BeTrue();
    }

    #endregion

    private static BuiltRoutingSlipDefinition<TestSlipData> CreateMinimalDefinition()
    {
        return RoutingSlipBuilder.Create<TestSlipData>("TestSlip")
            .Step("Step1")
            .Execute((data, _, _) => ValueTask.FromResult(
                LanguageExt.Prelude.Right<EncinaError, TestSlipData>(data)))
            .Build();
    }

    private sealed class TestSlipData
    {
        public string? Value { get; set; }
    }
}
