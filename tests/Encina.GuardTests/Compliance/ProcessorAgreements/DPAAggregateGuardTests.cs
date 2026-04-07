using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Model;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="DPAAggregate"/> factory method and instance method parameter
/// validation guards (ArgumentNullException, ArgumentException).
/// </summary>
public sealed class DPAAggregateGuardTests
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
    // Execute — factory method guards
    // ========================================================================

    [Fact]
    public void Execute_NullMandatoryTerms_ThrowsArgumentNullException()
    {
        var act = () => DPAAggregate.Execute(
            Guid.NewGuid(), Guid.NewGuid(), null!, true,
            ["purpose"], DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("mandatoryTerms");
    }

    [Fact]
    public void Execute_NullProcessingPurposes_ThrowsArgumentNullException()
    {
        var act = () => DPAAggregate.Execute(
            Guid.NewGuid(), Guid.NewGuid(), FullyCompliantTerms, true,
            null!, DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("processingPurposes");
    }

    [Fact]
    public void Execute_EmptyProcessingPurposes_ThrowsArgumentException()
    {
        var act = () => DPAAggregate.Execute(
            Guid.NewGuid(), Guid.NewGuid(), FullyCompliantTerms, true,
            [], DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("processingPurposes");
    }

    // ========================================================================
    // Amend — instance method guards
    // ========================================================================

    [Fact]
    public void Amend_NullUpdatedTerms_ThrowsArgumentNullException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Amend(
            null!, true, ["purpose"], "reason", DateTimeOffset.UtcNow);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("updatedTerms");
    }

    [Fact]
    public void Amend_NullProcessingPurposes_ThrowsArgumentNullException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Amend(
            FullyCompliantTerms, true, null!, "reason", DateTimeOffset.UtcNow);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("processingPurposes");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Amend_NullOrWhiteSpaceAmendmentReason_ThrowsArgumentException(string? reason)
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Amend(
            FullyCompliantTerms, true, ["purpose"], reason!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Amend_EmptyProcessingPurposes_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Amend(
            FullyCompliantTerms, true, [], "reason", DateTimeOffset.UtcNow);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("processingPurposes");
    }

    // ========================================================================
    // Audit — instance method guards
    // ========================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Audit_NullOrWhiteSpaceAuditorId_ThrowsArgumentException(string? auditorId)
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Audit(auditorId!, "findings", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Audit_NullOrWhiteSpaceAuditFindings_ThrowsArgumentException(string? findings)
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Audit("auditor-1", findings!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act);
    }

    // ========================================================================
    // Terminate — instance method guards
    // ========================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Terminate_NullOrWhiteSpaceReason_ThrowsArgumentException(string? reason)
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Terminate(reason!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act);
    }

    // ========================================================================
    // Execute — valid parameters do not throw
    // ========================================================================

    [Fact]
    public void Execute_ValidParameters_DoesNotThrow()
    {
        var act = () => DPAAggregate.Execute(
            Guid.NewGuid(), Guid.NewGuid(), FullyCompliantTerms, true,
            ["Data processing"], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1),
            DateTimeOffset.UtcNow);

        Should.NotThrow(act);
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
}
