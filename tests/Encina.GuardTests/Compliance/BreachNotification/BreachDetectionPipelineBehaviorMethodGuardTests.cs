using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Method-level guard tests for <see cref="BreachDetectionPipelineBehavior{TRequest, TResponse}"/>
/// Handle method null parameter handling.
/// </summary>
/// <remarks>
/// The Handle method only guards the <c>request</c> parameter with
/// <c>ArgumentNullException.ThrowIfNull</c>. The <c>context</c> and <c>nextStep</c>
/// parameters are not guarded in the current implementation. This test class covers
/// the existing guard and documents the non-guarded parameters.
/// </remarks>
public sealed class BreachDetectionPipelineBehaviorMethodGuardTests
{
    private sealed record TestCommand(string Data) : IRequest<Unit>;

    private readonly IBreachDetector _detector = Substitute.For<IBreachDetector>();
    private readonly IBreachNotificationService _breachService = Substitute.For<IBreachNotificationService>();
    private readonly IOptions<BreachNotificationOptions> _options = Options.Create(new BreachNotificationOptions());
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly ILogger<BreachDetectionPipelineBehavior<TestCommand, Unit>> _logger =
        NullLogger<BreachDetectionPipelineBehavior<TestCommand, Unit>>.Instance;

    #region Handle Guards

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when request is null,
    /// even in Disabled enforcement mode.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_DisabledMode_ThrowsArgumentNullException()
    {
        var options = Options.Create(new BreachNotificationOptions
        {
            EnforcementMode = BreachDetectionEnforcementMode.Disabled
        });
        var behavior = new BreachDetectionPipelineBehavior<TestCommand, Unit>(
            _detector, _breachService, options, _timeProvider, _logger);
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<Unit> next = () => ValueTask.FromResult(
            LanguageExt.Either<EncinaError, Unit>.Right(Unit.Default));

        var act = async () => await behavior.Handle(null!, context, next, CancellationToken.None);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when request is null
    /// in Block enforcement mode.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_BlockMode_ThrowsArgumentNullException()
    {
        var options = Options.Create(new BreachNotificationOptions
        {
            EnforcementMode = BreachDetectionEnforcementMode.Block
        });
        var behavior = new BreachDetectionPipelineBehavior<TestCommand, Unit>(
            _detector, _breachService, options, _timeProvider, _logger);
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<Unit> next = () => ValueTask.FromResult(
            LanguageExt.Either<EncinaError, Unit>.Right(Unit.Default));

        var act = async () => await behavior.Handle(null!, context, next, CancellationToken.None);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when request is null
    /// in Warn enforcement mode.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_WarnMode_ThrowsArgumentNullException()
    {
        var options = Options.Create(new BreachNotificationOptions
        {
            EnforcementMode = BreachDetectionEnforcementMode.Warn
        });
        var behavior = new BreachDetectionPipelineBehavior<TestCommand, Unit>(
            _detector, _breachService, options, _timeProvider, _logger);
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<Unit> next = () => ValueTask.FromResult(
            LanguageExt.Either<EncinaError, Unit>.Right(Unit.Default));

        var act = async () => await behavior.Handle(null!, context, next, CancellationToken.None);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    #endregion
}
