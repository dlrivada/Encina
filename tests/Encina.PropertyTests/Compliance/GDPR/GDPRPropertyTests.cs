using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Export;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.GDPR;

/// <summary>
/// Property-based tests for Encina.Compliance.GDPR invariants.
/// </summary>
public class GDPRPropertyTests
{
    // -- InMemoryProcessingActivityRegistry invariants --

    [Property(MaxTest = 50)]
    public bool Registry_RegisterThenGet_AlwaysReturnsRegistered(NonEmptyString name, NonEmptyString purpose)
    {
        var registry = new InMemoryProcessingActivityRegistry();
        var requestType = typeof(GDPRPropertyTests);
        var activity = CreateActivity(requestType, name.Get, purpose.Get);

        var registerResult = registry.RegisterActivityAsync(activity).AsTask().Result;
        if (!registerResult.IsRight) return false;

        var getResult = registry.GetActivityByRequestTypeAsync(requestType).AsTask().Result;
        if (!getResult.IsRight) return false;

        var option = (LanguageExt.Option<ProcessingActivity>)getResult;
        return option.Match(
            Some: found => found.Name == name.Get && found.Purpose == purpose.Get,
            None: () => false);
    }

    [Property(MaxTest = 20)]
    public Property Registry_GetAll_CountMatchesRegistrations()
    {
        return Prop.ForAll(
            Gen.Choose(1, 10).ToArbitrary(),
            count =>
            {
                var registry = new InMemoryProcessingActivityRegistry();
                var types = Enumerable.Range(0, count)
                    .Select(i => typeof(GDPRPropertyTests).Assembly.GetTypes().Skip(i).First())
                    .Distinct()
                    .ToList();

                var registered = 0;
                foreach (var type in types)
                {
                    var result = registry.RegisterActivityAsync(CreateActivity(type)).AsTask().Result;
                    if (result.IsRight) registered++;
                }

                var allResult = registry.GetAllActivitiesAsync().AsTask().Result;
                allResult.IsRight.ShouldBeTrue();
                var all = allResult.Match(
                    Right: activities => activities,
                    Left: _ => (IReadOnlyList<ProcessingActivity>)[]);
                all.Count.ShouldBe(registered);
            });
    }

    // -- ComplianceResult invariants --

    [Property(MaxTest = 50)]
    public Property ComplianceResult_Compliant_AlwaysHasEmptyErrors()
    {
        var warningsGen = Gen.Elements("warn1", "warn2", "warn3")
            .ArrayOf()
            .Select(arr => arr.Where(w => !string.IsNullOrEmpty(w)).ToArray());

        return Prop.ForAll(
            Arb.From(warningsGen),
            warnings =>
            {
                var result = warnings.Length > 0
                    ? ComplianceResult.CompliantWithWarnings(warnings)
                    : ComplianceResult.Compliant();

                result.IsCompliant.ShouldBeTrue();
                result.Errors.Count.ShouldBe(0, "compliant results must have no errors");
            });
    }

    [Property(MaxTest = 50)]
    public bool ComplianceResult_NonCompliant_AlwaysHasIsCompliantFalse(NonEmptyString errorMsg)
    {
        var result = ComplianceResult.NonCompliant(errorMsg.Get);
        return !result.IsCompliant && result.Errors.Count > 0;
    }

    // -- Exporter invariants --

    [Property(MaxTest = 20)]
    public Property JsonExporter_ActivityCount_AlwaysMatchesInput()
    {
        return Prop.ForAll(
            Gen.Choose(0, 5).ToArbitrary(),
            count =>
            {
                var exporter = new JsonRoPAExporter();
                var activities = Enumerable.Range(0, count)
                    .Select(i => CreateActivity(
                        typeof(GDPRPropertyTests).Assembly.GetTypes().Skip(i).First(),
                        $"Activity {i}"))
                    .ToList();
                var metadata = new RoPAExportMetadata("Test", "test@test.com", DateTimeOffset.UtcNow);

                var result = exporter.ExportAsync(activities, metadata).AsTask().Result;
                result.IsRight.ShouldBeTrue();
                var export = (RoPAExportResult)result;
                export.ActivityCount.ShouldBe(count);
            });
    }

    [Property(MaxTest = 20)]
    public Property CsvExporter_ActivityCount_AlwaysMatchesInput()
    {
        return Prop.ForAll(
            Gen.Choose(0, 5).ToArbitrary(),
            count =>
            {
                var exporter = new CsvRoPAExporter();
                var activities = Enumerable.Range(0, count)
                    .Select(i => CreateActivity(
                        typeof(GDPRPropertyTests).Assembly.GetTypes().Skip(i).First(),
                        $"Activity {i}"))
                    .ToList();
                var metadata = new RoPAExportMetadata("Test", "test@test.com", DateTimeOffset.UtcNow);

                var result = exporter.ExportAsync(activities, metadata).AsTask().Result;
                result.IsRight.ShouldBeTrue();
                var export = (RoPAExportResult)result;
                export.ActivityCount.ShouldBe(count);
            });
    }

    // -- LawfulBasis enum invariants --

    [Fact]
    public void LawfulBasis_ShouldHaveExactlySixValues()
    {
        Enum.GetValues<LawfulBasis>().Length.ShouldBe(6,
            "GDPR Article 6(1) defines exactly six lawful bases");
    }

    // -- DataProtectionOfficer record invariants --

    [Property(MaxTest = 50)]
    public bool DPO_EqualityByValue_ShouldHold(NonEmptyString name, NonEmptyString email)
    {
        var a = new DataProtectionOfficer(name.Get, email.Get);
        var b = new DataProtectionOfficer(name.Get, email.Get);
        return a == b;
    }

    // -- Helper --

    private static ProcessingActivity CreateActivity(
        Type requestType,
        string name = "Test",
        string purpose = "Testing") => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Purpose = purpose,
            LawfulBasis = LawfulBasis.Contract,
            CategoriesOfDataSubjects = ["Users"],
            CategoriesOfPersonalData = ["Email"],
            Recipients = [],
            RetentionPeriod = TimeSpan.FromDays(365),
            SecurityMeasures = "Encryption",
            RequestType = requestType,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastUpdatedAtUtc = DateTimeOffset.UtcNow
        };
}
