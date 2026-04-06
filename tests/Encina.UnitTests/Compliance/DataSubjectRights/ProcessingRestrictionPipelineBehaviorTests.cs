using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Abstractions;
using Encina.Compliance.GDPR;

using FluentAssertions;

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
        DSREnforcementMode mode = DSREnforcementMode.Block) where TRequest : IRequest<Unit>
    {
        var options = Options.Create(new DataSubjectRightsOptions
        {
            RestrictionEnforcementMode = mode
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

        result.IsRight.Should().BeTrue();
        _nextStepCalled.Should().BeTrue();
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

        result.IsRight.Should().BeTrue();
        _nextStepCalled.Should().BeTrue();
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

        result.IsRight.Should().BeTrue();
        _nextStepCalled.Should().BeTrue();
        await _dsrService.Received(1).HasActiveRestrictionAsync("cust-1", Arg.Any<CancellationToken>());
        _extractor.DidNotReceive().ExtractSubjectId(Arg.Any<RestrictedCommand>(), Arg.Any<IRequestContext>());
    }

    [Fact]
    public async Task Handle_SubjectIdPropertyNotFound_ShouldFallbackToExtractor()
    {
        _extractor.ExtractSubjectId(Arg.Any<MissingPropertyCommand>(), Arg.Any<IRequestContext>())
            .Returns("fallback-subject");
        _dsrService.HasActiveRestrictionAsync("fallback-subject", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        var behavior = CreateBehavior<MissingPropertyCommand>();
        var command = new MissingPropertyCommand("subject-1");
        var next = NextStep();

        var result = await behavior.Handle(command, _context, next, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        _nextStepCalled.Should().BeTrue();
        _extractor.Received(1).ExtractSubjectId(command, _context);
    }

    [Fact]
    public async Task Handle_SubjectIdPropertyNonString_ShouldFallbackToExtractor()
    {
        _extractor.ExtractSubjectId(Arg.Any<NonStringPropertyCommand>(), Arg.Any<IRequestContext>())
            .Returns("extracted-subject");
        _dsrService.HasActiveRestrictionAsync("extracted-subject", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        var behavior = CreateBehavior<NonStringPropertyCommand>();
        var command = new NonStringPropertyCommand(42);
        var next = NextStep();

        var result = await behavior.Handle(command, _context, next, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        _extractor.Received(1).ExtractSubjectId(command, _context);
    }

    [Fact]
    public async Task Handle_SubjectIdPropertyWhitespace_ShouldFallbackToExtractor()
    {
        _extractor.ExtractSubjectId(Arg.Any<WhitespaceIdCommand>(), Arg.Any<IRequestContext>())
            .Returns("extracted-subject");
        _dsrService.HasActiveRestrictionAsync("extracted-subject", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        var behavior = CreateBehavior<WhitespaceIdCommand>();
        var command = new WhitespaceIdCommand("   ");
        var next = NextStep();

        var result = await behavior.Handle(command, _context, next, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        _extractor.Received(1).ExtractSubjectId(command, _context);
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

        result.IsRight.Should().BeTrue();
        _nextStepCalled.Should().BeTrue();
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

        result.IsRight.Should().BeTrue();
        _nextStepCalled.Should().BeTrue();
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

        result.IsLeft.Should().BeTrue();
        _nextStepCalled.Should().BeFalse();
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

        result.IsRight.Should().BeTrue();
        _nextStepCalled.Should().BeTrue();
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

        result.IsRight.Should().BeTrue();
        _nextStepCalled.Should().BeTrue();
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

        result.IsRight.Should().BeTrue();
        await _dsrService.Received(1).HasActiveRestrictionAsync("activity-subject", Arg.Any<CancellationToken>());
    }
}
