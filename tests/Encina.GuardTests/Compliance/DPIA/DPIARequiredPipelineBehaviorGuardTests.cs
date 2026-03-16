using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Model;

namespace Encina.GuardTests.Compliance.DPIA;

/// <summary>
/// Guard tests for <see cref="DPIARequiredPipelineBehavior{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class DPIARequiredPipelineBehaviorGuardTests
{
    private readonly IDPIAService _service = Substitute.For<IDPIAService>();
    private readonly IOptions<DPIAOptions> _options = Options.Create(new DPIAOptions());
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<DPIARequiredPipelineBehavior<TestCommand, string>> _logger =
        NullLogger<DPIARequiredPipelineBehavior<TestCommand, string>>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when service is null.
    /// </summary>
    [Fact]
    public void Constructor_NullService_ThrowsArgumentNullException()
    {
        var act = () => new DPIARequiredPipelineBehavior<TestCommand, string>(
            null!, _options, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("service");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DPIARequiredPipelineBehavior<TestCommand, string>(
            _service, null!, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DPIARequiredPipelineBehavior<TestCommand, string>(
            _service, _options, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DPIARequiredPipelineBehavior<TestCommand, string>(
            _service, _options, _timeProvider, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region Handle Guards

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var context = Substitute.For<IRequestContext>();
        var nextStep = Substitute.For<RequestHandlerCallback<string>>();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(null!, context, nextStep, CancellationToken.None));
        ex.ParamName.ShouldBe("request");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var request = new TestCommand();
        var nextStep = Substitute.For<RequestHandlerCallback<string>>();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(request, null!, nextStep, CancellationToken.None));
        ex.ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when nextStep is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var request = new TestCommand();
        var context = Substitute.For<IRequestContext>();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(request, context, null!, CancellationToken.None));
        ex.ParamName.ShouldBe("nextStep");
    }

    #endregion

    #region Helpers

    private DPIARequiredPipelineBehavior<TestCommand, string> CreateSut() =>
        new(_service, _options, _timeProvider, _logger);

    public sealed record TestCommand : ICommand<string>;

    #endregion
}
