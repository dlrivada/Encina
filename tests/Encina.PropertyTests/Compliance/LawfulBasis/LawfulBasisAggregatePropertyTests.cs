using Encina.Compliance.LawfulBasis.Aggregates;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

using LawfulBasisEnum = global::Encina.Compliance.GDPR.LawfulBasis;
using LIAOutcomeEnum = global::Encina.Compliance.GDPR.LIAOutcome;

namespace Encina.PropertyTests.Compliance.LawfulBasis;

/// <summary>
/// Property-based tests for <see cref="LawfulBasisAggregate"/> and <see cref="LIAAggregate"/>
/// verifying invariants across randomized inputs using FsCheck.
/// </summary>
public class LawfulBasisAggregatePropertyTests
{
    private static readonly Gen<LawfulBasisEnum> BasisGen = Gen.Elements(
        LawfulBasisEnum.Consent,
        LawfulBasisEnum.Contract,
        LawfulBasisEnum.LegalObligation,
        LawfulBasisEnum.VitalInterests,
        LawfulBasisEnum.PublicTask,
        LawfulBasisEnum.LegitimateInterests);

    #region LawfulBasisAggregate Invariants

    /// <summary>
    /// Invariant: Register always produces an aggregate with Version equal to 1.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Register_AlwaysProducesVersion1(NonEmptyString requestTypeName)
    {
        var name = requestTypeName.Get.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var aggregate = LawfulBasisAggregate.Register(
            Guid.NewGuid(), name, LawfulBasisEnum.Consent, "purpose",
            null, null, null, DateTimeOffset.UtcNow);

        return aggregate.Version == 1;
    }

    /// <summary>
    /// Invariant: Register preserves all input properties in the resulting aggregate.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Register_PreservesAllInputProperties(
        NonEmptyString requestTypeName,
        NonEmptyString purpose)
    {
        return Prop.ForAll(BasisGen.ToArbitrary(), basis =>
        {
            var name = requestTypeName.Get.Trim();
            var purposeVal = purpose.Get.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(purposeVal))
            {
                return true;
            }

            var id = Guid.NewGuid();
            var liaRef = "LIA-001";
            var legalRef = "GDPR-Art6";
            var contractRef = "CONTRACT-001";

            var aggregate = LawfulBasisAggregate.Register(
                id, name, basis, purposeVal,
                liaRef, legalRef, contractRef, DateTimeOffset.UtcNow,
                "tenant-1", "module-1");

            return aggregate.Id == id &&
                   aggregate.RequestTypeName == name &&
                   aggregate.Basis == basis &&
                   aggregate.Purpose == purposeVal &&
                   aggregate.LIAReference == liaRef &&
                   aggregate.LegalReference == legalRef &&
                   aggregate.ContractReference == contractRef &&
                   aggregate.TenantId == "tenant-1" &&
                   aggregate.ModuleId == "module-1";
        });
    }

    /// <summary>
    /// Invariant: ChangeBasis never produces a revoked state.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ChangeBasis_NeverProducesRevokedState()
    {
        var aggregate = CreateActiveAggregate(LawfulBasisEnum.Consent);
        aggregate.ChangeBasis(
            LawfulBasisEnum.Contract, "updated purpose",
            null, null, "contract-ref", DateTimeOffset.UtcNow);

        return !aggregate.IsRevoked;
    }

    /// <summary>
    /// Invariant: Revoke always produces a revoked state.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Revoke_AlwaysProducesRevokedState(NonEmptyString reason)
    {
        var reasonVal = reason.Get.Trim();
        if (string.IsNullOrWhiteSpace(reasonVal))
        {
            return true;
        }

        var aggregate = CreateActiveAggregate(LawfulBasisEnum.Consent);
        aggregate.Revoke(reasonVal, DateTimeOffset.UtcNow);

        return aggregate.IsRevoked;
    }

    /// <summary>
    /// Invariant: Register followed by Revoke always results in IsRevoked being true.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property RegisterThenRevoke_IsRevokedIsTrue(NonEmptyString requestTypeName)
    {
        return Prop.ForAll(BasisGen.ToArbitrary(), basis =>
        {
            var name = requestTypeName.Get.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return true;
            }

            var aggregate = LawfulBasisAggregate.Register(
                Guid.NewGuid(), name, basis, null,
                null, null, null, DateTimeOffset.UtcNow);

            aggregate.Revoke("no longer needed", DateTimeOffset.UtcNow);

            return aggregate.IsRevoked;
        });
    }

    /// <summary>
    /// Invariant: Register followed by ChangeBasis preserves the original RequestTypeName.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool RegisterThenChangeBasis_PreservesRequestTypeName(NonEmptyString requestTypeName)
    {
        var name = requestTypeName.Get.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var aggregate = LawfulBasisAggregate.Register(
            Guid.NewGuid(), name, LawfulBasisEnum.Consent, "purpose",
            null, null, null, DateTimeOffset.UtcNow);

        aggregate.ChangeBasis(
            LawfulBasisEnum.LegitimateInterests, "new purpose",
            "LIA-001", null, null, DateTimeOffset.UtcNow);

        return aggregate.RequestTypeName == name;
    }

    #endregion

    #region LIAAggregate Invariants

    /// <summary>
    /// Invariant: Create always produces an aggregate with RequiresReview outcome.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool LIA_Create_AlwaysProducesRequiresReviewOutcome(NonEmptyString reference)
    {
        var refVal = reference.Get.Trim();
        if (string.IsNullOrWhiteSpace(refVal))
        {
            return true;
        }

        var aggregate = CreateLIA(refVal);

        return aggregate.Outcome == LIAOutcomeEnum.RequiresReview;
    }

    /// <summary>
    /// Invariant: Approve always produces an Approved outcome.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool LIA_Approve_AlwaysProducesApprovedOutcome(
        NonEmptyString conclusion,
        NonEmptyString approvedBy)
    {
        var conclusionVal = conclusion.Get.Trim();
        var approvedByVal = approvedBy.Get.Trim();

        if (string.IsNullOrWhiteSpace(conclusionVal) ||
            string.IsNullOrWhiteSpace(approvedByVal))
        {
            return true;
        }

        var aggregate = CreateLIA("LIA-TEST-001");
        aggregate.Approve(conclusionVal, approvedByVal, DateTimeOffset.UtcNow);

        return aggregate.Outcome == LIAOutcomeEnum.Approved;
    }

    /// <summary>
    /// Invariant: Reject always produces a Rejected outcome.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool LIA_Reject_AlwaysProducesRejectedOutcome(
        NonEmptyString conclusion,
        NonEmptyString rejectedBy)
    {
        var conclusionVal = conclusion.Get.Trim();
        var rejectedByVal = rejectedBy.Get.Trim();

        if (string.IsNullOrWhiteSpace(conclusionVal) ||
            string.IsNullOrWhiteSpace(rejectedByVal))
        {
            return true;
        }

        var aggregate = CreateLIA("LIA-TEST-002");
        aggregate.Reject(conclusionVal, rejectedByVal, DateTimeOffset.UtcNow);

        return aggregate.Outcome == LIAOutcomeEnum.Rejected;
    }

    /// <summary>
    /// Invariant: Create preserves the Reference property.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool LIA_Create_PreservesReference(NonEmptyString reference)
    {
        var refVal = reference.Get.Trim();
        if (string.IsNullOrWhiteSpace(refVal))
        {
            return true;
        }

        var aggregate = CreateLIA(refVal);

        return aggregate.Reference == refVal;
    }

    #endregion

    #region Helpers

    private static LawfulBasisAggregate CreateActiveAggregate(LawfulBasisEnum basis)
    {
        return LawfulBasisAggregate.Register(
            Guid.NewGuid(), "MyApp.Commands.ProcessOrder", basis, "Order processing",
            null, null, null, DateTimeOffset.UtcNow);
    }

    private static LIAAggregate CreateLIA(string reference)
    {
        return LIAAggregate.Create(
            Guid.NewGuid(),
            reference,
            "Fraud Detection LIA",
            "Fraud prevention processing",
            "Preventing fraudulent transactions",
            "Reduced fraud losses, customer trust",
            "Increased fraud exposure, financial losses",
            "Processing is necessary for fraud detection",
            ["Manual review", "Rule-based systems"],
            "Only transaction metadata is processed",
            "Transaction amounts, IP addresses, timestamps",
            "Customers expect fraud protection for payments",
            "Minimal impact with appropriate safeguards",
            ["Pseudonymization", "Access controls", "Retention limits"],
            "DPO",
            dpoInvolvement: true,
            DateTimeOffset.UtcNow);
    }

    #endregion
}
