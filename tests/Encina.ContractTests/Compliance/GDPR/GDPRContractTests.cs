#pragma warning disable CA1859 // Contract tests intentionally use interface types to verify contracts

using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Export;
using LanguageExt;

namespace Encina.ContractTests.Compliance.GDPR;

/// <summary>
/// Contract tests for Encina.Compliance.GDPR public interfaces.
/// Verifies that implementations conform to interface contracts.
/// </summary>
public class GDPRContractTests
{
    private static readonly DateTimeOffset FixedTime =
        new(2026, 2, 17, 10, 0, 0, TimeSpan.Zero);

    // -- IProcessingActivityRegistry contract --

    [Fact]
    public async Task IProcessingActivityRegistry_InMemory_RegisterAsync_ReturnsRight()
    {
        // Arrange
        IProcessingActivityRegistry registry = new InMemoryProcessingActivityRegistry();
        var activity = CreateActivity(typeof(string));

        // Act
        var result = await registry.RegisterActivityAsync(activity);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task IProcessingActivityRegistry_InMemory_GetAllAsync_ReturnsRight()
    {
        // Arrange
        IProcessingActivityRegistry registry = new InMemoryProcessingActivityRegistry();

        // Act
        var result = await registry.GetAllActivitiesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var activities = result.Match(
            Right: a => a,
            Left: _ => (IReadOnlyList<ProcessingActivity>)[]);
        activities.ShouldNotBeNull();
    }

    [Fact]
    public async Task IProcessingActivityRegistry_InMemory_GetByRequestType_ReturnsRight()
    {
        // Arrange
        IProcessingActivityRegistry registry = new InMemoryProcessingActivityRegistry();

        // Act
        var result = await registry.GetActivityByRequestTypeAsync(typeof(string));

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task IProcessingActivityRegistry_InMemory_UpdateAsync_NotFound_ReturnsLeft()
    {
        // Arrange
        IProcessingActivityRegistry registry = new InMemoryProcessingActivityRegistry();
        var activity = CreateActivity(typeof(string));

        // Act
        var result = await registry.UpdateActivityAsync(activity);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task IProcessingActivityRegistry_InMemory_RegisterThenGet_RoundTrips()
    {
        // Arrange
        IProcessingActivityRegistry registry = new InMemoryProcessingActivityRegistry();
        var activity = CreateActivity(typeof(string));

        // Act
        await registry.RegisterActivityAsync(activity);
        var result = await registry.GetActivityByRequestTypeAsync(typeof(string));

        // Assert
        result.IsRight.ShouldBeTrue();
        var option = (LanguageExt.Option<ProcessingActivity>)result;
        option.IsSome.ShouldBeTrue();
        option.IfSome(found => found.Name.ShouldBe(activity.Name));
    }

    // -- IGDPRComplianceValidator contract --

    [Fact]
    public async Task IGDPRComplianceValidator_Default_AlwaysReturnsCompliant()
    {
        // Arrange
        IGDPRComplianceValidator validator = new DefaultGDPRComplianceValidator();

        // Act
        var result = await validator.ValidateAsync("test request", RequestContext.CreateForTest());

        // Assert
        result.IsRight.ShouldBeTrue();
        var compliance = (ComplianceResult)result;
        compliance.IsCompliant.ShouldBeTrue();
    }

    // -- IRoPAExporter contract (JSON) --

    [Fact]
    public async Task IRoPAExporter_Json_ContentType_IsApplicationJson()
    {
        IRoPAExporter exporter = new JsonRoPAExporter();
        exporter.ContentType.ShouldBe("application/json");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task IRoPAExporter_Json_FileExtension_IsDotJson()
    {
        IRoPAExporter exporter = new JsonRoPAExporter();
        exporter.FileExtension.ShouldBe(".json");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task IRoPAExporter_Json_ExportAsync_ReturnsRight()
    {
        // Arrange
        IRoPAExporter exporter = new JsonRoPAExporter();
        var metadata = new RoPAExportMetadata("Test", "test@test.com", FixedTime);

        // Act
        var result = await exporter.ExportAsync([CreateActivity(typeof(string))], metadata);

        // Assert
        result.IsRight.ShouldBeTrue();
        var export = (RoPAExportResult)result;
        export.Content.Length.ShouldBeGreaterThan(0);
        export.ActivityCount.ShouldBe(1);
    }

    // -- IRoPAExporter contract (CSV) --

    [Fact]
    public async Task IRoPAExporter_Csv_ContentType_IsTextCsv()
    {
        IRoPAExporter exporter = new CsvRoPAExporter();
        exporter.ContentType.ShouldBe("text/csv");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task IRoPAExporter_Csv_FileExtension_IsDotCsv()
    {
        IRoPAExporter exporter = new CsvRoPAExporter();
        exporter.FileExtension.ShouldBe(".csv");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task IRoPAExporter_Csv_ExportAsync_ReturnsRight()
    {
        // Arrange
        IRoPAExporter exporter = new CsvRoPAExporter();
        var metadata = new RoPAExportMetadata("Test", "test@test.com", FixedTime);

        // Act
        var result = await exporter.ExportAsync([CreateActivity(typeof(string))], metadata);

        // Assert
        result.IsRight.ShouldBeTrue();
        var export = (RoPAExportResult)result;
        export.Content.Length.ShouldBeGreaterThan(0);
        export.ActivityCount.ShouldBe(1);
    }

    // -- ComplianceResult factory contract --

    [Fact]
    public void ComplianceResult_Compliant_IsAlwaysCompliant()
    {
        var result = ComplianceResult.Compliant();
        result.IsCompliant.ShouldBeTrue();
        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void ComplianceResult_NonCompliant_IsNeverCompliant()
    {
        var result = ComplianceResult.NonCompliant("error");
        result.IsCompliant.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThan(0);
    }

    // -- IDataProtectionOfficer contract --

    [Fact]
    public void IDataProtectionOfficer_DataProtectionOfficer_ImplementsInterface()
    {
        IDataProtectionOfficer dpo = new DataProtectionOfficer("Jane", "jane@example.com", "+1-555-0100");
        dpo.Name.ShouldBe("Jane");
        dpo.Email.ShouldBe("jane@example.com");
        dpo.Phone.ShouldBe("+1-555-0100");
    }

    [Fact]
    public void IDataProtectionOfficer_PhoneIsOptional()
    {
        IDataProtectionOfficer dpo = new DataProtectionOfficer("Jane", "jane@example.com");
        dpo.Phone.ShouldBeNull();
    }

    // -- Helper --

    private static ProcessingActivity CreateActivity(Type requestType) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Activity",
        Purpose = "Contract testing",
        LawfulBasis = LawfulBasis.Contract,
        CategoriesOfDataSubjects = ["Users"],
        CategoriesOfPersonalData = ["Email"],
        Recipients = [],
        RetentionPeriod = TimeSpan.FromDays(365),
        SecurityMeasures = "Encryption",
        RequestType = requestType,
        CreatedAtUtc = FixedTime,
        LastUpdatedAtUtc = FixedTime
    };
}
