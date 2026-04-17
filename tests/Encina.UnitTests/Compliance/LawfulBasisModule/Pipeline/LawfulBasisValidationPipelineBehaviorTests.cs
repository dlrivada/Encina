using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis;
using Encina.Compliance.LawfulBasis.Abstractions;
using Encina.Compliance.LawfulBasis.ReadModels;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static LanguageExt.Prelude;
using GDPRLawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis;

#pragma warning disable CA1034 // Nested types for test requests

#pragma warning disable CA2012 // NSubstitute Returns with ValueTask

namespace Encina.UnitTests.Compliance.LawfulBasisModule.Pipeline;

/// <summary>
/// Unit tests for <see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class LawfulBasisValidationPipelineBehaviorTests
{
    private readonly ILawfulBasisService _service = Substitute.For<ILawfulBasisService>();
    private readonly ILawfulBasisSubjectIdExtractor _subjectIdExtractor = Substitute.For<ILawfulBasisSubjectIdExtractor>();
    private readonly IConsentStatusProvider _consentProvider = Substitute.For<IConsentStatusProvider>();
    private readonly IRequestContext _context = Substitute.For<IRequestContext>();

    // ================================================================
    // Test request types with different attribute combinations
    // ================================================================

    public sealed record UndecoratedRequest : IRequest<string>;

    [ProcessesPersonalData]
    public sealed record PersonalDataOnlyRequest : IRequest<string>;

    [LawfulBasis(GDPRLawfulBasis.Contract, Purpose = "Order")]
    public sealed record ContractRequest : IRequest<string>;

    [LawfulBasis(GDPRLawfulBasis.LegitimateInterests, Purpose = "Fraud", LIAReference = "LIA-001")]
    public sealed record LIRequest : IRequest<string>;

    [LawfulBasis(GDPRLawfulBasis.LegitimateInterests, Purpose = "Fraud")]
    public sealed record LIRequestNoReference : IRequest<string>;

    [LawfulBasis(GDPRLawfulBasis.Consent, Purpose = "Marketing")]
    public sealed record ConsentRequest : IRequest<string>;

    [ProcessingActivity(
        Purpose = "Analytics",
        LawfulBasis = GDPRLawfulBasis.PublicTask,
        DataCategories = new[] { "Cat" },
        DataSubjects = new[] { "Sub" },
        RetentionDays = 30)]
    public sealed record ProcessingActivityOnlyRequest : IRequest<string>;

    [LawfulBasis(GDPRLawfulBasis.Contract)]
    [ProcessingActivity(
        Purpose = "Order",
        LawfulBasis = GDPRLawfulBasis.LegitimateInterests,
        DataCategories = new[] { "Cat" },
        DataSubjects = new[] { "Sub" },
        RetentionDays = 30)]
    public sealed record ConflictingAttributesRequest : IRequest<string>;

    private LawfulBasisValidationPipelineBehavior<TRequest, string> CreateBehavior<TRequest>(
        LawfulBasisOptions? options = null,
        IConsentStatusProvider? consentProvider = null)
        where TRequest : IRequest<string>
    {
        return new LawfulBasisValidationPipelineBehavior<TRequest, string>(
            _service,
            _subjectIdExtractor,
            Options.Create(options ?? new LawfulBasisOptions()),
            NullLogger<LawfulBasisValidationPipelineBehavior<TRequest, string>>.Instance,
            consentProvider);
    }

    private static RequestHandlerCallback<string> SuccessCallback(string value = "ok") =>
        () => new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>(value));

    private static string GetRight(Either<EncinaError, string> either) =>
        either.Match(Right: v => v, Left: _ => string.Empty);

    // ================================================================
    // Constructor
    // ================================================================

    [Fact]
    public void Constructor_WithAllDependencies_ShouldSucceed()
    {
        var behavior = CreateBehavior<UndecoratedRequest>();
        behavior.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullConsentProvider_ShouldSucceed()
    {
        var behavior = CreateBehavior<UndecoratedRequest>(consentProvider: null);
        behavior.ShouldNotBeNull();
    }

    // ================================================================
    // Disabled mode
    // ================================================================

    [Fact]
    public async Task Handle_DisabledMode_CallsNextAndReturnsResult()
    {
        var options = new LawfulBasisOptions { EnforcementMode = LawfulBasisEnforcementMode.Disabled };
        var behavior = CreateBehavior<ContractRequest>(options);
        var called = false;

        RequestHandlerCallback<string> next = () =>
        {
            called = true;
            return new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("disabled-result"));
        };

        var result = await behavior.Handle(new ContractRequest(), _context, next, CancellationToken.None);

        called.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("disabled-result");
    }

    // ================================================================
    // No attributes at all — skip
    // ================================================================

    [Fact]
    public async Task Handle_NoAttributes_SkipsValidation()
    {
        var behavior = CreateBehavior<UndecoratedRequest>();
        var result = await behavior.Handle(new UndecoratedRequest(), _context, SuccessCallback("skipped"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("skipped");
        // Service should not be called
        await _service.DidNotReceiveWithAnyArgs().GetRegistrationByRequestTypeAsync(default!, default);
    }

    // ================================================================
    // Contract basis — simple pass-through
    // ================================================================

    [Fact]
    public async Task Handle_ContractBasis_ShouldCallNextAndReturnResult()
    {
        var behavior = CreateBehavior<ContractRequest>();
        var result = await behavior.Handle(new ContractRequest(), _context, SuccessCallback("contract-ok"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("contract-ok");
    }

    // ================================================================
    // LegitimateInterests with valid LIA
    // ================================================================

    [Fact]
    public async Task Handle_LIWithValidApprovedLIA_CallsNext()
    {
        _service.HasApprovedLIAAsync("LIA-001", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(true)));

        var behavior = CreateBehavior<LIRequest>();
        var result = await behavior.Handle(new LIRequest(), _context, SuccessCallback("li-ok"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("li-ok");
    }

    [Fact]
    public async Task Handle_LIWithUnapprovedLIA_BlockedInBlockMode()
    {
        _service.HasApprovedLIAAsync("LIA-001", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(false)));

        var behavior = CreateBehavior<LIRequest>(new LawfulBasisOptions
        {
            EnforcementMode = LawfulBasisEnforcementMode.Block
        });
        var result = await behavior.Handle(new LIRequest(), _context, SuccessCallback(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_LIWithUnapprovedLIA_WarnModeCallsNext()
    {
        _service.HasApprovedLIAAsync("LIA-001", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(false)));

        var behavior = CreateBehavior<LIRequest>(new LawfulBasisOptions
        {
            EnforcementMode = LawfulBasisEnforcementMode.Warn
        });
        var result = await behavior.Handle(new LIRequest(), _context, SuccessCallback("warn-ok"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("warn-ok");
    }

    [Fact]
    public async Task Handle_LIServiceReturnsError_BlockedInBlockMode()
    {
        var error = EncinaErrors.Create("svc.err", "service failed");
        _service.HasApprovedLIAAsync("LIA-001", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(Left<EncinaError, bool>(error)));

        var behavior = CreateBehavior<LIRequest>();
        var result = await behavior.Handle(new LIRequest(), _context, SuccessCallback(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_LIWithoutLIAReference_BlockedInBlockMode()
    {
        var behavior = CreateBehavior<LIRequestNoReference>();
        var result = await behavior.Handle(new LIRequestNoReference(), _context, SuccessCallback(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_LIValidationDisabled_SkipsLIACheck()
    {
        var options = new LawfulBasisOptions { ValidateLIAForLegitimateInterests = false };
        var behavior = CreateBehavior<LIRequestNoReference>(options);
        var result = await behavior.Handle(new LIRequestNoReference(), _context, SuccessCallback("skip-lia"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("skip-lia");
        await _service.DidNotReceiveWithAnyArgs().HasApprovedLIAAsync(default!, default);
    }

    // ================================================================
    // Consent basis
    // ================================================================

    [Fact]
    public async Task Handle_ConsentBasisWithNullConsentProvider_BlockedInBlockMode()
    {
        var behavior = CreateBehavior<ConsentRequest>(consentProvider: null);
        var result = await behavior.Handle(new ConsentRequest(), _context, SuccessCallback(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ConsentBasisNoSubjectId_BlockedInBlockMode()
    {
        _subjectIdExtractor.ExtractSubjectId(Arg.Any<ConsentRequest>(), _context).Returns((string?)null);

        var behavior = CreateBehavior<ConsentRequest>(consentProvider: _consentProvider);
        var result = await behavior.Handle(new ConsentRequest(), _context, SuccessCallback(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ConsentBasisValidConsent_CallsNext()
    {
        _subjectIdExtractor.ExtractSubjectId(Arg.Any<ConsentRequest>(), _context).Returns("user-1");
        _consentProvider.CheckConsentAsync("user-1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, ConsentCheckResult>>(
                Right<EncinaError, ConsentCheckResult>(new ConsentCheckResult(true, []))));

        var behavior = CreateBehavior<ConsentRequest>(consentProvider: _consentProvider);
        var result = await behavior.Handle(new ConsentRequest(), _context, SuccessCallback("consent-ok"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("consent-ok");
    }

    [Fact]
    public async Task Handle_ConsentBasisNoValidConsent_BlockedInBlockMode()
    {
        _subjectIdExtractor.ExtractSubjectId(Arg.Any<ConsentRequest>(), _context).Returns("user-1");
        _consentProvider.CheckConsentAsync("user-1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, ConsentCheckResult>>(
                Right<EncinaError, ConsentCheckResult>(new ConsentCheckResult(false, ["Marketing"]))));

        var behavior = CreateBehavior<ConsentRequest>(consentProvider: _consentProvider);
        var result = await behavior.Handle(new ConsentRequest(), _context, SuccessCallback(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ConsentBasisProviderError_BlockedInBlockMode()
    {
        _subjectIdExtractor.ExtractSubjectId(Arg.Any<ConsentRequest>(), _context).Returns("user-1");
        var error = EncinaErrors.Create("consent.err", "consent provider failed");
        _consentProvider.CheckConsentAsync("user-1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, ConsentCheckResult>>(
                Left<EncinaError, ConsentCheckResult>(error)));

        var behavior = CreateBehavior<ConsentRequest>(consentProvider: _consentProvider);
        var result = await behavior.Handle(new ConsentRequest(), _context, SuccessCallback(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ConsentValidationDisabled_SkipsConsentCheck()
    {
        var options = new LawfulBasisOptions { ValidateConsentForConsentBasis = false };
        var behavior = CreateBehavior<ConsentRequest>(options, consentProvider: null);
        var result = await behavior.Handle(new ConsentRequest(), _context, SuccessCallback("skip-consent"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("skip-consent");
    }

    // ================================================================
    // ProcessesPersonalData attribute with no basis
    // ================================================================

    [Fact]
    public async Task Handle_ProcessesPersonalDataNoBasis_BlockedWhenRequireDeclaredBasis()
    {
        _service.GetRegistrationByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>>(
                Right<EncinaError, Option<LawfulBasisReadModel>>(Option<LawfulBasisReadModel>.None)));

        var options = new LawfulBasisOptions
        {
            RequireDeclaredBasis = true,
            EnforcementMode = LawfulBasisEnforcementMode.Block
        };
        var behavior = CreateBehavior<PersonalDataOnlyRequest>(options);
        var result = await behavior.Handle(new PersonalDataOnlyRequest(), _context, SuccessCallback(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ProcessesPersonalDataNoBasis_WarnModeCallsNext()
    {
        _service.GetRegistrationByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>>(
                Right<EncinaError, Option<LawfulBasisReadModel>>(Option<LawfulBasisReadModel>.None)));

        var options = new LawfulBasisOptions
        {
            RequireDeclaredBasis = true,
            EnforcementMode = LawfulBasisEnforcementMode.Warn
        };
        var behavior = CreateBehavior<PersonalDataOnlyRequest>(options);
        var result = await behavior.Handle(new PersonalDataOnlyRequest(), _context, SuccessCallback("warn-pd"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("warn-pd");
    }

    [Fact]
    public async Task Handle_ProcessesPersonalDataNoBasis_RequireDeclaredBasisFalse_CallsNext()
    {
        var options = new LawfulBasisOptions
        {
            RequireDeclaredBasis = false
        };
        var behavior = CreateBehavior<PersonalDataOnlyRequest>(options);
        var result = await behavior.Handle(new PersonalDataOnlyRequest(), _context, SuccessCallback("no-req"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("no-req");
    }

    [Fact]
    public async Task Handle_ProcessesPersonalDataWithServiceBasis_CallsNext()
    {
        var readModel = new LawfulBasisReadModel
        {
            Id = Guid.NewGuid(),
            Basis = GDPRLawfulBasis.Contract,
            RequestTypeName = typeof(PersonalDataOnlyRequest).AssemblyQualifiedName!
        };
        _service.GetRegistrationByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>>(
                Right<EncinaError, Option<LawfulBasisReadModel>>(Optional(readModel))));

        var options = new LawfulBasisOptions { RequireDeclaredBasis = true };
        var behavior = CreateBehavior<PersonalDataOnlyRequest>(options);
        var result = await behavior.Handle(new PersonalDataOnlyRequest(), _context, SuccessCallback("svc-ok"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("svc-ok");
    }

    [Fact]
    public async Task Handle_ProcessesPersonalDataWithServiceError_FallsThroughToEnforcement()
    {
        _service.GetRegistrationByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>>(
                Left<EncinaError, Option<LawfulBasisReadModel>>(EncinaErrors.Create("svc.err", "failed"))));

        var options = new LawfulBasisOptions
        {
            RequireDeclaredBasis = true,
            EnforcementMode = LawfulBasisEnforcementMode.Block
        };
        var behavior = CreateBehavior<PersonalDataOnlyRequest>(options);
        var result = await behavior.Handle(new PersonalDataOnlyRequest(), _context, SuccessCallback(), CancellationToken.None);

        // Service error -> no basis -> enforcement blocks
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // ProcessingActivity attribute only
    // ================================================================

    [Fact]
    public async Task Handle_ProcessingActivityAttribute_ReadsBasis()
    {
        var behavior = CreateBehavior<ProcessingActivityOnlyRequest>();
        var result = await behavior.Handle(new ProcessingActivityOnlyRequest(), _context, SuccessCallback("pa-ok"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("pa-ok");
    }

    // ================================================================
    // Attribute conflict detection
    // ================================================================

    [Fact]
    public async Task Handle_ConflictingAttributes_LawfulBasisAttributeWins()
    {
        // The LawfulBasisAttribute says Contract, ProcessingActivityAttribute says LegitimateInterests
        // Contract should be used and validation should pass since Contract has no extra checks
        var behavior = CreateBehavior<ConflictingAttributesRequest>();
        var result = await behavior.Handle(new ConflictingAttributesRequest(), _context, SuccessCallback("conflict-ok"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("conflict-ok");
        // Should not have called HasApprovedLIAAsync (that's only for LegitimateInterests)
        await _service.DidNotReceiveWithAnyArgs().HasApprovedLIAAsync(default!, default);
    }
}
