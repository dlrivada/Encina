using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Model;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="DPAAggregate"/> state transition validation guards
/// that throw <see cref="InvalidOperationException"/> when called from an invalid state.
/// </summary>
public sealed class DPAAggregateStateGuardTests
{
    private static readonly DPAMandatoryTerms FullyCompliantTerms = new()
    {
        ProcessOnDocumentedInstructions = true,
        ConfidentialityObligations = true,
        SecurityMeasures = true,
        SubProcessorRequirements = true,
        DataSubjectRightsAssistance = true,
        ComplianceAssistance = true,
        DataDeletionOrReturn = true,
        AuditRights = true
    };

    // ========================================================================
    // Amend — only allowed in Active or PendingRenewal
    // ========================================================================

    [Fact]
    public void Amend_WhenTerminated_ThrowsInvalidOperationException()
    {
        var aggregate = CreateTerminatedAggregate();

        var act = () => aggregate.Amend(
            FullyCompliantTerms, true, ["purpose"], "reason", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Amend_WhenExpired_ThrowsInvalidOperationException()
    {
        var aggregate = CreateExpiredAggregate();

        var act = () => aggregate.Amend(
            FullyCompliantTerms, true, ["purpose"], "reason", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // Audit — not allowed in Terminated or Expired
    // ========================================================================

    [Fact]
    public void Audit_WhenTerminated_ThrowsInvalidOperationException()
    {
        var aggregate = CreateTerminatedAggregate();

        var act = () => aggregate.Audit("auditor-1", "findings", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Audit_WhenExpired_ThrowsInvalidOperationException()
    {
        var aggregate = CreateExpiredAggregate();

        var act = () => aggregate.Audit("auditor-1", "findings", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // Renew — only allowed in Active or PendingRenewal
    // ========================================================================

    [Fact]
    public void Renew_WhenTerminated_ThrowsInvalidOperationException()
    {
        var aggregate = CreateTerminatedAggregate();

        var act = () => aggregate.Renew(DateTimeOffset.UtcNow.AddYears(1), DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Renew_WhenExpired_ThrowsInvalidOperationException()
    {
        var aggregate = CreateExpiredAggregate();

        var act = () => aggregate.Renew(DateTimeOffset.UtcNow.AddYears(1), DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // Terminate — not allowed when already Terminated or Expired
    // ========================================================================

    [Fact]
    public void Terminate_WhenAlreadyTerminated_ThrowsInvalidOperationException()
    {
        var aggregate = CreateTerminatedAggregate();

        var act = () => aggregate.Terminate("another reason", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Terminate_WhenExpired_ThrowsInvalidOperationException()
    {
        var aggregate = CreateExpiredAggregate();

        var act = () => aggregate.Terminate("reason", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // MarkExpired — not allowed when already Expired or Terminated
    // ========================================================================

    [Fact]
    public void MarkExpired_WhenAlreadyExpired_ThrowsInvalidOperationException()
    {
        var aggregate = CreateExpiredAggregate();

        var act = () => aggregate.MarkExpired(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void MarkExpired_WhenTerminated_ThrowsInvalidOperationException()
    {
        var aggregate = CreateTerminatedAggregate();

        var act = () => aggregate.MarkExpired(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // MarkPendingRenewal — only allowed in Active
    // ========================================================================

    [Fact]
    public void MarkPendingRenewal_WhenTerminated_ThrowsInvalidOperationException()
    {
        var aggregate = CreateTerminatedAggregate();

        var act = () => aggregate.MarkPendingRenewal(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void MarkPendingRenewal_WhenExpired_ThrowsInvalidOperationException()
    {
        var aggregate = CreateExpiredAggregate();

        var act = () => aggregate.MarkPendingRenewal(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void MarkPendingRenewal_WhenAlreadyPendingRenewal_ThrowsInvalidOperationException()
    {
        var aggregate = CreatePendingRenewalAggregate();

        var act = () => aggregate.MarkPendingRenewal(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static DPAAggregate CreateActiveAggregate()
    {
        return DPAAggregate.Execute(
            Guid.NewGuid(), Guid.NewGuid(), FullyCompliantTerms, true,
            ["Data processing"], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1),
            DateTimeOffset.UtcNow);
    }

    private static DPAAggregate CreateTerminatedAggregate()
    {
        var aggregate = CreateActiveAggregate();
        aggregate.Terminate("Contract breach", DateTimeOffset.UtcNow);
        return aggregate;
    }

    private static DPAAggregate CreateExpiredAggregate()
    {
        var aggregate = CreateActiveAggregate();
        aggregate.MarkExpired(DateTimeOffset.UtcNow);
        return aggregate;
    }

    private static DPAAggregate CreatePendingRenewalAggregate()
    {
        var aggregate = CreateActiveAggregate();
        aggregate.MarkPendingRenewal(DateTimeOffset.UtcNow);
        return aggregate;
    }
}
