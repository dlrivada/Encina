using Encina.Compliance.NIS2.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.NIS2;

/// <summary>
/// Property-based tests for NIS2 model types verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public sealed class NIS2PropertyTests
{
    #region NIS2Incident Deadline Invariants

    /// <summary>
    /// Invariant: EarlyWarningDeadlineUtc is always DetectedAtUtc + 24 hours,
    /// and IncidentNotificationDeadlineUtc is always DetectedAtUtc + 72 hours,
    /// so EarlyWarning &lt; IncidentNotification always holds.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IncidentDeadlines_EarlyWarningIsBeforeIncidentNotification(DateTimeOffset detectedAt)
    {
        var incident = NIS2Incident.Create(
            "test incident",
            NIS2IncidentSeverity.High,
            detectedAt,
            isSignificant: true,
            ["service-a"],
            "initial assessment");

        return incident.EarlyWarningDeadlineUtc < incident.IncidentNotificationDeadlineUtc;
    }

    /// <summary>
    /// Invariant: EarlyWarningDeadlineUtc is exactly DetectedAtUtc + 24 hours.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IncidentDeadlines_EarlyWarningIsExactly24HoursAfterDetection(DateTimeOffset detectedAt)
    {
        var incident = NIS2Incident.Create(
            "test",
            NIS2IncidentSeverity.Medium,
            detectedAt,
            isSignificant: true,
            ["svc"],
            "assessment");

        return incident.EarlyWarningDeadlineUtc == detectedAt.AddHours(24);
    }

    /// <summary>
    /// Invariant: IncidentNotificationDeadlineUtc is exactly DetectedAtUtc + 72 hours.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IncidentDeadlines_IncidentNotificationIsExactly72HoursAfterDetection(DateTimeOffset detectedAt)
    {
        var incident = NIS2Incident.Create(
            "test",
            NIS2IncidentSeverity.Critical,
            detectedAt,
            isSignificant: true,
            ["svc"],
            "assessment");

        return incident.IncidentNotificationDeadlineUtc == detectedAt.AddHours(72);
    }

    /// <summary>
    /// Invariant: When IncidentNotificationAtUtc is set, FinalReportDeadlineUtc is
    /// exactly 1 month after IncidentNotificationAtUtc, and is always after
    /// IncidentNotificationDeadlineUtc (since notification happens within 72h of detection
    /// and the final report deadline is 1 month after notification).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IncidentDeadlines_FinalReportIsOneMonthAfterNotification(DateTimeOffset detectedAt)
    {
        var notifiedAt = detectedAt.AddHours(48); // within 72h deadline

        var incident = NIS2Incident.Create(
            "test",
            NIS2IncidentSeverity.High,
            detectedAt,
            isSignificant: true,
            ["svc"],
            "assessment") with
        {
            IncidentNotificationAtUtc = notifiedAt
        };

        return incident.FinalReportDeadlineUtc.HasValue
            && incident.FinalReportDeadlineUtc.Value == notifiedAt.AddMonths(1);
    }

    /// <summary>
    /// Invariant: When IncidentNotificationAtUtc is null, FinalReportDeadlineUtc is null.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IncidentDeadlines_FinalReportIsNull_WhenNotificationNotSubmitted(DateTimeOffset detectedAt)
    {
        var incident = NIS2Incident.Create(
            "test",
            NIS2IncidentSeverity.Low,
            detectedAt,
            isSignificant: false,
            ["svc"],
            "assessment");

        return incident.FinalReportDeadlineUtc is null;
    }

    /// <summary>
    /// Invariant: When IncidentNotificationAtUtc is set, all three deadlines
    /// are monotonically ordered: EarlyWarning &lt; IncidentNotification &lt; FinalReport.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IncidentDeadlines_AreMonotonicallyIncreasing_WhenNotificationSet(DateTimeOffset detectedAt)
    {
        // Notification submitted within the 72h window
        var notifiedAt = detectedAt.AddHours(48);

        var incident = NIS2Incident.Create(
            "test",
            NIS2IncidentSeverity.High,
            detectedAt,
            isSignificant: true,
            ["svc"],
            "assessment") with
        {
            IncidentNotificationAtUtc = notifiedAt
        };

        return incident.EarlyWarningDeadlineUtc < incident.IncidentNotificationDeadlineUtc
            && incident.IncidentNotificationDeadlineUtc < incident.FinalReportDeadlineUtc!.Value;
    }

    #endregion

    #region NIS2Incident Create Factory Invariants

    /// <summary>
    /// Invariant: Create always generates a non-empty Guid.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_AlwaysGeneratesNonEmptyId(NonEmptyString description)
    {
        var incident = NIS2Incident.Create(
            description.Get,
            NIS2IncidentSeverity.Medium,
            DateTimeOffset.UtcNow,
            isSignificant: true,
            ["svc"],
            "assessment");

        return incident.Id != Guid.Empty;
    }

    /// <summary>
    /// Invariant: Two calls to Create produce distinct Ids.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_ProducesUniqueIds(NonEmptyString description)
    {
        var now = DateTimeOffset.UtcNow;

        var incident1 = NIS2Incident.Create(
            description.Get, NIS2IncidentSeverity.High, now, true, ["svc"], "assessment");
        var incident2 = NIS2Incident.Create(
            description.Get, NIS2IncidentSeverity.High, now, true, ["svc"], "assessment");

        return incident1.Id != incident2.Id;
    }

    /// <summary>
    /// Invariant: Create preserves the DetectedAtUtc value.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesDetectedAtUtc(DateTimeOffset detectedAt)
    {
        var incident = NIS2Incident.Create(
            "test", NIS2IncidentSeverity.Low, detectedAt, false, ["svc"], "assessment");

        return incident.DetectedAtUtc == detectedAt;
    }

    /// <summary>
    /// Invariant: Create preserves all input parameters.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesAllInputParameters(
        NonEmptyString description,
        NonEmptyString assessment,
        bool isSignificant)
    {
        var now = DateTimeOffset.UtcNow;
        string[] services = ["service-a", "service-b"];

        var incident = NIS2Incident.Create(
            description.Get,
            NIS2IncidentSeverity.Critical,
            now,
            isSignificant,
            services,
            assessment.Get);

        return incident.Description == description.Get
            && incident.Severity == NIS2IncidentSeverity.Critical
            && incident.DetectedAtUtc == now
            && incident.IsSignificant == isSignificant
            && incident.AffectedServices.Count == 2
            && incident.InitialAssessment == assessment.Get;
    }

    /// <summary>
    /// Invariant: Newly created incidents have no notification timestamps.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_NewIncidentHasNoNotificationTimestamps(DateTimeOffset detectedAt)
    {
        var incident = NIS2Incident.Create(
            "test", NIS2IncidentSeverity.Medium, detectedAt, true, ["svc"], "assessment");

        return incident.EarlyWarningAtUtc is null
            && incident.IncidentNotificationAtUtc is null
            && incident.FinalReportAtUtc is null;
    }

    #endregion

    #region NIS2ComplianceResult Invariants

    /// <summary>
    /// Invariant: CompliancePercentage is always in [0, 100] for any combination of measure results.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CompliancePercentage_IsAlwaysBetween0And100(bool[] satisfiedFlags)
    {
        // Ensure we have at least 1 measure result to avoid division by zero edge case
        if (satisfiedFlags is null || satisfiedFlags.Length == 0)
        {
            return true; // Vacuously true
        }

        var measureResults = satisfiedFlags
            .Select((isSatisfied, i) =>
            {
                var measure = (NIS2Measure)(i % 10);
                return isSatisfied
                    ? NIS2MeasureResult.Satisfied(measure, "OK")
                    : NIS2MeasureResult.NotSatisfied(measure, "Missing", ["Fix it"]);
            })
            .ToList();

        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Essential,
            NIS2Sector.DigitalInfrastructure,
            measureResults,
            DateTimeOffset.UtcNow);

        return result.CompliancePercentage >= 0 && result.CompliancePercentage <= 100;
    }

    /// <summary>
    /// Invariant: IsCompliant is true if and only if MissingCount == 0.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IsCompliant_IffMissingCountIsZero(bool[] satisfiedFlags)
    {
        if (satisfiedFlags is null || satisfiedFlags.Length == 0)
        {
            return true;
        }

        var measureResults = satisfiedFlags
            .Select((isSatisfied, i) =>
            {
                var measure = (NIS2Measure)(i % 10);
                return isSatisfied
                    ? NIS2MeasureResult.Satisfied(measure, "OK")
                    : NIS2MeasureResult.NotSatisfied(measure, "Missing", ["Fix it"]);
            })
            .ToList();

        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Essential,
            NIS2Sector.DigitalInfrastructure,
            measureResults,
            DateTimeOffset.UtcNow);

        return result.IsCompliant == (result.MissingCount == 0);
    }

    /// <summary>
    /// Invariant: MissingCount matches the actual number of not-satisfied measures.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool MissingCount_MatchesActualNotSatisfiedCount(bool[] satisfiedFlags)
    {
        if (satisfiedFlags is null || satisfiedFlags.Length == 0)
        {
            return true;
        }

        var measureResults = satisfiedFlags
            .Select((isSatisfied, i) =>
            {
                var measure = (NIS2Measure)(i % 10);
                return isSatisfied
                    ? NIS2MeasureResult.Satisfied(measure, "OK")
                    : NIS2MeasureResult.NotSatisfied(measure, "Missing", ["Fix it"]);
            })
            .ToList();

        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Essential,
            NIS2Sector.DigitalInfrastructure,
            measureResults,
            DateTimeOffset.UtcNow);

        var expectedMissingCount = satisfiedFlags.Count(f => !f);
        return result.MissingCount == expectedMissingCount;
    }

    /// <summary>
    /// Invariant: When all measures are satisfied, CompliancePercentage is 100.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool CompliancePercentage_Is100_WhenAllSatisfied(PositiveInt count)
    {
        var n = Math.Min(count.Get, 20); // Cap to avoid excessive test data

        var measureResults = Enumerable.Range(0, n)
            .Select(i => NIS2MeasureResult.Satisfied((NIS2Measure)(i % 10), "OK"))
            .ToList();

        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Important,
            NIS2Sector.Manufacturing,
            measureResults,
            DateTimeOffset.UtcNow);

        return result.CompliancePercentage == 100;
    }

    /// <summary>
    /// Invariant: When no measures are satisfied, CompliancePercentage is 0.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool CompliancePercentage_Is0_WhenNoneSatisfied(PositiveInt count)
    {
        var n = Math.Min(count.Get, 20);

        var measureResults = Enumerable.Range(0, n)
            .Select(i => NIS2MeasureResult.NotSatisfied((NIS2Measure)(i % 10), "Missing", ["Fix"]))
            .ToList();

        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Essential,
            NIS2Sector.Energy,
            measureResults,
            DateTimeOffset.UtcNow);

        return result.CompliancePercentage == 0;
    }

    /// <summary>
    /// Invariant: CompliancePercentage with empty MeasureResults is 0.
    /// </summary>
    [Fact]
    public void CompliancePercentage_Is0_WhenEmptyResults()
    {
        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Essential,
            NIS2Sector.DigitalInfrastructure,
            [],
            DateTimeOffset.UtcNow);

        result.CompliancePercentage.ShouldBe(0);
    }

    /// <summary>
    /// Invariant: MissingMeasures contains exactly the not-satisfied measures.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool MissingMeasures_ContainsExactlyNotSatisfiedMeasures(bool[] satisfiedFlags)
    {
        if (satisfiedFlags is null || satisfiedFlags.Length == 0)
        {
            return true;
        }

        // Use first 10 flags only to map 1:1 to unique measures
        var flags = satisfiedFlags.Take(10).ToArray();

        var measureResults = flags
            .Select((isSatisfied, i) =>
            {
                var measure = (NIS2Measure)i;
                return isSatisfied
                    ? NIS2MeasureResult.Satisfied(measure, "OK")
                    : NIS2MeasureResult.NotSatisfied(measure, "Missing", ["Fix"]);
            })
            .ToList();

        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Essential,
            NIS2Sector.DigitalInfrastructure,
            measureResults,
            DateTimeOffset.UtcNow);

        var expectedMissing = flags
            .Select((isSatisfied, i) => (isSatisfied, measure: (NIS2Measure)i))
            .Where(x => !x.isSatisfied)
            .Select(x => x.measure)
            .ToHashSet();

        return result.MissingMeasures.ToHashSet().SetEquals(expectedMissing);
    }

    /// <summary>
    /// Invariant: Create preserves EntityType and Sector.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesEntityTypeAndSector(bool isSatisfied)
    {
        var measures = new[]
        {
            isSatisfied
                ? NIS2MeasureResult.Satisfied(NIS2Measure.RiskAnalysisAndSecurityPolicies, "OK")
                : NIS2MeasureResult.NotSatisfied(NIS2Measure.RiskAnalysisAndSecurityPolicies, "Missing", ["Fix"])
        };

        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Important,
            NIS2Sector.Health,
            measures,
            DateTimeOffset.UtcNow);

        return result.EntityType == NIS2EntityType.Important
            && result.Sector == NIS2Sector.Health;
    }

    #endregion

    #region NIS2MeasureResult Invariants

    /// <summary>
    /// Invariant: Satisfied results always have IsSatisfied == true and empty Recommendations.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Satisfied_AlwaysHasIsSatisfiedTrue(NonEmptyString details)
    {
        var allMeasures = Enum.GetValues<NIS2Measure>();
        return allMeasures.All(measure =>
        {
            var result = NIS2MeasureResult.Satisfied(measure, details.Get);
            return result.IsSatisfied && result.Recommendations.Count == 0;
        });
    }

    /// <summary>
    /// Invariant: NotSatisfied results always have IsSatisfied == false.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool NotSatisfied_AlwaysHasIsSatisfiedFalse(NonEmptyString details)
    {
        var allMeasures = Enum.GetValues<NIS2Measure>();
        return allMeasures.All(measure =>
        {
            var result = NIS2MeasureResult.NotSatisfied(measure, details.Get, ["Recommendation"]);
            return !result.IsSatisfied && result.Recommendations.Count > 0;
        });
    }

    #endregion
}
