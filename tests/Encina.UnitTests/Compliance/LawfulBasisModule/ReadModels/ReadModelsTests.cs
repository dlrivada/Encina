using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis.ReadModels;

using GDPRLawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis;

namespace Encina.UnitTests.Compliance.LawfulBasisModule.ReadModels;

/// <summary>
/// Unit tests for read model POCO classes.
/// </summary>
public class ReadModelsTests
{
    [Fact]
    public void LawfulBasisReadModel_Default_HasExpectedDefaults()
    {
        var model = new LawfulBasisReadModel();

        model.Id.ShouldBe(Guid.Empty);
        model.RequestTypeName.ShouldBe(string.Empty);
        model.IsRevoked.ShouldBeFalse();
        model.Version.ShouldBe(0);
        model.Purpose.ShouldBeNull();
        model.LIAReference.ShouldBeNull();
        model.LegalReference.ShouldBeNull();
        model.ContractReference.ShouldBeNull();
        model.TenantId.ShouldBeNull();
        model.ModuleId.ShouldBeNull();
    }

    [Fact]
    public void LawfulBasisReadModel_SettersWork()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var model = new LawfulBasisReadModel
        {
            Id = id,
            RequestTypeName = "MyApp.Command",
            Basis = GDPRLawfulBasis.LegitimateInterests,
            Purpose = "Fraud",
            LIAReference = "LIA-001",
            LegalReference = "LegRef",
            ContractReference = "CtrRef",
            IsRevoked = true,
            RevocationReason = "Reason",
            RegisteredAtUtc = now,
            TenantId = "tenant-1",
            ModuleId = "module-1",
            LastModifiedAtUtc = now,
            Version = 5
        };

        model.Id.ShouldBe(id);
        model.Basis.ShouldBe(GDPRLawfulBasis.LegitimateInterests);
        model.IsRevoked.ShouldBeTrue();
        model.Version.ShouldBe(5);
        model.RevocationReason.ShouldBe("Reason");
    }

    [Fact]
    public void LIAReadModel_Default_HasExpectedDefaults()
    {
        var model = new LIAReadModel();

        model.Id.ShouldBe(Guid.Empty);
        model.Reference.ShouldBe(string.Empty);
        model.Name.ShouldBe(string.Empty);
        model.Purpose.ShouldBe(string.Empty);
        model.LegitimateInterest.ShouldBe(string.Empty);
        model.Benefits.ShouldBe(string.Empty);
        model.ConsequencesIfNotProcessed.ShouldBe(string.Empty);
        model.NecessityJustification.ShouldBe(string.Empty);
        model.AlternativesConsidered.ShouldBeEmpty();
        model.DataMinimisationNotes.ShouldBe(string.Empty);
        model.NatureOfData.ShouldBe(string.Empty);
        model.ReasonableExpectations.ShouldBe(string.Empty);
        model.ImpactAssessment.ShouldBe(string.Empty);
        model.Safeguards.ShouldBeEmpty();
        model.AssessedBy.ShouldBe(string.Empty);
        model.DPOInvolvement.ShouldBeFalse();
        model.Version.ShouldBe(0);
    }

    private static readonly IReadOnlyList<string> TestAlternatives = ["A", "B"];
    private static readonly IReadOnlyList<string> TestSafeguards = ["S"];

    [Fact]
    public void LIAReadModel_SettersWork()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var alts = TestAlternatives;
        var sg = TestSafeguards;

        var model = new LIAReadModel
        {
            Id = id,
            Reference = "LIA-001",
            Name = "Fraud LIA",
            Purpose = "Fraud",
            LegitimateInterest = "Interest",
            Benefits = "Benefits",
            ConsequencesIfNotProcessed = "Consequences",
            NecessityJustification = "Necessary",
            AlternativesConsidered = alts,
            DataMinimisationNotes = "Min",
            NatureOfData = "Data",
            ReasonableExpectations = "Expect",
            ImpactAssessment = "Impact",
            Safeguards = sg,
            AssessedBy = "DPO",
            DPOInvolvement = true,
            AssessedAtUtc = now,
            Conditions = "Cond",
            Outcome = LIAOutcome.Approved,
            Conclusion = "Approved",
            NextReviewAtUtc = now.AddYears(1),
            TenantId = "tenant-1",
            ModuleId = "module-1",
            LastModifiedAtUtc = now,
            Version = 3
        };

        model.Id.ShouldBe(id);
        model.Reference.ShouldBe("LIA-001");
        model.Outcome.ShouldBe(LIAOutcome.Approved);
        model.Version.ShouldBe(3);
        model.DPOInvolvement.ShouldBeTrue();
        model.AlternativesConsidered.Count.ShouldBe(2);
        model.Safeguards.Count.ShouldBe(1);
    }
}
