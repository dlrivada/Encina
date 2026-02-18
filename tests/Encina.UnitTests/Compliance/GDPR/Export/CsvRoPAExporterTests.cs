using System.Text;
using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Export;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.GDPR.Export;

/// <summary>
/// Unit tests for <see cref="CsvRoPAExporter"/>.
/// </summary>
public class CsvRoPAExporterTests
{
    private readonly CsvRoPAExporter _sut = new();

    private static readonly DateTimeOffset FixedTime =
        new(2026, 2, 17, 10, 0, 0, TimeSpan.Zero);

    // -- Properties --

    [Fact]
    public void ContentType_ShouldBeTextCsv()
    {
        _sut.ContentType.Should().Be("text/csv");
    }

    [Fact]
    public void FileExtension_ShouldBeDotCsv()
    {
        _sut.FileExtension.Should().Be(".csv");
    }

    // -- ExportAsync --

    [Fact]
    public async Task ExportAsync_EmptyList_ShouldContainHeadersOnly()
    {
        // Arrange
        var metadata = CreateMetadata();

        // Act
        var result = await _sut.ExportAsync([], metadata);

        // Assert
        result.IsRight.Should().BeTrue();
        var export = (RoPAExportResult)result;
        export.ActivityCount.Should().Be(0);

        var csv = GetCsvContent(export);
        csv.Should().Contain("# Records of Processing Activities");
        csv.Should().Contain("# Controller: Acme Corp");
        csv.Should().Contain("Id,Name,Purpose,LawfulBasis");
    }

    [Fact]
    public async Task ExportAsync_SingleActivity_ShouldContainDataRow()
    {
        // Arrange
        var activity = CreateActivity();
        var metadata = CreateMetadata();

        // Act
        var result = await _sut.ExportAsync([activity], metadata);

        // Assert
        result.IsRight.Should().BeTrue();
        var csv = GetCsvContent((RoPAExportResult)result);

        // Should contain header row + 1 data row
        var dataLines = csv.Split('\n')
            .Where(l => !l.StartsWith('#') && !string.IsNullOrWhiteSpace(l))
            .ToList();

        // Header + data row
        dataLines.Should().HaveCount(2);
        dataLines[1].Should().Contain("Order Processing");
        dataLines[1].Should().Contain("Contract");
    }

    [Fact]
    public async Task ExportAsync_WithDPO_ShouldIncludeDPOInComments()
    {
        // Arrange
        var dpo = new DataProtectionOfficer("Jane Doe", "dpo@acme.com");
        var metadata = CreateMetadata(dpo);

        // Act
        var result = await _sut.ExportAsync([CreateActivity()], metadata);

        // Assert
        var csv = GetCsvContent((RoPAExportResult)result);
        csv.Should().Contain("# DPO: Jane Doe (dpo@acme.com)");
    }

    [Fact]
    public async Task ExportAsync_ListFields_ShouldBeSemicolonSeparated()
    {
        // Arrange
        var activity = CreateActivity() with
        {
            CategoriesOfPersonalData = (IReadOnlyList<string>)["Name", "Email", "Phone"]
        };
        var metadata = CreateMetadata();

        // Act
        var result = await _sut.ExportAsync([activity], metadata);

        // Assert
        var csv = GetCsvContent((RoPAExportResult)result);
        csv.Should().Contain("Name;Email;Phone");
    }

    [Fact]
    public async Task ExportAsync_FieldWithComma_ShouldBeQuoted()
    {
        // Arrange
        var activity = CreateActivity() with
        {
            Purpose = "Processing, including fulfillment"
        };
        var metadata = CreateMetadata();

        // Act
        var result = await _sut.ExportAsync([activity], metadata);

        // Assert
        var csv = GetCsvContent((RoPAExportResult)result);
        csv.Should().Contain("\"Processing, including fulfillment\"");
    }

    [Fact]
    public async Task ExportAsync_FieldWithQuotes_ShouldBeEscaped()
    {
        // Arrange
        var activity = CreateActivity() with
        {
            Purpose = "Process \"special\" data"
        };
        var metadata = CreateMetadata();

        // Act
        var result = await _sut.ExportAsync([activity], metadata);

        // Assert
        var csv = GetCsvContent((RoPAExportResult)result);
        csv.Should().Contain("\"Process \"\"special\"\" data\"");
    }

    [Fact]
    public async Task ExportAsync_Content_ShouldStartWithBOM()
    {
        // Arrange
        var metadata = CreateMetadata();

        // Act
        var result = await _sut.ExportAsync([], metadata);

        // Assert
        result.IsRight.Should().BeTrue();
        var export = (RoPAExportResult)result;
        var bom = Encoding.UTF8.GetPreamble();
        export.Content.Take(bom.Length).Should().BeEquivalentTo(bom);
    }

    [Fact]
    public async Task ExportAsync_MultipleActivities_ShouldHaveMultipleDataRows()
    {
        // Arrange
        var activities = new[]
        {
            CreateActivity("Activity 1", typeof(string)),
            CreateActivity("Activity 2", typeof(int)),
            CreateActivity("Activity 3", typeof(double))
        };
        var metadata = CreateMetadata();

        // Act
        var result = await _sut.ExportAsync(activities, metadata);

        // Assert
        result.IsRight.Should().BeTrue();
        var export = (RoPAExportResult)result;
        export.ActivityCount.Should().Be(3);

        var csv = GetCsvContent(export);
        var dataLines = csv.Split('\n')
            .Where(l => !l.StartsWith('#') && !string.IsNullOrWhiteSpace(l))
            .ToList();

        dataLines.Should().HaveCount(4); // 1 header + 3 data rows
    }

    [Fact]
    public async Task ExportAsync_NullActivities_ShouldThrowArgumentNullException()
    {
        var act = async () => await _sut.ExportAsync(null!, CreateMetadata());
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("activities");
    }

    [Fact]
    public async Task ExportAsync_NullMetadata_ShouldThrowArgumentNullException()
    {
        var act = async () => await _sut.ExportAsync([], null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("metadata");
    }

    [Fact]
    public async Task ExportAsync_RetentionPeriod_ShouldBeInDays()
    {
        // Arrange
        var activity = CreateActivity() with { RetentionPeriod = TimeSpan.FromDays(730) };
        var metadata = CreateMetadata();

        // Act
        var result = await _sut.ExportAsync([activity], metadata);

        // Assert
        var csv = GetCsvContent((RoPAExportResult)result);
        // Find the data row and check retention days field
        var dataRow = csv.Split('\n')
            .First(l => !l.StartsWith('#') && !string.IsNullOrWhiteSpace(l) && !l.StartsWith("Id", StringComparison.Ordinal));
        dataRow.Should().Contain(",730,");
    }

    [Fact]
    public async Task ExportAsync_NullOptionalFields_ShouldBeEmpty()
    {
        // Arrange
        var activity = CreateActivity() with
        {
            ThirdCountryTransfers = null,
            Safeguards = null
        };
        var metadata = CreateMetadata();

        // Act
        var result = await _sut.ExportAsync([activity], metadata);

        // Assert
        result.IsRight.Should().BeTrue();
        // Should not throw; null fields should be handled as empty strings
    }

    // -- Helpers --

    private static string GetCsvContent(RoPAExportResult export)
    {
        var bom = Encoding.UTF8.GetPreamble();
        return Encoding.UTF8.GetString(export.Content.Skip(bom.Length).ToArray());
    }

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
        RequestType = requestType ?? typeof(CsvRoPAExporterTests),
        CreatedAtUtc = FixedTime,
        LastUpdatedAtUtc = FixedTime
    };
}
