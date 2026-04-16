using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Model;

using Shouldly;

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

        incident.Id.ShouldNotBe(Guid.Empty);
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

        incident.Description.ShouldBe("test description");
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

        incident.Severity.ShouldBe(NIS2IncidentSeverity.Critical);
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

        incident.DetectedAtUtc.ShouldBe(detectedAt);
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

        incident.IsSignificant.ShouldBeTrue();
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

        incident.AffectedServices.Count.ShouldBe(2);
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

        incident.InitialAssessment.ShouldBe("my assessment");
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

        incident.EarlyWarningAtUtc.ShouldBeNull();
        incident.IncidentNotificationAtUtc.ShouldBeNull();
        incident.FinalReportAtUtc.ShouldBeNull();
    }

    [Fact]
    public void EarlyWarningDeadlineUtc_ShouldBe24HoursAfterDetection()
    {
        var detected = DateTimeOffset.UtcNow;

        var incident = NIS2Incident.Create(
            "test", NIS2IncidentSeverity.High, detected, true, ["svc"], "assessment");

        incident.EarlyWarningDeadlineUtc.ShouldBe(detected.AddHours(24));
    }

    [Fact]
    public void IncidentNotificationDeadlineUtc_ShouldBe72HoursAfterDetection()
    {
        var detected = DateTimeOffset.UtcNow;

        var incident = NIS2Incident.Create(
            "test", NIS2IncidentSeverity.High, detected, true, ["svc"], "assessment");

        incident.IncidentNotificationDeadlineUtc.ShouldBe(detected.AddHours(72));
    }

    [Fact]
    public void FinalReportDeadlineUtc_ShouldBeNull_WhenNotificationNotSubmitted()
    {
        var incident = NIS2Incident.Create(
            "test", NIS2IncidentSeverity.Low, DateTimeOffset.UtcNow, false, ["svc"], "assessment");

        incident.FinalReportDeadlineUtc.ShouldBeNull();
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

        incident.FinalReportDeadlineUtc.ShouldBe(notifiedAt.AddMonths(1));
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

        result.IsCompliant.ShouldBeTrue();
        result.MissingCount.ShouldBe(0);
        result.CompliancePercentage.ShouldBe(100);
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

        result.IsCompliant.ShouldBeFalse();
        result.MissingCount.ShouldBe(1);
        result.CompliancePercentage.ShouldBe(0);
    }

    [Fact]
    public void Create_WithEmptyResults_ShouldNotBeCompliant()
    {
        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Important,
            NIS2Sector.Manufacturing,
            [],
            DateTimeOffset.UtcNow);

        result.IsCompliant.ShouldBeTrue();
        result.CompliancePercentage.ShouldBe(0);
    }

    [Fact]
    public void Create_ShouldPreserveEntityTypeAndSector()
    {
        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Important,
            NIS2Sector.Health,
            [],
            DateTimeOffset.UtcNow);

        result.EntityType.ShouldBe(NIS2EntityType.Important);
        result.Sector.ShouldBe(NIS2Sector.Health);
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

        result.IsSatisfied.ShouldBeTrue();
        result.Measure.ShouldBe(NIS2Measure.RiskAnalysisAndSecurityPolicies);
        result.Details.ShouldBe("OK");
        result.Recommendations.ShouldBeEmpty();
    }

    [Fact]
    public void NotSatisfied_ShouldSetIsSatisfiedFalse()
    {
        var result = NIS2MeasureResult.NotSatisfied(
            NIS2Measure.IncidentHandling, "Missing", ["Fix it"]);

        result.IsSatisfied.ShouldBeFalse();
        result.Measure.ShouldBe(NIS2Measure.IncidentHandling);
        result.Details.ShouldBe("Missing");
        result.Recommendations.ShouldContain("Fix it");
        result.Recommendations.Count.ShouldBe(1);
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

        assessment.SupplierId.ShouldBe("supplier-1");
        assessment.OverallRisk.ShouldBe(SupplierRiskLevel.Medium);
        assessment.Risks.Count.ShouldBe(1);
        assessment.AssessedAtUtc.ShouldBe(now);
        assessment.NextAssessmentDueAtUtc.ShouldBe(nextDue);
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

        info.SupplierId.ShouldBe("supplier-1");
        info.Name.ShouldBe("Test Supplier");
        info.RiskLevel.ShouldBe(SupplierRiskLevel.Low);
        info.MitigationMeasures.ShouldBeEmpty();
        info.CertificationStatus.ShouldBeNull();
        info.LastAssessmentAtUtc.ShouldBeNull();
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

        record.ResponsiblePerson.ShouldBe("Jane Doe");
        record.Role.ShouldBe("CISO");
        record.AcknowledgedAtUtc.ShouldBe(now);
        record.ComplianceAreas.Count.ShouldBe(2);
        record.TrainingCompletedAtUtc.ShouldBeNull();
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

        context.Options.ShouldBeSameAs(options);
        context.TimeProvider.ShouldBeSameAs(TimeProvider.System);
        context.ServiceProvider.ShouldBeSameAs(serviceProvider);
        context.TenantId.ShouldBe("tenant-1");
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

        context.TenantId.ShouldBeNull();
    }
}

#endregion
