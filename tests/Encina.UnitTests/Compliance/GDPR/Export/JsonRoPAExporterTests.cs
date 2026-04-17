using System.Text;
using System.Text.Json;
using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Export;
using Shouldly;

namespace Encina.UnitTests.Compliance.GDPR.Export;

/// <summary>
/// Unit tests for <see cref="JsonRoPAExporter"/>.
/// </summary>
public class JsonRoPAExporterTests
{
    private readonly JsonRoPAExporter _sut = new();

    private static readonly DateTimeOffset FixedTime =
        new(2026, 2, 17, 10, 0, 0, TimeSpan.Zero);

    // -- Properties --

    [Fact]
    public void ContentType_ShouldBeApplicationJson()
    {
        _sut.ContentType.ShouldBe("application/json");
    }

    [Fact]
    public void FileExtension_ShouldBeDotJson()
    {
        _sut.FileExtension.ShouldBe(".json");
    }

    // -- ExportAsync --

    [Fact]
    public async Task ExportAsync_EmptyList_ShouldReturnValidJson()
    {
        // Arrange
        var metadata = CreateMetadata();

        // Act
        var result = await _sut.ExportAsync([], metadata);

        // Assert
        result.IsRight.ShouldBeTrue();
        var export = (RoPAExportResult)result;
        export.ActivityCount.ShouldBe(0);
        export.ContentType.ShouldBe("application/json");
        export.FileExtension.ShouldBe(".json");

        var json = Encoding.UTF8.GetString(export.Content);
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("metadata").GetProperty("activityCount").GetInt32().ShouldBe(0);
        doc.RootElement.GetProperty("activities").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task ExportAsync_SingleActivity_ShouldContainAllFields()
    {
        // Arrange
        var activity = CreateActivity();
        var metadata = CreateMetadata();

        // Act
        var result = await _sut.ExportAsync([activity], metadata);

        // Assert
        result.IsRight.ShouldBeTrue();
        var export = (RoPAExportResult)result;
        export.ActivityCount.ShouldBe(1);

        var json = Encoding.UTF8.GetString(export.Content);
        var doc = JsonDocument.Parse(json);

        // Verify metadata
        var meta = doc.RootElement.GetProperty("metadata");
        meta.GetProperty("controllerName").GetString().ShouldBe("Acme Corp");
        meta.GetProperty("controllerEmail").GetString().ShouldBe("privacy@acme.com");
        meta.GetProperty("activityCount").GetInt32().ShouldBe(1);

        // Verify activity
        var activities = doc.RootElement.GetProperty("activities");
        activities.GetArrayLength().ShouldBe(1);
        var act = activities[0];
        act.GetProperty("name").GetString().ShouldBe("Order Processing");
        act.GetProperty("purpose").GetString().ShouldBe("Fulfill orders");
        act.GetProperty("lawfulBasis").GetString().ShouldBe("contract");
        act.GetProperty("retentionDays").GetInt32().ShouldBe(2555);
    }

    [Fact]
    public async Task ExportAsync_WithDPO_ShouldIncludeDPOInMetadata()
    {
        // Arrange
        var dpo = new DataProtectionOfficer("Jane Doe", "dpo@acme.com", "+1-555-0100");
        var metadata = CreateMetadata(dpo);

        // Act
        var result = await _sut.ExportAsync([CreateActivity()], metadata);

        // Assert
        result.IsRight.ShouldBeTrue();
        var json = Encoding.UTF8.GetString(((RoPAExportResult)result).Content);
        var doc = JsonDocument.Parse(json);
        var dpoNode = doc.RootElement.GetProperty("metadata").GetProperty("dataProtectionOfficer");
        dpoNode.GetProperty("name").GetString().ShouldBe("Jane Doe");
        dpoNode.GetProperty("email").GetString().ShouldBe("dpo@acme.com");
        dpoNode.GetProperty("phone").GetString().ShouldBe("+1-555-0100");
    }

    [Fact]
    public async Task ExportAsync_WithoutDPO_ShouldOmitDPO()
    {
        // Arrange
        var metadata = CreateMetadata();

        // Act
        var result = await _sut.ExportAsync([CreateActivity()], metadata);

        // Assert
        result.IsRight.ShouldBeTrue();
        var json = Encoding.UTF8.GetString(((RoPAExportResult)result).Content);
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("metadata")
            .TryGetProperty("dataProtectionOfficer", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task ExportAsync_MultipleActivities_ShouldContainAll()
    {
        // Arrange
        var activities = new[]
        {
            CreateActivity("Activity 1", typeof(string)),
            CreateActivity("Activity 2", typeof(int))
        };
        var metadata = CreateMetadata();

        // Act
        var result = await _sut.ExportAsync(activities, metadata);

        // Assert
        result.IsRight.ShouldBeTrue();
        var export = (RoPAExportResult)result;
        export.ActivityCount.ShouldBe(2);
    }

    [Fact]
    public async Task ExportAsync_NullActivities_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.ExportAsync(null!, CreateMetadata());

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("activities");
    }

    [Fact]
    public async Task ExportAsync_NullMetadata_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.ExportAsync([], null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("metadata");
    }

    [Fact]
    public async Task ExportAsync_ExportedAtUtc_ShouldMatchMetadata()
    {
        // Arrange
        var metadata = CreateMetadata();

        // Act
        var result = await _sut.ExportAsync([], metadata);

        // Assert
        result.IsRight.ShouldBeTrue();
        ((RoPAExportResult)result).ExportedAtUtc.ShouldBe(FixedTime);
    }

    [Fact]
    public async Task ExportAsync_RequestType_ShouldBeFullName()
    {
        // Arrange
        var activity = CreateActivity("Test", typeof(JsonRoPAExporterTests));
        var metadata = CreateMetadata();

        // Act
        var result = await _sut.ExportAsync([activity], metadata);

        // Assert
        result.IsRight.ShouldBeTrue();
        var json = Encoding.UTF8.GetString(((RoPAExportResult)result).Content);
        json.ShouldContain(typeof(JsonRoPAExporterTests).FullName!);
    }

    // -- Helpers --

    private static RoPAExportMetadata CreateMetadata(IDataProtectionOfficer? dpo = null) =>
        new("Acme Corp", "privacy@acme.com", FixedTime, dpo);

    private static ProcessingActivity CreateActivity(
        string name = "Order Processing",
        Type? requestType = null) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Purpose = "Fulfill orders",
            LawfulBasis = LawfulBasis.Contract,
            CategoriesOfDataSubjects = ["Customers"],
            CategoriesOfPersonalData = ["Name", "Email"],
            Recipients = ["Shipping Provider"],
            ThirdCountryTransfers = null,
            Safeguards = null,
            RetentionPeriod = TimeSpan.FromDays(2555),
            SecurityMeasures = "AES-256",
            RequestType = requestType ?? typeof(JsonRoPAExporterTests),
            CreatedAtUtc = FixedTime,
            LastUpdatedAtUtc = FixedTime
        };
}
