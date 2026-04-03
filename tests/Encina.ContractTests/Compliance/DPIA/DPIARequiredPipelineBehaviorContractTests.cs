using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Model;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.ContractTests.Compliance.DPIA;

/// <summary>
/// Behavioral contract tests for <see cref="DPIARequiredPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
[Trait("Category", "Contract")]
public sealed class DPIARequiredPipelineBehaviorContractTests
{
    [Fact]
    public void ImplementsIPipelineBehavior()
    {
        typeof(IPipelineBehavior<TestDPIACommand, string>).IsAssignableFrom(
            typeof(DPIARequiredPipelineBehavior<TestDPIACommand, string>)).ShouldBeTrue();
    }

    [Fact]
    public void IsSealed()
    {
        typeof(DPIARequiredPipelineBehavior<TestDPIACommand, string>).IsSealed.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_DisabledMode_SkipsCheckAndCallsNext()
    {
        var service = Substitute.For<IDPIAService>();
        var options = Options.Create(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Disabled });
        var logger = NullLogger<DPIARequiredPipelineBehavior<TestDPIACommand, string>>.Instance;
        var behavior = new DPIARequiredPipelineBehavior<TestDPIACommand, string>(service, options, TimeProvider.System, logger);
        var context = CreateContext();
        var called = false;

        RequestHandlerCallback<string> next = () =>
        {
            called = true;
            return new ValueTask<Either<EncinaError, string>>(Either<EncinaError, string>.Right("ok"));
        };

        var result = await behavior.Handle(new TestDPIACommand("v"), context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        called.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_NoAttribute_SkipsCheckAndCallsNext()
    {
        var service = Substitute.For<IDPIAService>();
        var options = Options.Create(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Block });
        var logger = NullLogger<DPIARequiredPipelineBehavior<TestNoDPIACommand, string>>.Instance;
        var behavior = new DPIARequiredPipelineBehavior<TestNoDPIACommand, string>(service, options, TimeProvider.System, logger);
        var context = CreateContext();
        var called = false;

        RequestHandlerCallback<string> next = () =>
        {
            called = true;
            return new ValueTask<Either<EncinaError, string>>(Either<EncinaError, string>.Right("ok"));
        };

        var result = await behavior.Handle(new TestNoDPIACommand("v"), context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        called.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_NullService_Throws()
    {
        var options = Options.Create(new DPIAOptions());
        var logger = NullLogger<DPIARequiredPipelineBehavior<TestDPIACommand, string>>.Instance;
        Should.Throw<ArgumentNullException>(
            () => new DPIARequiredPipelineBehavior<TestDPIACommand, string>(null!, options, TimeProvider.System, logger));
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        var service = Substitute.For<IDPIAService>();
        var logger = NullLogger<DPIARequiredPipelineBehavior<TestDPIACommand, string>>.Instance;
        Should.Throw<ArgumentNullException>(
            () => new DPIARequiredPipelineBehavior<TestDPIACommand, string>(service, null!, TimeProvider.System, logger));
    }

    private static IRequestContext CreateContext()
    {
        var ctx = Substitute.For<IRequestContext>();
        ctx.CorrelationId.Returns("corr-1");
        return ctx;
    }

    [RequiresDPIA(ProcessingType = "AutomatedDecisionMaking")]
    public sealed record TestDPIACommand(string Value) : ICommand<string>;
    public sealed record TestNoDPIACommand(string Value) : ICommand<string>;
}
