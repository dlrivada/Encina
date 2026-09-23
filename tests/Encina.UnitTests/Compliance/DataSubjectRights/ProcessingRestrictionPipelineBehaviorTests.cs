using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Abstractions;
using Encina.Compliance.GDPR;

using Shouldly;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for <see cref="ProcessingRestrictionPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class ProcessingRestrictionPipelineBehaviorTests
{
    // ================================================================
    // Test request types — each with different attribute configuration
    // ================================================================

    private sealed record PlainCommand(string Data) : IRequest<Unit>;

    [RestrictProcessing(SubjectIdProperty = nameof(CustomerId))]
    private sealed record RestrictedCommand(string CustomerId, string NewEmail) : IRequest<Unit>;

    [ProcessesPersonalData]
    private sealed record PersonalDataCommand(string SubjectId) : IRequest<Unit>;

    [ProcessingActivity(
        Purpose = "Order fulfillment",
        LawfulBasis = LawfulBasis.Contract,
        DataCategories = ["Name", "Email"],
        DataSubjects = ["Customers"],
        RetentionDays = 365)]
    private sealed record ActivityCommand(string CustomerId) : IRequest<Unit>;

    [RestrictProcessing(SubjectIdProperty = "NonExistentProperty")]
    private sealed record MissingPropertyCommand(string SubjectId) : IRequest<Unit>;

    [RestrictProcessing(SubjectIdProperty = nameof(NumericId))]
    private sealed record NonStringPropertyCommand(int NumericId) : IRequest<Unit>;

    [RestrictProcessing(SubjectIdProperty = nameof(CustomerId))]
    private sealed record WhitespaceIdCommand(string CustomerId) : IRequest<Unit>;

    [RestrictProcessing(SubjectIdProperty = nameof(PatientId))]
    private sealed record GuidIdCommand(Guid PatientId) : IRequest<Unit>;

    private readonly record struct PatientId(Guid Value);

    [RestrictProcessing(SubjectIdProperty = nameof(Patient))]
    private sealed record StronglyTypedIdCommand(PatientId Patient) : IRequest<Unit>;

    private readonly record struct FormattablePatientId(Guid Value) : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString("N", formatProvider);
    }

    [RestrictProcessing(SubjectIdProperty = nameof(Patient))]
    private sealed record FormattableIdCommand(FormattablePatientId Patient) : IRequest<Unit>;

    [RestrictProcessing(SubjectIdProperty = nameof(Score))]
    private sealed record UnsupportedIdCommand(double Score) : IRequest<Unit>;

    [RestrictProcessing]
    private sealed record ExtractorOnlyRestrictedCommand(string Payload) : IRequest<Unit>;

    // ================================================================
    // Shared setup
    // ================================================================

    private readonly IDSRService _dsrService = Substitute.For<IDSRService>();
    private readonly IDataSubjectIdExtractor _extractor = Substitute.For<IDataSubjectIdExtractor>();
    private readonly IRequestContext _context = Substitute.For<IRequestContext>();

    private static bool _nextStepCalled;

    private static RequestHandlerCallback<Unit> NextStep()
    {
        _nextStepCalled = false;
        return () =>
        {
            _nextStepCalled = true;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };
    }

    private ProcessingRestrictionPipelineBehavior<TRequest, Unit> CreateBehavior<TRequest>(
        DSREnforcementMode mode = DSREnforcementMode.Block,
        bool failClosedOnMissingSubjectId = true) where TRequest : IRequest<Unit>
    {
        var options = Options.Create(new DataSubjectRightsOptions
        {
            RestrictionEnforcementMode = mode,
            FailClosedOnMissingSubjectId = failClosedOnMissingSubjectId
        });

        return new ProcessingRestrictionPipelineBehavior<TRequest, Unit>(
            _dsrService,
            _extractor,
            options,
            NullLoggerFactory.Instance.CreateLogger<ProcessingRestrictionPipelineBehavior<TRequest, Unit>>());
    }

    // ================================================================
    // Disabled mode
    // ================================================================

    [Fact]
    public async Task Handle_DisabledMode_ShouldCallNextWithoutChecking()
    {
        var behavior = CreateBehavior<RestrictedCommand>(DSREnforcementMode.Disabled);
        var command = new RestrictedCommand("cust-1", "new@email.com");
        var next = NextStep();

        var result = await behavior.Handle(command, _context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _nextStepCalled.ShouldBeTrue();
        await _dsrService.DidNotReceive().HasActiveRestrictionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ================================================================
    // No attributes
    // ================================================================

    [Fact]
    public async Task Handle_NoAttributes_ShouldSkipAndCallNext()
    {
        var behavior = CreateBehavior<PlainCommand>();
        var command = new PlainCommand("data");
        var next = NextStep();

        var result = await behavior.Handle(command, _context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _nextStepCalled.ShouldBeTrue();
        await _dsrService.DidNotReceive().HasActiveRestrictionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ================================================================
    // Subject ID extraction
    // ================================================================

    [Fact]
    public async Task Handle_SubjectIdFromProperty_ShouldUseReflectionAndCheckRestriction()
    {
        _dsrService.HasActiveRestrictionAsync("cust-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        var behavior = CreateBehavior<RestrictedCommand>();
        var command = new RestrictedCommand("cust-1", "new@email.com");
        var next = NextStep();

        var result = await behavior.Handle(command, _context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _nextStepCalled.ShouldBeTrue();
        await _dsrService.Received(1).HasActiveRestrictionAsync("cust-1", Arg.Any<CancellationToken>());
        _extractor.DidNotReceive().ExtractSubjectId(Arg.Any<RestrictedCommand>(), Arg.Any<IRequestContext>());
    }

    [Fact]
    public async Task Handle_SubjectIdPropertyNotFound_ShouldThrowConfigurationError()
    {
        // An explicit SubjectIdProperty that does not exist is a configuration error: the behavior
        // must not fall back to the extractor (and from there to the authenticated caller).
        _extractor.ExtractSubjectId(Arg.Any<MissingPropertyCommand>(), Arg.Any<IRequestContext>())
            .Returns("fallback-subject");

        var behavior = CreateBehavior<MissingPropertyCommand>();
        var command = new MissingPropertyCommand("subject-1");
        var next = NextStep();

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            async () => await behavior.Handle(command, _context, next, CancellationToken.None));

        ex.Message.ShouldContain("NonExistentProperty");
        _nextStepCalled.ShouldBeFalse();
        _extractor.DidNotReceive().ExtractSubjectId(Arg.Any<MissingPropertyCommand>(), Arg.Any<IRequestContext>());
        await _dsrService.DidNotReceive().HasActiveRestrictionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SubjectIdPropertyInteger_ShouldCheckTheConvertedValue()
    {
        _dsrService.HasActiveRestrictionAsync("42", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        var behavior = CreateBehavior<NonStringPropertyCommand>();
        var command = new NonStringPropertyCommand(42);
        var next = NextStep();

        var result = await behavior.Handle(command, _context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _dsrService.Received(1).HasActiveRestrictionAsync("42", Arg.Any<CancellationToken>());
        _extractor.DidNotReceive().ExtractSubjectId(Arg.Any<NonStringPropertyCommand>(), Arg.Any<IRequestContext>());
    }

    [Fact]
    public async Task Handle_SubjectIdPropertyWhitespace_BlockMode_ShouldFailClosed_NotFallBackToExtractor()
    {
        _extractor.ExtractSubjectId(Arg.Any<WhitespaceIdCommand>(), Arg.Any<IRequestContext>())
            .Returns("the-caller");

        var behavior = CreateBehavior<WhitespaceIdCommand>();
        var command = new WhitespaceIdCommand("   ");
        var next = NextStep();

        var result = await behavior.Handle(command, _context, next, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.SubjectIdMissingCode));
        _nextStepCalled.ShouldBeFalse();
        _extractor.DidNotReceive().ExtractSubjectId(Arg.Any<WhitespaceIdCommand>(), Arg.Any<IRequestContext>());
        await _dsrService.DidNotReceive().HasActiveRestrictionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ================================================================
    // Subject ID conversion on the explicit SubjectIdProperty path (#1149)
    // ================================================================

    [Fact]
    public async Task Handle_GuidSubjectIdProperty_ShouldCheckTheGuidInDFormat()
    {
        var patientId = Guid.NewGuid();
        _dsrService.HasActiveRestrictionAsync(patientId.ToString("D"), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));

        var behavior = CreateBehavior<GuidIdCommand>();
        var next = NextStep();

        var result = await behavior.Handle(new GuidIdCommand(patientId), _context, next, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.RestrictionActiveCode));
        _nextStepCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_RecordStructSubjectIdProperty_ShouldUnwrapValue()
    {
        var patientId = Guid.NewGuid();
        _dsrService.HasActiveRestrictionAsync(patientId.ToString("D"), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        var behavior = CreateBehavior<StronglyTypedIdCommand>();
        var next = NextStep();

        var result = await behavior.Handle(new StronglyTypedIdCommand(new PatientId(patientId)), _context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _dsrService.Received(1).HasActiveRestrictionAsync(patientId.ToString("D"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnsupportedSubjectIdPropertyType_ShouldThrowConfigurationError()
    {
        var behavior = CreateBehavior<UnsupportedIdCommand>();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await behavior.Handle(new UnsupportedIdCommand(1.5), _context, NextStep(), CancellationToken.None));

        await _dsrService.DidNotReceive().HasActiveRestrictionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ================================================================
    // Missing subject on [RestrictProcessing] requests fails closed
    // ================================================================

    [Fact]
    public async Task Handle_RestrictProcessing_EmptyGuidSubject_BlockMode_ShouldReturnSubjectIdMissing()
    {
        var behavior = CreateBehavior<GuidIdCommand>(DSREnforcementMode.Block);
        var next = NextStep();

        var result = await behavior.Handle(new GuidIdCommand(Guid.Empty), _context, next, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.SubjectIdMissingCode));
        _nextStepCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_RestrictProcessing_FormattableWrapperOfEmptyGuid_BlockMode_ShouldReturnSubjectIdMissing()
    {
        // The wrapper implements IFormattable, but its Value (Guid.Empty) decides: the subject is missing.
        var behavior = CreateBehavior<FormattableIdCommand>(DSREnforcementMode.Block);
        var next = NextStep();

        var result = await behavior.Handle(
            new FormattableIdCommand(new FormattablePatientId(Guid.Empty)), _context, next, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.SubjectIdMissingCode));
        _nextStepCalled.ShouldBeFalse();
        await _dsrService.DidNotReceive().HasActiveRestrictionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RestrictProcessing_NoSubjectFromExtractor_BlockMode_ShouldReturnSubjectIdMissing()
    {
        _extractor.ExtractSubjectId(Arg.Any<ExtractorOnlyRestrictedCommand>(), Arg.Any<IRequestContext>())
            .Returns((string?)null);

        var behavior = CreateBehavior<ExtractorOnlyRestrictedCommand>(DSREnforcementMode.Block);
        var next = NextStep();

        var result = await behavior.Handle(new ExtractorOnlyRestrictedCommand("payload"), _context, next, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.SubjectIdMissingCode));
        _nextStepCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_RestrictProcessing_MissingSubject_WarnMode_ShouldCallNext()
    {
        var behavior = CreateBehavior<GuidIdCommand>(DSREnforcementMode.Warn);
        var next = NextStep();

        var result = await behavior.Handle(new GuidIdCommand(Guid.Empty), _context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _nextStepCalled.ShouldBeTrue();
        await _dsrService.DidNotReceive().HasActiveRestrictionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RestrictProcessing_MissingSubject_FailClosedDisabled_ShouldSkipAndCallNext()
    {
        var behavior = CreateBehavior<GuidIdCommand>(DSREnforcementMode.Block, failClosedOnMissingSubjectId: false);
        var next = NextStep();

        var result = await behavior.Handle(new GuidIdCommand(Guid.Empty), _context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _nextStepCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_NoSubjectId_ShouldSkipAndCallNext()
    {
        _extractor.ExtractSubjectId(Arg.Any<PersonalDataCommand>(), Arg.Any<IRequestContext>())
            .Returns((string?)null);

        var behavior = CreateBehavior<PersonalDataCommand>();
        var command = new PersonalDataCommand("subject-1");
        var next = NextStep();

        var result = await behavior.Handle(command, _context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _nextStepCalled.ShouldBeTrue();
        await _dsrService.DidNotReceive().HasActiveRestrictionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ================================================================
    // Restriction enforcement
    // ================================================================

    [Fact]
    public async Task Handle_NotRestricted_ShouldCallNext()
    {
        _extractor.ExtractSubjectId(Arg.Any<PersonalDataCommand>(), Arg.Any<IRequestContext>())
            .Returns("subject-1");
        _dsrService.HasActiveRestrictionAsync("subject-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        var behavior = CreateBehavior<PersonalDataCommand>();
        var command = new PersonalDataCommand("subject-1");
        var next = NextStep();

        var result = await behavior.Handle(command, _context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _nextStepCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_Restricted_BlockMode_ShouldReturnError()
    {
        _extractor.ExtractSubjectId(Arg.Any<PersonalDataCommand>(), Arg.Any<IRequestContext>())
            .Returns("restricted-subject");
        _dsrService.HasActiveRestrictionAsync("restricted-subject", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));

        var behavior = CreateBehavior<PersonalDataCommand>(DSREnforcementMode.Block);
        var command = new PersonalDataCommand("restricted-subject");
        var next = NextStep();

        var result = await behavior.Handle(command, _context, next, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        _nextStepCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_Restricted_WarnMode_ShouldLogAndCallNext()
    {
        _extractor.ExtractSubjectId(Arg.Any<PersonalDataCommand>(), Arg.Any<IRequestContext>())
            .Returns("restricted-subject");
        _dsrService.HasActiveRestrictionAsync("restricted-subject", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));

        var behavior = CreateBehavior<PersonalDataCommand>(DSREnforcementMode.Warn);
        var command = new PersonalDataCommand("restricted-subject");
        var next = NextStep();

        var result = await behavior.Handle(command, _context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _nextStepCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_StoreError_ShouldFailOpenAndCallNext()
    {
        var storeError = EncinaErrors.Create("store.error", "DB unavailable");

        _extractor.ExtractSubjectId(Arg.Any<PersonalDataCommand>(), Arg.Any<IRequestContext>())
            .Returns("subject-1");
        _dsrService.HasActiveRestrictionAsync("subject-1", Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, bool>(storeError));

        var behavior = CreateBehavior<PersonalDataCommand>();
        var command = new PersonalDataCommand("subject-1");
        var next = NextStep();

        var result = await behavior.Handle(command, _context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _nextStepCalled.ShouldBeTrue();
    }

    // ================================================================
    // Different attribute types
    // ================================================================

    [Fact]
    public async Task Handle_ProcessingActivityAttribute_ShouldCheckRestriction()
    {
        _extractor.ExtractSubjectId(Arg.Any<ActivityCommand>(), Arg.Any<IRequestContext>())
            .Returns("activity-subject");
        _dsrService.HasActiveRestrictionAsync("activity-subject", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        var behavior = CreateBehavior<ActivityCommand>();
        var command = new ActivityCommand("activity-subject");
        var next = NextStep();

        var result = await behavior.Handle(command, _context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _dsrService.Received(1).HasActiveRestrictionAsync("activity-subject", Arg.Any<CancellationToken>());
    }
}
