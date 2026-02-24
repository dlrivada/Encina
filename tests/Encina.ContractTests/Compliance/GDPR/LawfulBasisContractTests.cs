#pragma warning disable CA1859 // Contract tests intentionally use interface types to verify contracts

using Encina.Compliance.GDPR;
using LanguageExt;

namespace Encina.ContractTests.Compliance.GDPR;

/// <summary>
/// Contract tests for Encina.Compliance.GDPR lawful basis interfaces.
/// Verifies that implementations conform to interface contracts.
/// </summary>
public class LawfulBasisContractTests
{
    private static readonly DateTimeOffset FixedTime =
        new(2026, 2, 24, 10, 0, 0, TimeSpan.Zero);

    // ================================================================
    // ILawfulBasisRegistry contract (InMemory)
    // ================================================================

    /// <summary>
    /// Verifies that RegisterAsync returns Right on success.
    /// </summary>
    [Fact]
    public async Task ILawfulBasisRegistry_InMemory_RegisterAsync_ReturnsRight()
    {
        ILawfulBasisRegistry registry = new InMemoryLawfulBasisRegistry();
        var registration = CreateRegistration(typeof(string));

        var result = await registry.RegisterAsync(registration);

        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that duplicate registration returns Left.
    /// </summary>
    [Fact]
    public async Task ILawfulBasisRegistry_InMemory_DuplicateRegister_ReturnsLeft()
    {
        ILawfulBasisRegistry registry = new InMemoryLawfulBasisRegistry();
        var registration = CreateRegistration(typeof(string));

        await registry.RegisterAsync(registration);
        var result = await registry.RegisterAsync(registration);

        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that GetAllAsync returns Right with empty collection initially.
    /// </summary>
    [Fact]
    public async Task ILawfulBasisRegistry_InMemory_GetAllAsync_EmptyReturnsRight()
    {
        ILawfulBasisRegistry registry = new InMemoryLawfulBasisRegistry();

        var result = await registry.GetAllAsync();

        result.IsRight.ShouldBeTrue();
        var registrations = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<LawfulBasisRegistration>)[]);
        registrations.ShouldNotBeNull();
        registrations.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that GetByRequestTypeAsync returns Right.
    /// </summary>
    [Fact]
    public async Task ILawfulBasisRegistry_InMemory_GetByRequestType_ReturnsRight()
    {
        ILawfulBasisRegistry registry = new InMemoryLawfulBasisRegistry();

        var result = await registry.GetByRequestTypeAsync(typeof(string));

        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that GetByRequestTypeNameAsync returns Right.
    /// </summary>
    [Fact]
    public async Task ILawfulBasisRegistry_InMemory_GetByRequestTypeName_ReturnsRight()
    {
        ILawfulBasisRegistry registry = new InMemoryLawfulBasisRegistry();

        var result = await registry.GetByRequestTypeNameAsync("NonExistent");

        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that register-then-get round-trip works correctly.
    /// </summary>
    [Fact]
    public async Task ILawfulBasisRegistry_InMemory_RegisterThenGet_RoundTrips()
    {
        ILawfulBasisRegistry registry = new InMemoryLawfulBasisRegistry();
        var registration = CreateRegistration(typeof(string));

        await registry.RegisterAsync(registration);
        var result = await registry.GetByRequestTypeAsync(typeof(string));

        result.IsRight.ShouldBeTrue();
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsSome.ShouldBeTrue();
        option.IfSome(found =>
        {
            found.Basis.ShouldBe(LawfulBasis.Contract);
            found.Purpose.ShouldBe("Contract testing");
        });
    }

    /// <summary>
    /// Verifies that GetByRequestTypeAsync returns None for unregistered types.
    /// </summary>
    [Fact]
    public async Task ILawfulBasisRegistry_InMemory_GetUnregistered_ReturnsNone()
    {
        ILawfulBasisRegistry registry = new InMemoryLawfulBasisRegistry();

        var result = await registry.GetByRequestTypeAsync(typeof(int));

        result.IsRight.ShouldBeTrue();
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsNone.ShouldBeTrue();
    }

    // ================================================================
    // ILIAStore contract (InMemory)
    // ================================================================

    /// <summary>
    /// Verifies that StoreAsync returns Right on success.
    /// </summary>
    [Fact]
    public async Task ILIAStore_InMemory_StoreAsync_ReturnsRight()
    {
        ILIAStore store = new InMemoryLIAStore();
        var record = CreateLIARecord("LIA-001");

        var result = await store.StoreAsync(record);

        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that store-then-get round-trip works correctly.
    /// </summary>
    [Fact]
    public async Task ILIAStore_InMemory_StoreThenGet_RoundTrips()
    {
        ILIAStore store = new InMemoryLIAStore();
        var record = CreateLIARecord("LIA-002");

        await store.StoreAsync(record);
        var result = await store.GetByReferenceAsync("LIA-002");

        result.IsRight.ShouldBeTrue();
        var option = (Option<LIARecord>)result;
        option.IsSome.ShouldBeTrue();
        option.IfSome(found =>
        {
            found.Id.ShouldBe("LIA-002");
            found.Outcome.ShouldBe(LIAOutcome.Approved);
        });
    }

    /// <summary>
    /// Verifies that GetByReferenceAsync returns None for unknown references.
    /// </summary>
    [Fact]
    public async Task ILIAStore_InMemory_GetUnknown_ReturnsNone()
    {
        ILIAStore store = new InMemoryLIAStore();

        var result = await store.GetByReferenceAsync("NONEXISTENT");

        result.IsRight.ShouldBeTrue();
        var option = (Option<LIARecord>)result;
        option.IsNone.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that GetPendingReviewAsync returns Right.
    /// </summary>
    [Fact]
    public async Task ILIAStore_InMemory_GetPendingReview_ReturnsRight()
    {
        ILIAStore store = new InMemoryLIAStore();

        var result = await store.GetPendingReviewAsync();

        result.IsRight.ShouldBeTrue();
        var records = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<LIARecord>)[]);
        records.ShouldNotBeNull();
        records.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that upsert replaces existing record.
    /// </summary>
    [Fact]
    public async Task ILIAStore_InMemory_Upsert_ReplacesExisting()
    {
        ILIAStore store = new InMemoryLIAStore();
        var first = CreateLIARecord("LIA-UPS", LIAOutcome.RequiresReview);
        var second = CreateLIARecord("LIA-UPS", LIAOutcome.Approved);

        await store.StoreAsync(first);
        await store.StoreAsync(second);

        var result = await store.GetByReferenceAsync("LIA-UPS");
        result.IsRight.ShouldBeTrue();
        var option = (Option<LIARecord>)result;
        option.IsSome.ShouldBeTrue();
        option.IfSome(found => found.Outcome.ShouldBe(LIAOutcome.Approved));
    }

    // ================================================================
    // ILawfulBasisProvider contract (Default)
    // ================================================================

    /// <summary>
    /// Verifies that GetBasisForRequestAsync returns Right with None for unregistered type.
    /// </summary>
    [Fact]
    public async Task ILawfulBasisProvider_Default_GetBasis_UnregisteredReturnsNone()
    {
        ILawfulBasisRegistry registry = new InMemoryLawfulBasisRegistry();
        ILawfulBasisProvider provider = new DefaultLawfulBasisProvider(registry);

        var result = await provider.GetBasisForRequestAsync(typeof(string));

        result.IsRight.ShouldBeTrue();
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsNone.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that GetBasisForRequestAsync returns registered basis.
    /// </summary>
    [Fact]
    public async Task ILawfulBasisProvider_Default_GetBasis_RegisteredReturnsSome()
    {
        ILawfulBasisRegistry registry = new InMemoryLawfulBasisRegistry();
        await registry.RegisterAsync(CreateRegistration(typeof(string)));
        ILawfulBasisProvider provider = new DefaultLawfulBasisProvider(registry);

        var result = await provider.GetBasisForRequestAsync(typeof(string));

        result.IsRight.ShouldBeTrue();
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsSome.ShouldBeTrue();
    }

    // ================================================================
    // ILegitimateInterestAssessment contract (Default)
    // ================================================================

    /// <summary>
    /// Verifies that ValidateAsync returns Left(LIA not found) for unknown references.
    /// </summary>
    [Fact]
    public async Task ILegitimateInterestAssessment_Default_UnknownRef_ReturnsLeft()
    {
        ILIAStore store = new InMemoryLIAStore();
        ILegitimateInterestAssessment lia = new DefaultLegitimateInterestAssessment(store);

        var result = await lia.ValidateAsync("UNKNOWN-REF");

        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ValidateAsync returns Right(Approved) for approved LIA.
    /// </summary>
    [Fact]
    public async Task ILegitimateInterestAssessment_Default_Approved_ReturnsRight()
    {
        ILIAStore store = new InMemoryLIAStore();
        await store.StoreAsync(CreateLIARecord("LIA-APPROVE", LIAOutcome.Approved));
        ILegitimateInterestAssessment lia = new DefaultLegitimateInterestAssessment(store);

        var result = await lia.ValidateAsync("LIA-APPROVE");

        result.IsRight.ShouldBeTrue();
        var validation = (LIAValidationResult)result;
        validation.IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ValidateAsync returns Left for rejected LIA.
    /// </summary>
    [Fact]
    public async Task ILegitimateInterestAssessment_Default_Rejected_ReturnsLeft()
    {
        ILIAStore store = new InMemoryLIAStore();
        await store.StoreAsync(CreateLIARecord("LIA-REJECT", LIAOutcome.Rejected));
        ILegitimateInterestAssessment lia = new DefaultLegitimateInterestAssessment(store);

        var result = await lia.ValidateAsync("LIA-REJECT");

        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ValidateAsync returns Left for pending-review LIA.
    /// </summary>
    [Fact]
    public async Task ILegitimateInterestAssessment_Default_PendingReview_ReturnsLeft()
    {
        ILIAStore store = new InMemoryLIAStore();
        await store.StoreAsync(CreateLIARecord("LIA-REVIEW", LIAOutcome.RequiresReview));
        ILegitimateInterestAssessment lia = new DefaultLegitimateInterestAssessment(store);

        var result = await lia.ValidateAsync("LIA-REVIEW");

        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // LawfulBasisValidationResult factory contracts
    // ================================================================

    /// <summary>
    /// Verifies that Valid factory produces valid result.
    /// </summary>
    [Fact]
    public void LawfulBasisValidationResult_Valid_IsAlwaysValid()
    {
        var result = LawfulBasisValidationResult.Valid(LawfulBasis.Contract);
        result.IsValid.ShouldBeTrue();
        result.Basis.ShouldBe(LawfulBasis.Contract);
        result.Errors.Count.ShouldBe(0);
        result.Warnings.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that Invalid factory produces invalid result.
    /// </summary>
    [Fact]
    public void LawfulBasisValidationResult_Invalid_IsNeverValid()
    {
        var result = LawfulBasisValidationResult.Invalid("No basis declared");
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies that ValidWithWarnings produces valid result with warnings.
    /// </summary>
    [Fact]
    public void LawfulBasisValidationResult_ValidWithWarnings_HasWarnings()
    {
        var result = LawfulBasisValidationResult.ValidWithWarnings(
            LawfulBasis.LegitimateInterests, "Missing LIA reference");
        result.IsValid.ShouldBeTrue();
        result.Warnings.Count.ShouldBe(1);
        result.Errors.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that Invalid with basis carries the basis and errors/warnings.
    /// </summary>
    [Fact]
    public void LawfulBasisValidationResult_InvalidWithBasis_CarriesAll()
    {
        var result = LawfulBasisValidationResult.Invalid(
            LawfulBasis.Consent, ["No consent"], ["Approaching expiry"]);
        result.IsValid.ShouldBeFalse();
        result.Basis.ShouldBe(LawfulBasis.Consent);
        result.Errors.Count.ShouldBe(1);
        result.Warnings.Count.ShouldBe(1);
    }

    // ================================================================
    // LIAValidationResult factory contracts
    // ================================================================

    /// <summary>
    /// Verifies the Approved factory.
    /// </summary>
    [Fact]
    public void LIAValidationResult_Approved_Contract()
    {
        var result = LIAValidationResult.Approved();
        result.IsValid.ShouldBeTrue();
        result.Outcome.ShouldBe(LIAOutcome.Approved);
        result.RejectionReason.ShouldBeNull();
        result.RequiresReview.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies the Rejected factory.
    /// </summary>
    [Fact]
    public void LIAValidationResult_Rejected_Contract()
    {
        var result = LIAValidationResult.Rejected("Impact too high");
        result.IsValid.ShouldBeFalse();
        result.Outcome.ShouldBe(LIAOutcome.Rejected);
        result.RejectionReason.ShouldBe("Impact too high");
        result.RequiresReview.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies the PendingReview factory.
    /// </summary>
    [Fact]
    public void LIAValidationResult_PendingReview_Contract()
    {
        var result = LIAValidationResult.PendingReview();
        result.IsValid.ShouldBeFalse();
        result.Outcome.ShouldBe(LIAOutcome.RequiresReview);
        result.RequiresReview.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies the NotFound factory.
    /// </summary>
    [Fact]
    public void LIAValidationResult_NotFound_Contract()
    {
        var result = LIAValidationResult.NotFound();
        result.IsValid.ShouldBeFalse();
        result.Outcome.ShouldBeNull();
        result.RequiresReview.ShouldBeFalse();
    }

    // ================================================================
    // Helpers
    // ================================================================

    private static LawfulBasisRegistration CreateRegistration(Type requestType) => new()
    {
        RequestType = requestType,
        Basis = LawfulBasis.Contract,
        Purpose = "Contract testing",
        RegisteredAtUtc = FixedTime
    };

    private static LIARecord CreateLIARecord(
        string id,
        LIAOutcome outcome = LIAOutcome.Approved) => new()
        {
            Id = id,
            Name = "Test LIA",
            Purpose = "Contract testing",
            LegitimateInterest = "Test interest",
            Benefits = "Test benefits",
            ConsequencesIfNotProcessed = "None",
            NecessityJustification = "Required for testing",
            AlternativesConsidered = ["Manual"],
            DataMinimisationNotes = "Test data only",
            NatureOfData = "Test identifiers",
            ReasonableExpectations = "Expected by developers",
            ImpactAssessment = "Minimal impact",
            Safeguards = ["Encryption"],
            Outcome = outcome,
            Conclusion = "Test conclusion",
            AssessedAtUtc = FixedTime,
            AssessedBy = "Test DPO"
        };
}
