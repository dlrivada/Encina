using Encina.Compliance.GDPR;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.GDPR;

/// <summary>
/// Property-based tests for Encina.Compliance.GDPR lawful basis invariants.
/// </summary>
public class LawfulBasisPropertyTests
{
    // ================================================================
    // InMemoryLawfulBasisRegistry invariants
    // ================================================================

    /// <summary>
    /// Verifies that registering then retrieving a lawful basis always returns the registered data.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool LawfulBasisRegistry_RegisterThenGet_AlwaysReturnsRegistered(PositiveInt seed)
    {
        var registry = new InMemoryLawfulBasisRegistry();
        var basisValues = Enum.GetValues<LawfulBasis>();
        var basis = basisValues[seed.Get % basisValues.Length];
        var requestType = typeof(LawfulBasisPropertyTests);

        var registration = new LawfulBasisRegistration
        {
            RequestType = requestType,
            Basis = basis,
            Purpose = $"Testing-{seed.Get}",
            RegisteredAtUtc = DateTimeOffset.UtcNow
        };

        var registerResult = registry.RegisterAsync(registration).AsTask().Result;
        if (!registerResult.IsRight) return false;

        var getResult = registry.GetByRequestTypeAsync(requestType).AsTask().Result;
        if (!getResult.IsRight) return false;

        var option = (LanguageExt.Option<LawfulBasisRegistration>)getResult;
        return option.Match(
            Some: found => found.Basis == basis && found.Purpose == $"Testing-{seed.Get}",
            None: () => false);
    }

    /// <summary>
    /// Verifies that GetAllAsync count matches the number of successful registrations.
    /// </summary>
    [Property(MaxTest = 20)]
    public Property LawfulBasisRegistry_GetAll_CountMatchesRegistrations()
    {
        return Prop.ForAll(
            Gen.Choose(1, 8).ToArbitrary(),
            count =>
            {
                var registry = new InMemoryLawfulBasisRegistry();
                var types = typeof(LawfulBasisPropertyTests).Assembly.GetTypes()
                    .Take(count)
                    .Distinct()
                    .ToList();
                var basisValues = Enum.GetValues<LawfulBasis>();
                var registered = 0;

                foreach (var type in types)
                {
                    var registration = new LawfulBasisRegistration
                    {
                        RequestType = type,
                        Basis = basisValues[registered % basisValues.Length],
                        RegisteredAtUtc = DateTimeOffset.UtcNow
                    };
                    var result = registry.RegisterAsync(registration).AsTask().Result;
                    if (result.IsRight) registered++;
                }

                var allResult = registry.GetAllAsync().AsTask().Result;
                allResult.IsRight.ShouldBeTrue();
                var all = allResult.Match(
                    Right: r => r,
                    Left: _ => (IReadOnlyList<LawfulBasisRegistration>)[]);
                all.Count.ShouldBe(registered);
            });
    }

    /// <summary>
    /// Verifies that retrieving a non-registered type returns None.
    /// </summary>
    [Fact]
    public void LawfulBasisRegistry_GetUnregistered_ReturnsNone()
    {
        var registry = new InMemoryLawfulBasisRegistry();
        var result = registry.GetByRequestTypeAsync(typeof(string)).AsTask().Result;
        result.IsRight.ShouldBeTrue();
        var option = (LanguageExt.Option<LawfulBasisRegistration>)result;
        option.IsNone.ShouldBeTrue();
    }

    // ================================================================
    // InMemoryLIAStore invariants
    // ================================================================

    /// <summary>
    /// Verifies that storing and retrieving a LIA record always returns the stored data.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool LIAStore_StoreThenGet_AlwaysReturnsStored(NonEmptyString id, NonEmptyString name)
    {
        var store = new InMemoryLIAStore();
        var record = CreateLIARecord(id.Get, name.Get);

        var storeResult = store.StoreAsync(record).AsTask().Result;
        if (!storeResult.IsRight) return false;

        var getResult = store.GetByReferenceAsync(id.Get).AsTask().Result;
        if (!getResult.IsRight) return false;

        var option = (LanguageExt.Option<LIARecord>)getResult;
        return option.Match(
            Some: found => found.Id == id.Get && found.Name == name.Get,
            None: () => false);
    }

    /// <summary>
    /// Verifies that LIA upsert replaces previous record.
    /// </summary>
    [Property(MaxTest = 20)]
    public bool LIAStore_Upsert_AlwaysReplacesWithLatest(NonEmptyString id)
    {
        var store = new InMemoryLIAStore();
        var first = CreateLIARecord(id.Get, "First", LIAOutcome.RequiresReview);
        var second = CreateLIARecord(id.Get, "Second", LIAOutcome.Approved);

        store.StoreAsync(first).AsTask().Wait();
        store.StoreAsync(second).AsTask().Wait();

        var result = store.GetByReferenceAsync(id.Get).AsTask().Result;
        if (!result.IsRight) return false;

        var option = (LanguageExt.Option<LIARecord>)result;
        return option.Match(
            Some: found => found.Name == "Second" && found.Outcome == LIAOutcome.Approved,
            None: () => false);
    }

    /// <summary>
    /// Verifies that GetPendingReviewAsync never returns approved records.
    /// </summary>
    [Property(MaxTest = 20)]
    public Property LIAStore_GetPendingReview_NeverReturnsApproved()
    {
        return Prop.ForAll(
            Gen.Choose(1, 5).ToArbitrary(),
            count =>
            {
                var store = new InMemoryLIAStore();
                var outcomes = Enum.GetValues<LIAOutcome>();

                for (var i = 0; i < count; i++)
                {
                    var outcome = outcomes[i % outcomes.Length];
                    var record = CreateLIARecord($"LIA-{i}", $"Record {i}", outcome);
                    store.StoreAsync(record).AsTask().Wait();
                }

                var result = store.GetPendingReviewAsync().AsTask().Result;
                result.IsRight.ShouldBeTrue();
                var pending = result.Match(
                    Right: r => r,
                    Left: _ => (IReadOnlyList<LIARecord>)[]);
                pending.ShouldAllBe(r => r.Outcome == LIAOutcome.RequiresReview);
            });
    }

    // ================================================================
    // LawfulBasisValidationResult invariants
    // ================================================================

    /// <summary>
    /// Verifies that a valid result always has IsValid true and no errors.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ValidationResult_Valid_AlwaysHasNoErrors(PositiveInt seed)
    {
        var basisValues = Enum.GetValues<LawfulBasis>();
        var basis = basisValues[seed.Get % basisValues.Length];
        var result = LawfulBasisValidationResult.Valid(basis);
        return result.IsValid && result.Errors.Count == 0 && result.Basis == basis;
    }

    /// <summary>
    /// Verifies that an invalid result always has IsValid false and at least one error.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ValidationResult_Invalid_AlwaysHasErrors(NonEmptyString error)
    {
        var result = LawfulBasisValidationResult.Invalid(error.Get);
        return !result.IsValid && result.Errors.Count > 0;
    }

    /// <summary>
    /// Verifies that valid-with-warnings has both valid state and warnings.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ValidationResult_ValidWithWarnings_HasBothStates(NonEmptyString warning)
    {
        var result = LawfulBasisValidationResult.ValidWithWarnings(LawfulBasis.Contract, warning.Get);
        return result.IsValid && result.Warnings.Count > 0 && result.Errors.Count == 0;
    }

    // ================================================================
    // LIAValidationResult invariants
    // ================================================================

    /// <summary>
    /// Verifies that Approved always produces a valid result with Approved outcome.
    /// </summary>
    [Fact]
    public void LIAValidation_Approved_AlwaysValid()
    {
        var result = LIAValidationResult.Approved();
        result.IsValid.ShouldBeTrue();
        result.Outcome.ShouldBe(LIAOutcome.Approved);
        result.RequiresReview.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that Rejected always produces an invalid result.
    /// </summary>
    [Property(MaxTest = 20)]
    public bool LIAValidation_Rejected_AlwaysInvalid(NonEmptyString reason)
    {
        var result = LIAValidationResult.Rejected(reason.Get);
        return !result.IsValid && result.Outcome == LIAOutcome.Rejected && !result.RequiresReview;
    }

    /// <summary>
    /// Verifies that PendingReview always produces an invalid result requiring review.
    /// </summary>
    [Fact]
    public void LIAValidation_PendingReview_RequiresReview()
    {
        var result = LIAValidationResult.PendingReview();
        result.IsValid.ShouldBeFalse();
        result.Outcome.ShouldBe(LIAOutcome.RequiresReview);
        result.RequiresReview.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that NotFound always produces an invalid result with no outcome.
    /// </summary>
    [Fact]
    public void LIAValidation_NotFound_HasNoOutcome()
    {
        var result = LIAValidationResult.NotFound();
        result.IsValid.ShouldBeFalse();
        result.Outcome.ShouldBeNull();
        result.RequiresReview.ShouldBeFalse();
    }

    // ================================================================
    // LawfulBasisRegistration.FromAttribute invariants
    // ================================================================

    /// <summary>
    /// Verifies that FromAttribute returns null for types without the attribute.
    /// </summary>
    [Fact]
    public void Registration_FromAttribute_TypeWithoutAttribute_ReturnsNull()
    {
        var result = LawfulBasisRegistration.FromAttribute(typeof(string));
        result.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that FromAttribute returns correct basis for decorated types.
    /// </summary>
    [Fact]
    public void Registration_FromAttribute_DecoratedType_ReturnsBasis()
    {
        var result = LawfulBasisRegistration.FromAttribute(typeof(TestConsentCommand));
        result.ShouldNotBeNull();
        result!.Basis.ShouldBe(LawfulBasis.Consent);
        result.Purpose.ShouldBe("Test consent purpose");
    }

    // ================================================================
    // LIAOutcome enum invariants
    // ================================================================

    /// <summary>
    /// Verifies that LIAOutcome has exactly three values (Approved, Rejected, RequiresReview).
    /// </summary>
    [Fact]
    public void LIAOutcome_ShouldHaveExactlyThreeValues()
    {
        Enum.GetValues<LIAOutcome>().Length.ShouldBe(3,
            "LIA assessments have exactly three possible outcomes");
    }

    // ================================================================
    // Helpers
    // ================================================================

    [LawfulBasis(LawfulBasis.Consent, Purpose = "Test consent purpose")]
    private sealed record TestConsentCommand;

    private static LIARecord CreateLIARecord(
        string id = "LIA-001",
        string name = "Test LIA",
        LIAOutcome outcome = LIAOutcome.Approved) => new()
        {
            Id = id,
            Name = name,
            Purpose = "Testing",
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
            AssessedAtUtc = DateTimeOffset.UtcNow,
            AssessedBy = "Test DPO"
        };
}
