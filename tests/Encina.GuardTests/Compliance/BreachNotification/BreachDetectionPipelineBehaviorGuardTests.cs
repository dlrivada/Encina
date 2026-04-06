using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Abstractions;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="BreachDetectionPipelineBehavior{TRequest, TResponse}"/>
/// constructor and Handle method null parameter handling.
/// </summary>
public sealed class BreachDetectionPipelineBehaviorGuardTests
{
    private sealed record TestCommand(string Data) : IRequest<Unit>;

    private readonly IBreachDetector _detector = Substitute.For<IBreachDetector>();
    private readonly IBreachNotificationService _breachService = Substitute.For<IBreachNotificationService>();
    private readonly IOptions<BreachNotificationOptions> _options = Options.Create(new BreachNotificationOptions());
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly ILogger<BreachDetectionPipelineBehavior<TestCommand, Unit>> _logger =
        NullLogger<BreachDetectionPipelineBehavior<TestCommand, Unit>>.Instance;

    #region Constructor Guards

    [Fact]
    public void Constructor_NullDetector_ThrowsArgumentNullException()
    {
        var act = () => new BreachDetectionPipelineBehavior<TestCommand, Unit>(
            null!, _breachService, _options, _timeProvider, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("detector");
    }

    [Fact]
    public void Constructor_NullBreachService_ThrowsArgumentNullException()
    {
        var act = () => new BreachDetectionPipelineBehavior<TestCommand, Unit>(
            _detector, null!, _options, _timeProvider, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("breachService");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new BreachDetectionPipelineBehavior<TestCommand, Unit>(
            _detector, _breachService, null!, _timeProvider, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new BreachDetectionPipelineBehavior<TestCommand, Unit>(
            _detector, _breachService, _options, null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new BreachDetectionPipelineBehavior<TestCommand, Unit>(
            _detector, _breachService, _options, _timeProvider, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Handle Guards

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var behavior = new BreachDetectionPipelineBehavior<TestCommand, Unit>(
            _detector, _breachService, _options, _timeProvider, _logger);

        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<Unit> next = () => ValueTask.FromResult(
            LanguageExt.Either<EncinaError, Unit>.Right(Unit.Default));

        var act = async () => await behavior.Handle(null!, context, next, CancellationToken.None);

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion
}
