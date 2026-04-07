#pragma warning disable CA2012

using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Compliance.BreachNotification;

/// <summary>
/// Contract tests verifying <see cref="BreachDetectionPipelineBehavior{TRequest, TResponse}"/>
/// behavior contract: disabled mode skips detection, no attribute skips detection.
/// These tests exercise the actual pipeline behavior implementation.
/// </summary>
[Trait("Category", "Contract")]
public class BreachDetectionPipelineBehaviorContractTests
{
    private sealed record PlainCommand(string Data) : IRequest<Unit>;

    [Fact]
    public async Task Contract_DisabledMode_ShouldSkipDetectionAndCallNext()
    {
        var options = Options.Create(new BreachNotificationOptions
        {
            EnforcementMode = BreachDetectionEnforcementMode.Disabled
        });

        var behavior = new BreachDetectionPipelineBehavior<PlainCommand, Unit>(
            Substitute.For<IBreachDetector>(),
            Substitute.For<IBreachNotificationService>(),
            options,
            TimeProvider.System,
            NullLogger<BreachDetectionPipelineBehavior<PlainCommand, Unit>>.Instance);

        var nextCalled = false;
        var result = await behavior.Handle(
            new PlainCommand("test"),
            Substitute.For<IRequestContext>(),
            () =>
            {
                nextCalled = true;
                return ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
            },
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_NoAttribute_ShouldSkipDetectionAndCallNext()
    {
        var options = Options.Create(new BreachNotificationOptions
        {
            EnforcementMode = BreachDetectionEnforcementMode.Block
        });

        var behavior = new BreachDetectionPipelineBehavior<PlainCommand, Unit>(
            Substitute.For<IBreachDetector>(),
            Substitute.For<IBreachNotificationService>(),
            options,
            TimeProvider.System,
            NullLogger<BreachDetectionPipelineBehavior<PlainCommand, Unit>>.Instance);

        var result = await behavior.Handle(
            new PlainCommand("test"),
            Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }
}
