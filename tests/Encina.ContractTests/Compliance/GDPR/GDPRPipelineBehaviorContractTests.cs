#pragma warning disable CA1859 // Contract tests intentionally use interface types to verify contracts

using Encina.Compliance.GDPR;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using LawfulBasisValue = global::Encina.Compliance.GDPR.LawfulBasis;

namespace Encina.ContractTests.Compliance.GDPR;

/// <summary>
/// Contract tests for <see cref="GDPRCompliancePipelineBehavior{TRequest, TResponse}"/> as an
/// <see cref="IPipelineBehavior{TRequest, TResponse}"/>: when the next step runs, when the
/// pipeline short-circuits with a <c>Left</c>, and that the outcome follows the enforcement mode.
/// </summary>
public class GDPRPipelineBehaviorContractTests
{
    [ProcessingActivity(
        Purpose = "Contract test",
        LawfulBasis = LawfulBasisValue.Contract,
        DataCategories = ["Email"],
        DataSubjects = ["Customers"],
        RetentionDays = 30)]
    private sealed record AnnotatedCommand : ICommand<Unit>;

    [ProcessesPersonalData]
    private sealed record PersonalDataCommand : ICommand<Unit>;

    private sealed record PlainCommand : ICommand<Unit>;

    private sealed class FixedValidator(Either<EncinaError, ComplianceResult> outcome) : IGDPRComplianceValidator
    {
        public ValueTask<Either<EncinaError, ComplianceResult>> ValidateAsync<TRequest>(
            TRequest request, IRequestContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(outcome);
    }

    private sealed class FailingRegistry : IProcessingActivityRegistry
    {
        public ValueTask<Either<EncinaError, Unit>> RegisterActivityAsync(ProcessingActivity activity, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Either<EncinaError, Unit>>(EncinaError.New("registry down"));

        public ValueTask<Either<EncinaError, IReadOnlyList<ProcessingActivity>>> GetAllActivitiesAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ProcessingActivity>>>(EncinaError.New("registry down"));

        public ValueTask<Either<EncinaError, Option<ProcessingActivity>>> GetActivityByRequestTypeAsync(Type requestType, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Either<EncinaError, Option<ProcessingActivity>>>(EncinaError.New("registry down"));

        public ValueTask<Either<EncinaError, Unit>> UpdateActivityAsync(ProcessingActivity activity, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Either<EncinaError, Unit>>(EncinaError.New("registry down"));
    }

    private sealed class NextProbe
    {
        public int Calls { get; private set; }

        public Either<EncinaError, Unit> Outcome { get; init; } = Unit.Default;

        public RequestHandlerCallback<Unit> Callback => () =>
        {
            Calls++;
            return ValueTask.FromResult(Outcome);
        };
    }

    private static InMemoryProcessingActivityRegistry RegistryWithAnnotatedCommand()
    {
        var registry = new InMemoryProcessingActivityRegistry();
        registry.AutoRegisterFromAssemblies([typeof(GDPRPipelineBehaviorContractTests).Assembly]);
        return registry;
    }

    private static IPipelineBehavior<TRequest, Unit> Create<TRequest>(
        IProcessingActivityRegistry registry,
        IGDPRComplianceValidator? validator = null,
        Action<GDPROptions>? configure = null)
        where TRequest : IRequest<Unit>
    {
        var options = new GDPROptions();
        configure?.Invoke(options);
        return new GDPRCompliancePipelineBehavior<TRequest, Unit>(
            registry,
            validator ?? new DefaultGDPRComplianceValidator(),
            Options.Create(options),
            NullLogger<GDPRCompliancePipelineBehavior<TRequest, Unit>>.Instance);
    }

    [Fact]
    public async Task Handle_RequestWithoutGdprAttributes_CallsNextOnceAndReturnsItsResult()
    {
        var next = new NextProbe();
        var sut = Create<PlainCommand>(new FailingRegistry());

        var result = await sut.Handle(new PlainCommand(), RequestContext.CreateForTest(), next.Callback, TestContext.Current.CancellationToken);

        result.IsRight.ShouldBeTrue();
        next.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_RegisteredCompliantActivity_CallsNextOnce()
    {
        var next = new NextProbe();
        var sut = Create<AnnotatedCommand>(RegistryWithAnnotatedCommand());

        var result = await sut.Handle(new AnnotatedCommand(), RequestContext.CreateForTest(), next.Callback, TestContext.Current.CancellationToken);

        result.IsRight.ShouldBeTrue();
        next.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_NextReturnsLeft_PropagatesTheNextError()
    {
        var next = new NextProbe { Outcome = EncinaErrors.Create("test.handler_failed", "handler failed") };
        var sut = Create<AnnotatedCommand>(RegistryWithAnnotatedCommand());

        var result = await sut.Handle(new AnnotatedCommand(), RequestContext.CreateForTest(), next.Callback, TestContext.Current.CancellationToken);

        result.IsLeft.ShouldBeTrue();
        next.Calls.ShouldBe(1);
        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe("test.handler_failed");
    }

    [Fact]
    public async Task Handle_PersonalDataAttributeOnly_UnregisteredAndNotBlocking_CallsNext()
    {
        var next = new NextProbe();
        var sut = Create<PersonalDataCommand>(new InMemoryProcessingActivityRegistry());

        var result = await sut.Handle(new PersonalDataCommand(), RequestContext.CreateForTest(), next.Callback, TestContext.Current.CancellationToken);

        result.IsRight.ShouldBeTrue();
        next.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_UnregisteredActivityAndBlocking_ReturnsLeftWithoutCallingNext()
    {
        var next = new NextProbe();
        var sut = Create<PersonalDataCommand>(
            new InMemoryProcessingActivityRegistry(), configure: o => o.BlockUnregisteredProcessing = true);

        var result = await sut.Handle(new PersonalDataCommand(), RequestContext.CreateForTest(), next.Callback, TestContext.Current.CancellationToken);

        result.IsLeft.ShouldBeTrue();
        next.Calls.ShouldBe(0);
        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe(GDPRErrors.UnregisteredActivityCode);
    }

    [Fact]
    public async Task Handle_RegistryLookupFails_ReturnsLeftWithoutCallingNext()
    {
        var next = new NextProbe();
        var sut = Create<AnnotatedCommand>(new FailingRegistry());

        var result = await sut.Handle(new AnnotatedCommand(), RequestContext.CreateForTest(), next.Callback, TestContext.Current.CancellationToken);

        result.IsLeft.ShouldBeTrue();
        next.Calls.ShouldBe(0);
        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe(GDPRErrors.RegistryLookupFailedCode);
    }

    [Fact]
    public async Task Handle_NonCompliantInEnforceMode_ReturnsLeftWithoutCallingNext()
    {
        var next = new NextProbe();
        var validator = new FixedValidator(ComplianceResult.NonCompliant("no consent"));
        var sut = Create<AnnotatedCommand>(RegistryWithAnnotatedCommand(), validator);

        var result = await sut.Handle(new AnnotatedCommand(), RequestContext.CreateForTest(), next.Callback, TestContext.Current.CancellationToken);

        result.IsLeft.ShouldBeTrue();
        next.Calls.ShouldBe(0);
        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe(GDPRErrors.ComplianceValidationFailedCode);
    }

    [Fact]
    public async Task Handle_NonCompliantInWarnOnlyMode_CallsNext()
    {
        var next = new NextProbe();
        var validator = new FixedValidator(ComplianceResult.NonCompliant("no consent"));
        var sut = Create<AnnotatedCommand>(
            RegistryWithAnnotatedCommand(), validator, o => o.EnforcementMode = GDPREnforcementMode.WarnOnly);

        var result = await sut.Handle(new AnnotatedCommand(), RequestContext.CreateForTest(), next.Callback, TestContext.Current.CancellationToken);

        result.IsRight.ShouldBeTrue();
        next.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_CompliantWithWarnings_CallsNext()
    {
        var next = new NextProbe();
        var validator = new FixedValidator(ComplianceResult.CompliantWithWarnings("retention close to limit"));
        var sut = Create<AnnotatedCommand>(RegistryWithAnnotatedCommand(), validator);

        var result = await sut.Handle(new AnnotatedCommand(), RequestContext.CreateForTest(), next.Callback, TestContext.Current.CancellationToken);

        result.IsRight.ShouldBeTrue();
        next.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_ValidatorReturnsLeft_PropagatesValidatorErrorWithoutCallingNext()
    {
        var next = new NextProbe();
        var validator = new FixedValidator(EncinaErrors.Create("test.validator_unavailable", "validator unavailable"));
        var sut = Create<AnnotatedCommand>(RegistryWithAnnotatedCommand(), validator);

        var result = await sut.Handle(new AnnotatedCommand(), RequestContext.CreateForTest(), next.Callback, TestContext.Current.CancellationToken);

        result.IsLeft.ShouldBeTrue();
        next.Calls.ShouldBe(0);
        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe("test.validator_unavailable");
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var sut = Create<AnnotatedCommand>(RegistryWithAnnotatedCommand());

        var act = () => sut.Handle(null!, RequestContext.CreateForTest(), new NextProbe().Callback, TestContext.Current.CancellationToken).AsTask();

        await Should.ThrowAsync<ArgumentNullException>(act);
    }
}
