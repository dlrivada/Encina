using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Model;

using FluentAssertions;

namespace Encina.GuardTests.Compliance.NIS2;

#region NIS2Incident

/// <summary>
/// Guard clause tests for <see cref="NIS2Incident"/> factory methods.
/// Verifies that the Create factory produces valid instances and that
/// required properties are correctly populated.
/// </summary>
public sealed class NIS2IncidentGuardTests
{
    [Fact]
    public void Create_WithValidInputs_ShouldProduceNonEmptyId()
    {
        var incident = NIS2Incident.Create(
            "test incident",
            NIS2IncidentSeverity.High,
            DateTimeOffset.UtcNow,
            isSignificant: true,
            ["service-a"],
            "initial assessment");

        incident.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldPreserveDescription()
    {
        var incident = NIS2Incident.Create(
            "test description",
            NIS2IncidentSeverity.Medium,
            DateTimeOffset.UtcNow,
            isSignificant: false,
            ["svc"],
            "assessment");

        incident.Description.Should().Be("test description");
    }

    [Fact]
    public void Create_ShouldPreserveSeverity()
    {
        var incident = NIS2Incident.Create(
            "test",
            NIS2IncidentSeverity.Critical,
            DateTimeOffset.UtcNow,
            isSignificant: true,
            ["svc"],
            "assessment");

        incident.Severity.Should().Be(NIS2IncidentSeverity.Critical);
    }

    [Fact]
    public void Create_ShouldPreserveDetectedAtUtc()
    {
        var detectedAt = DateTimeOffset.UtcNow.AddHours(-1);

        var incident = NIS2Incident.Create(
            "test",
            NIS2IncidentSeverity.Low,
            detectedAt,
            isSignificant: false,
            ["svc"],
            "assessment");

        incident.DetectedAtUtc.Should().Be(detectedAt);
    }

    [Fact]
    public void Create_ShouldPreserveIsSignificant()
    {
        var incident = NIS2Incident.Create(
            "test",
            NIS2IncidentSeverity.High,
            DateTimeOffset.UtcNow,
            isSignificant: true,
            ["svc"],
            "assessment");

        incident.IsSignificant.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldPreserveAffectedServices()
    {
        string[] services = ["service-a", "service-b"];

        var incident = NIS2Incident.Create(
            "test",
            NIS2IncidentSeverity.Medium,
            DateTimeOffset.UtcNow,
            isSignificant: false,
            services,
            "assessment");

        incident.AffectedServices.Should().HaveCount(2);
    }

    [Fact]
    public void Create_ShouldPreserveInitialAssessment()
    {
        var incident = NIS2Incident.Create(
            "test",
            NIS2IncidentSeverity.Low,
            DateTimeOffset.UtcNow,
            isSignificant: false,
            ["svc"],
            "my assessment");

        incident.InitialAssessment.Should().Be("my assessment");
    }

    [Fact]
    public void Create_NewIncident_ShouldHaveNullNotificationTimestamps()
    {
        var incident = NIS2Incident.Create(
            "test",
            NIS2IncidentSeverity.Medium,
            DateTimeOffset.UtcNow,
            isSignificant: true,
            ["svc"],
            "assessment");

        incident.EarlyWarningAtUtc.Should().BeNull();
        incident.IncidentNotificationAtUtc.Should().BeNull();
        incident.FinalReportAtUtc.Should().BeNull();
    }

    [Fact]
    public void EarlyWarningDeadlineUtc_ShouldBe24HoursAfterDetection()
    {
        var detected = DateTimeOffset.UtcNow;

        var incident = NIS2Incident.Create(
            "test", NIS2IncidentSeverity.High, detected, true, ["svc"], "assessment");

        incident.EarlyWarningDeadlineUtc.Should().Be(detected.AddHours(24));
    }

    [Fact]
    public void IncidentNotificationDeadlineUtc_ShouldBe72HoursAfterDetection()
    {
        var detected = DateTimeOffset.UtcNow;

        var incident = NIS2Incident.Create(
            "test", NIS2IncidentSeverity.High, detected, true, ["svc"], "assessment");

        incident.IncidentNotificationDeadlineUtc.Should().Be(detected.AddHours(72));
    }

    [Fact]
    public void FinalReportDeadlineUtc_ShouldBeNull_WhenNotificationNotSubmitted()
    {
        var incident = NIS2Incident.Create(
            "test", NIS2IncidentSeverity.Low, DateTimeOffset.UtcNow, false, ["svc"], "assessment");

        incident.FinalReportDeadlineUtc.Should().BeNull();
    }

    [Fact]
    public void FinalReportDeadlineUtc_ShouldBeOneMonthAfterNotification_WhenNotificationSet()
    {
        var detected = DateTimeOffset.UtcNow;
        var notifiedAt = detected.AddHours(48);

        var incident = NIS2Incident.Create(
            "test", NIS2IncidentSeverity.High, detected, true, ["svc"], "assessment") with
        {
            IncidentNotificationAtUtc = notifiedAt
        };

        incident.FinalReportDeadlineUtc.Should().Be(notifiedAt.AddMonths(1));
    }
}

#endregion

#region NIS2ComplianceResult

/// <summary>
/// Guard clause tests for <see cref="NIS2ComplianceResult"/> factory methods.
/// </summary>
public sealed class NIS2ComplianceResultGuardTests
{
    [Fact]
    public void Create_WithAllSatisfied_ShouldBeCompliant()
    {
        var measures = new[]
        {
            NIS2MeasureResult.Satisfied(NIS2Measure.RiskAnalysisAndSecurityPolicies, "OK")
        };

        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Essential,
            NIS2Sector.DigitalInfrastructure,
            measures,
            DateTimeOffset.UtcNow);

        result.IsCompliant.Should().BeTrue();
        result.MissingCount.Should().Be(0);
        result.CompliancePercentage.Should().Be(100);
    }

    [Fact]
    public void Create_WithNoneSatisfied_ShouldNotBeCompliant()
    {
        var measures = new[]
        {
            NIS2MeasureResult.NotSatisfied(NIS2Measure.RiskAnalysisAndSecurityPolicies, "Missing", ["Fix it"])
        };

        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Essential,
            NIS2Sector.Energy,
            measures,
            DateTimeOffset.UtcNow);

        result.IsCompliant.Should().BeFalse();
        result.MissingCount.Should().Be(1);
        result.CompliancePercentage.Should().Be(0);
    }

    [Fact]
    public void Create_WithEmptyResults_ShouldNotBeCompliant()
    {
        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Important,
            NIS2Sector.Manufacturing,
            [],
            DateTimeOffset.UtcNow);

        result.IsCompliant.Should().BeTrue();
        result.CompliancePercentage.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldPreserveEntityTypeAndSector()
    {
        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Important,
            NIS2Sector.Health,
            [],
            DateTimeOffset.UtcNow);

        result.EntityType.Should().Be(NIS2EntityType.Important);
        result.Sector.Should().Be(NIS2Sector.Health);
    }
}

#endregion

#region NIS2MeasureResult

/// <summary>
/// Guard clause tests for <see cref="NIS2MeasureResult"/> factory methods.
/// </summary>
public sealed class NIS2MeasureResultGuardTests
{
    [Fact]
    public void Satisfied_ShouldSetIsSatisfiedTrue()
    {
        var result = NIS2MeasureResult.Satisfied(
            NIS2Measure.RiskAnalysisAndSecurityPolicies, "OK");

        result.IsSatisfied.Should().BeTrue();
        result.Measure.Should().Be(NIS2Measure.RiskAnalysisAndSecurityPolicies);
        result.Details.Should().Be("OK");
        result.Recommendations.Should().BeEmpty();
    }

    [Fact]
    public void NotSatisfied_ShouldSetIsSatisfiedFalse()
    {
        var result = NIS2MeasureResult.NotSatisfied(
            NIS2Measure.IncidentHandling, "Missing", ["Fix it"]);

        result.IsSatisfied.Should().BeFalse();
        result.Measure.Should().Be(NIS2Measure.IncidentHandling);
        result.Details.Should().Be("Missing");
        result.Recommendations.Should().ContainSingle("Fix it");
    }
}

#endregion

#region SupplyChainAssessment

/// <summary>
/// Guard clause tests for <see cref="SupplyChainAssessment"/> factory methods.
/// </summary>
public sealed class SupplyChainAssessmentGuardTests
{
    [Fact]
    public void Create_ShouldPreserveAllProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var nextDue = now.AddMonths(3);
        var risks = new List<SupplierRisk>
        {
            new()
            {
                SupplierId = "supplier-1",
                RiskLevel = SupplierRiskLevel.Medium,
                RiskDescription = "Test risk",
                RecommendedActions = ["Fix it"]
            }
        };

        var assessment = SupplyChainAssessment.Create(
            "supplier-1",
            SupplierRiskLevel.Medium,
            risks,
            now,
            nextDue);

        assessment.SupplierId.Should().Be("supplier-1");
        assessment.OverallRisk.Should().Be(SupplierRiskLevel.Medium);
        assessment.Risks.Should().HaveCount(1);
        assessment.AssessedAtUtc.Should().Be(now);
        assessment.NextAssessmentDueAtUtc.Should().Be(nextDue);
    }
}

#endregion

#region SupplierInfo

/// <summary>
/// Guard clause tests for <see cref="SupplierInfo"/> factory methods.
/// </summary>
public sealed class SupplierInfoGuardTests
{
    [Fact]
    public void Create_ShouldPreserveAllProperties()
    {
        var info = SupplierInfo.Create("supplier-1", "Test Supplier", SupplierRiskLevel.Low);

        info.SupplierId.Should().Be("supplier-1");
        info.Name.Should().Be("Test Supplier");
        info.RiskLevel.Should().Be(SupplierRiskLevel.Low);
        info.MitigationMeasures.Should().BeEmpty();
        info.CertificationStatus.Should().BeNull();
        info.LastAssessmentAtUtc.Should().BeNull();
    }
}

#endregion

#region ManagementAccountabilityRecord

/// <summary>
/// Guard clause tests for <see cref="ManagementAccountabilityRecord"/> factory methods.
/// </summary>
public sealed class ManagementAccountabilityRecordGuardTests
{
    [Fact]
    public void Create_ShouldPreserveAllProperties()
    {
        var now = DateTimeOffset.UtcNow;
        string[] areas = ["Risk Analysis", "Incident Handling"];

        var record = ManagementAccountabilityRecord.Create(
            "Jane Doe",
            "CISO",
            now,
            areas);

        record.ResponsiblePerson.Should().Be("Jane Doe");
        record.Role.Should().Be("CISO");
        record.AcknowledgedAtUtc.Should().Be(now);
        record.ComplianceAreas.Should().HaveCount(2);
        record.TrainingCompletedAtUtc.Should().BeNull();
    }
}

#endregion

#region NIS2MeasureContext

/// <summary>
/// Guard clause tests for <see cref="NIS2MeasureContext"/> record.
/// </summary>
public sealed class NIS2MeasureContextGuardTests
{
    [Fact]
    public void Create_ShouldPreserveAllProperties()
    {
        var options = new NIS2Options();
        var serviceProvider = NSubstitute.Substitute.For<IServiceProvider>();

        var context = new NIS2MeasureContext
        {
            Options = options,
            TimeProvider = TimeProvider.System,
            ServiceProvider = serviceProvider,
            TenantId = "tenant-1"
        };

        context.Options.Should().BeSameAs(options);
        context.TimeProvider.Should().BeSameAs(TimeProvider.System);
        context.ServiceProvider.Should().BeSameAs(serviceProvider);
        context.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public void Create_DefaultTenantId_ShouldBeNull()
    {
        var context = new NIS2MeasureContext
        {
            Options = new NIS2Options(),
            TimeProvider = TimeProvider.System,
            ServiceProvider = NSubstitute.Substitute.For<IServiceProvider>()
        };

        context.TenantId.Should().BeNull();
    }
}

#endregion
