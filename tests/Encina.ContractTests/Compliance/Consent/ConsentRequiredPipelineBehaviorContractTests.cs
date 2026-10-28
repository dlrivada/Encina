using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Abstractions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // NSubstitute mock setup for ValueTask-returning method

namespace Encina.ContractTests.Compliance.Consent;

/// <summary>
/// Contract tests for <see cref="ConsentRequiredPipelineBehavior{TRequest, TResponse}"/>: only requests
/// decorated with <see cref="RequireConsentAttribute"/> are checked, the enforcement mode decides whether a
/// missing consent blocks, and a missing subject or a validator failure never reports success. Runs the real
/// behavior against a substituted <see cref="IConsentValidator"/>.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Consent")]
public sealed class ConsentRequiredPipelineBehaviorContractTests
{
    private const string Purpose = "marketing";

    [RequireConsent(Purpose, SubjectIdProperty = nameof(SubjectId))]
    private sealed record GuardedRequest(string SubjectId) : IRequest<string>;

    [RequireConsent(Purpose, SubjectIdProperty = nameof(SubjectId))]
    private sealed record GuardedGuidRequest(Guid SubjectId) : IRequest<string>;

    [RequireConsent(Purpose, SubjectIdProperty = "DoesNotExist")]
    private sealed record MisconfiguredRequest(string SubjectId) : IRequest<string>;

    private sealed record PlainRequest(string SubjectId) : IRequest<string>;

    private readonly IConsentValidator _validator = Substitute.For<IConsentValidator>();

    private ConsentRequiredPipelineBehavior<TRequest, string> CreateSut<TRequest>(ConsentEnforcementMode mode)
        where TRequest : IRequest<string> =>
        new(
            _validator,
            Options.Create(new ConsentOptions { EnforcementMode = mode }),
            NullLogger<ConsentRequiredPipelineBehavior<TRequest, string>>.Instance);

    private void ValidatorReturns(Either<EncinaError, ConsentValidationResult> result) =>
        _validator
            .ValidateAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, ConsentValidationResult>>(result));

    private static RequestHandlerCallback<string> Next(string value, Action? onCalled = null) =>
        () =>
        {
            onCalled?.Invoke();
            return new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>(value));
        };

    private static string CodeOf(Either<EncinaError, string> result) =>
        result.Match(_ => string.Empty, error => error.GetCode().IfNone(string.Empty));

    [Fact]
    public async Task Handle_EnforcementDisabled_SkipsValidationAndProceeds()
    {
        var sut = CreateSut<GuardedRequest>(ConsentEnforcementMode.Disabled);

        var result = await sut.Handle(new GuardedRequest("subject-1"), RequestContext.CreateForTest(), Next("done"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _validator.DidNotReceiveWithAnyArgs().ValidateAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_RequestWithoutAttribute_SkipsValidationAndProceeds()
    {
        var sut = CreateSut<PlainRequest>(ConsentEnforcementMode.Block);

        var result = await sut.Handle(new PlainRequest("subject-1"), RequestContext.CreateForTest(), Next("done"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _validator.DidNotReceiveWithAnyArgs().ValidateAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_ConsentValid_ValidatesTheDeclaredPurposeAndProceeds()
    {
        ValidatorReturns(Right<EncinaError, ConsentValidationResult>(ConsentValidationResult.Valid()));
        var sut = CreateSut<GuardedRequest>(ConsentEnforcementMode.Block);

        var result = await sut.Handle(new GuardedRequest("subject-1"), RequestContext.CreateForTest(), Next("done"), CancellationToken.None);

        result.Match(value => value, _ => string.Empty).ShouldBe("done");
        await _validator.Received(1).ValidateAsync(
            "subject-1",
            Arg.Is<IEnumerable<string>>(purposes => purposes.SequenceEqual(new[] { Purpose })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConsentValidWithWarnings_Proceeds()
    {
        ValidatorReturns(Right<EncinaError, ConsentValidationResult>(ConsentValidationResult.ValidWithWarnings("expires soon")));
        var sut = CreateSut<GuardedRequest>(ConsentEnforcementMode.Block);

        var result = await sut.Handle(new GuardedRequest("subject-1"), RequestContext.CreateForTest(), Next("done"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ConsentMissingInBlockMode_ReturnsMissingConsentAndDoesNotCallNext()
    {
        ValidatorReturns(Right<EncinaError, ConsentValidationResult>(
            ConsentValidationResult.Invalid(["No consent record found."], [Purpose])));
        var sut = CreateSut<GuardedRequest>(ConsentEnforcementMode.Block);
        var nextCalled = false;

        var result = await sut.Handle(
            new GuardedRequest("subject-1"), RequestContext.CreateForTest(), Next("done", () => nextCalled = true), CancellationToken.None);

        CodeOf(result).ShouldBe(ConsentErrors.MissingConsentCode);
        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ConsentMissingInWarnMode_ProceedsToTheHandler()
    {
        ValidatorReturns(Right<EncinaError, ConsentValidationResult>(
            ConsentValidationResult.Invalid(["No consent record found."], [Purpose])));
        var sut = CreateSut<GuardedRequest>(ConsentEnforcementMode.Warn);
        var nextCalled = false;

        var result = await sut.Handle(
            new GuardedRequest("subject-1"), RequestContext.CreateForTest(), Next("done", () => nextCalled = true), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ValidatorFails_PropagatesTheErrorInsteadOfProceeding()
    {
        ValidatorReturns(Left<EncinaError, ConsentValidationResult>(EncinaErrors.Create("validator.down", "unavailable")));
        var sut = CreateSut<GuardedRequest>(ConsentEnforcementMode.Warn);
        var nextCalled = false;

        var result = await sut.Handle(
            new GuardedRequest("subject-1"), RequestContext.CreateForTest(), Next("done", () => nextCalled = true), CancellationToken.None);

        CodeOf(result).ShouldBe("validator.down");
        nextCalled.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_SubjectIdBlank_FailsClosedWithoutValidating(string subjectId)
    {
        var sut = CreateSut<GuardedRequest>(ConsentEnforcementMode.Warn);

        var result = await sut.Handle(new GuardedRequest(subjectId), RequestContext.CreateForTest(), Next("done"), CancellationToken.None);

        CodeOf(result).ShouldBe(ConsentErrors.MissingConsentCode);
        await _validator.DidNotReceiveWithAnyArgs().ValidateAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_EmptyGuidSubject_FailsClosedWithoutValidating()
    {
        var sut = CreateSut<GuardedGuidRequest>(ConsentEnforcementMode.Warn);

        var result = await sut.Handle(new GuardedGuidRequest(Guid.Empty), RequestContext.CreateForTest(), Next("done"), CancellationToken.None);

        CodeOf(result).ShouldBe(ConsentErrors.MissingConsentCode);
        await _validator.DidNotReceiveWithAnyArgs().ValidateAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_GuidSubject_IsValidatedInInvariantDFormat()
    {
        ValidatorReturns(Right<EncinaError, ConsentValidationResult>(ConsentValidationResult.Valid()));
        var sut = CreateSut<GuardedGuidRequest>(ConsentEnforcementMode.Block);
        var subject = Guid.NewGuid();

        await sut.Handle(new GuardedGuidRequest(subject), RequestContext.CreateForTest(), Next("done"), CancellationToken.None);

        await _validator.Received(1).ValidateAsync(
            subject.ToString("D"), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SubjectIdPropertyDoesNotExist_ThrowsAConfigurationError()
    {
        var sut = CreateSut<MisconfiguredRequest>(ConsentEnforcementMode.Block);

        var act = async () => await sut.Handle(
            new MisconfiguredRequest("subject-1"), RequestContext.CreateForTest(), Next("done"), CancellationToken.None);

        await Should.ThrowAsync<InvalidOperationException>(act);
    }
}
