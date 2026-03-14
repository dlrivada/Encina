#pragma warning disable CA2012

using Encina.Compliance.PrivacyByDesign;

namespace Encina.GuardTests.Compliance.PrivacyByDesign;

/// <summary>
/// Guard tests for <see cref="DataMinimizationPipelineBehavior{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class DataMinimizationPipelineBehaviorGuardTests
{
    private readonly IPrivacyByDesignValidator _validator = Substitute.For<IPrivacyByDesignValidator>();
    private readonly IOptions<PrivacyByDesignOptions> _options = Options.Create(new PrivacyByDesignOptions());
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<DataMinimizationPipelineBehavior<TestRequest, string>> _logger =
        NullLogger<DataMinimizationPipelineBehavior<TestRequest, string>>.Instance;
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when validator is null.
    /// </summary>
    [Fact]
    public void Constructor_NullValidator_ThrowsArgumentNullException()
    {
        var act = () => new DataMinimizationPipelineBehavior<TestRequest, string>(
            null!, _options, _timeProvider, _logger, _serviceProvider);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("validator");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DataMinimizationPipelineBehavior<TestRequest, string>(
            _validator, null!, _timeProvider, _logger, _serviceProvider);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DataMinimizationPipelineBehavior<TestRequest, string>(
            _validator, _options, null!, _logger, _serviceProvider);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DataMinimizationPipelineBehavior<TestRequest, string>(
            _validator, _options, _timeProvider, null!, _serviceProvider);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when serviceProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new DataMinimizationPipelineBehavior<TestRequest, string>(
            _validator, _options, _timeProvider, _logger, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
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
        var request = new TestRequest();
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
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(request, context, null!, CancellationToken.None));
        ex.ParamName.ShouldBe("nextStep");
    }

    #endregion

    #region Helpers

    private DataMinimizationPipelineBehavior<TestRequest, string> CreateSut() =>
        new(_validator, _options, _timeProvider, _logger, _serviceProvider);

    public sealed record TestRequest : IRequest<string>;

    #endregion
}
