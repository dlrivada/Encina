using Encina.Compliance.LawfulBasis.Events;

namespace Encina.UnitTests.Compliance.LawfulBasisModule.Events;

/// <summary>
/// Unit tests for LIA event records.
/// </summary>
public class LIAEventsTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly IReadOnlyList<string> Alts = ["A", "B"];
    private static readonly IReadOnlyList<string> Safeguards = ["Encryption", "Access control"];

    [Fact]
    public void LIACreated_StoresAllProperties()
    {
        var id = Guid.NewGuid();
        var evt = new LIACreated(
            id,
            "LIA-001",
            "Fraud LIA",
            "Fraud prevention",
            "Fraud interest",
            "Reduced fraud",
            "Higher losses",
            "No alternative",
            Alts,
            "Minimal data",
            "Financial",
            "Reasonable",
            "Low impact",
            Safeguards,
            "DPO",
            true,
            Now,
            "Annual review",
            "tenant-1",
            "module-1");

        evt.LIAId.ShouldBe(id);
        evt.Reference.ShouldBe("LIA-001");
        evt.Name.ShouldBe("Fraud LIA");
        evt.Purpose.ShouldBe("Fraud prevention");
        evt.LegitimateInterest.ShouldBe("Fraud interest");
        evt.Benefits.ShouldBe("Reduced fraud");
        evt.ConsequencesIfNotProcessed.ShouldBe("Higher losses");
        evt.NecessityJustification.ShouldBe("No alternative");
        evt.AlternativesConsidered.ShouldBe(Alts);
        evt.DataMinimisationNotes.ShouldBe("Minimal data");
        evt.NatureOfData.ShouldBe("Financial");
        evt.ReasonableExpectations.ShouldBe("Reasonable");
        evt.ImpactAssessment.ShouldBe("Low impact");
        evt.Safeguards.ShouldBe(Safeguards);
        evt.AssessedBy.ShouldBe("DPO");
        evt.DPOInvolvement.ShouldBeTrue();
        evt.AssessedAtUtc.ShouldBe(Now);
        evt.Conditions.ShouldBe("Annual review");
        evt.TenantId.ShouldBe("tenant-1");
        evt.ModuleId.ShouldBe("module-1");
    }

    [Fact]
    public void LIAApproved_StoresAllProperties()
    {
        var id = Guid.NewGuid();
        var evt = new LIAApproved(id, "Balancing passed", "Approver1", Now, "tenant-1", "module-1");

        evt.LIAId.ShouldBe(id);
        evt.Conclusion.ShouldBe("Balancing passed");
        evt.ApprovedBy.ShouldBe("Approver1");
        evt.ApprovedAtUtc.ShouldBe(Now);
        evt.TenantId.ShouldBe("tenant-1");
        evt.ModuleId.ShouldBe("module-1");
    }

    [Fact]
    public void LIARejected_StoresAllProperties()
    {
        var id = Guid.NewGuid();
        var evt = new LIARejected(id, "Balancing failed", "Rejecter1", Now, null, null);

        evt.LIAId.ShouldBe(id);
        evt.Conclusion.ShouldBe("Balancing failed");
        evt.RejectedBy.ShouldBe("Rejecter1");
        evt.RejectedAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void LIAReviewScheduled_StoresAllProperties()
    {
        var id = Guid.NewGuid();
        var nextReview = Now.AddYears(1);
        var evt = new LIAReviewScheduled(id, nextReview, "Scheduler1", Now, null, null);

        evt.LIAId.ShouldBe(id);
        evt.NextReviewAtUtc.ShouldBe(nextReview);
        evt.ScheduledBy.ShouldBe("Scheduler1");
        evt.ScheduledAtUtc.ShouldBe(Now);
    }
}
