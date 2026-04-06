using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Abstractions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// Guard tests for <see cref="DataResidencyPipelineBehavior{TRequest, TResponse}"/>
/// verifying null parameter handling in constructor and Handle method.
/// </summary>
public class DataResidencyPipelineBehaviorGuardTests
{
    private sealed record TestCommand(string Data) : IRequest<Unit>;

    private readonly IRegionContextProvider _regionCtx = Substitute.For<IRegionContextProvider>();
    private readonly IResidencyPolicyService _policyService = Substitute.For<IResidencyPolicyService>();
    private readonly ICrossBorderTransferValidator _transferValidator = Substitute.For<ICrossBorderTransferValidator>();
    private readonly IDataLocationService _locationService = Substitute.For<IDataLocationService>();
    private readonly IOptions<DataResidencyOptions> _options = Options.Create(new DataResidencyOptions());
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly Microsoft.Extensions.Logging.ILogger<DataResidencyPipelineBehavior<TestCommand, Unit>> _logger =
        NullLoggerFactory.Instance.CreateLogger<DataResidencyPipelineBehavior<TestCommand, Unit>>();

    #region Constructor Guards

    [Fact]
    public void Constructor_NullRegionContextProvider_ThrowsArgumentNullException()
    {
        var act = () => new DataResidencyPipelineBehavior<TestCommand, Unit>(
            null!, _policyService, _transferValidator, _locationService, _options, _timeProvider, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("regionContextProvider");
    }

    [Fact]
    public void Constructor_NullResidencyPolicyService_ThrowsArgumentNullException()
    {
        var act = () => new DataResidencyPipelineBehavior<TestCommand, Unit>(
            _regionCtx, null!, _transferValidator, _locationService, _options, _timeProvider, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("residencyPolicyService");
    }

    [Fact]
    public void Constructor_NullTransferValidator_ThrowsArgumentNullException()
    {
        var act = () => new DataResidencyPipelineBehavior<TestCommand, Unit>(
            _regionCtx, _policyService, null!, _locationService, _options, _timeProvider, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("transferValidator");
    }

    [Fact]
    public void Constructor_NullDataLocationService_ThrowsArgumentNullException()
    {
        var act = () => new DataResidencyPipelineBehavior<TestCommand, Unit>(
            _regionCtx, _policyService, _transferValidator, null!, _options, _timeProvider, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("dataLocationService");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DataResidencyPipelineBehavior<TestCommand, Unit>(
            _regionCtx, _policyService, _transferValidator, _locationService, null!, _timeProvider, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DataResidencyPipelineBehavior<TestCommand, Unit>(
            _regionCtx, _policyService, _transferValidator, _locationService, _options, null!, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DataResidencyPipelineBehavior<TestCommand, Unit>(
            _regionCtx, _policyService, _transferValidator, _locationService, _options, _timeProvider, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Handle Guards

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new DataResidencyPipelineBehavior<TestCommand, Unit>(
            _regionCtx, _policyService, _transferValidator, _locationService, _options, _timeProvider, _logger);

        var act = () => sut.Handle(
            null!,
            Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default),
            CancellationToken.None).AsTask();

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion
}
