using System.Text;
using System.Text.Json;

using Encina.Compliance.DataSubjectRights;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for export format writers (CSV, JSON, XML) verifying data export behavior.
/// </summary>
public class ExportFormatWriterTests
{
    private static readonly IReadOnlyList<PersonalDataLocation> TestData =
    [
        new PersonalDataLocation
        {
            EntityType = typeof(object),
            EntityId = "entity-1",
            FieldName = "Email",
            Category = PersonalDataCategory.Contact,
            IsErasable = true,
            IsPortable = true,
            HasLegalRetention = false,
            CurrentValue = "test@example.com"
        },
        new PersonalDataLocation
        {
            EntityType = typeof(object),
            EntityId = "entity-1",
            FieldName = "Name",
            Category = PersonalDataCategory.Identity,
            IsErasable = true,
            IsPortable = true,
            HasLegalRetention = false,
            CurrentValue = "John Doe"
        }
    ];

    #region CsvExportFormatWriter

    [Fact]
    public async Task CsvWriter_WriteAsync_EmptyData_ReturnsHeaderOnly()
    {
        var sut = new CsvExportFormatWriter();

        var result = await sut.WriteAsync(Array.Empty<PersonalDataLocation>());

        result.IsRight.ShouldBeTrue();
        var data = result.RightAsEnumerable().First();
        data.ContentType.ShouldBe("text/csv");
        data.Format.ShouldBe(ExportFormat.CSV);
        data.FieldCount.ShouldBe(0);
        data.FileName.ShouldContain(".csv");
    }

    [Fact]
    public async Task CsvWriter_WriteAsync_WithData_ProducesValidCsv()
    {
        var sut = new CsvExportFormatWriter();

        var result = await sut.WriteAsync(TestData);

        result.IsRight.ShouldBeTrue();
        var data = result.RightAsEnumerable().First();
        data.FieldCount.ShouldBe(2);

        // Check content has header and data rows
        var content = Encoding.UTF8.GetString(data.Content);
        content.ShouldContain("EntityType");
        content.ShouldContain("FieldName");
        content.ShouldContain("Email");
        content.ShouldContain("test@example.com");
    }

    [Fact]
    public async Task CsvWriter_WriteAsync_EscapesSpecialCharacters()
    {
        var dataWithComma = new List<PersonalDataLocation>
        {
            new()
            {
                EntityType = typeof(object),
                EntityId = "entity-1",
                FieldName = "Address",
                Category = PersonalDataCategory.Contact,
                IsErasable = true,
                IsPortable = true,
                HasLegalRetention = false,
                CurrentValue = "123 Main St, Suite 400"
            }
        };

        var sut = new CsvExportFormatWriter();
        var result = await sut.WriteAsync(dataWithComma);

        result.IsRight.ShouldBeTrue();
        var content = Encoding.UTF8.GetString(result.RightAsEnumerable().First().Content);
        // Commas in values should be quoted
        content.ShouldContain("\"123 Main St, Suite 400\"");
    }

    #endregion

    #region JsonExportFormatWriter

    [Fact]
    public async Task JsonWriter_WriteAsync_EmptyData_ReturnsEmptyArray()
    {
        var sut = new JsonExportFormatWriter();

        var result = await sut.WriteAsync(Array.Empty<PersonalDataLocation>());

        result.IsRight.ShouldBeTrue();
        var data = result.RightAsEnumerable().First();
        data.ContentType.ShouldBe("application/json");
        data.Format.ShouldBe(ExportFormat.JSON);
        data.FieldCount.ShouldBe(0);
        data.FileName.ShouldContain(".json");
    }

    [Fact]
    public async Task JsonWriter_WriteAsync_WithData_ProducesValidJson()
    {
        var sut = new JsonExportFormatWriter();

        var result = await sut.WriteAsync(TestData);

        result.IsRight.ShouldBeTrue();
        var data = result.RightAsEnumerable().First();
        data.FieldCount.ShouldBe(2);

        // Verify it's valid JSON
        var content = Encoding.UTF8.GetString(data.Content);
        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.RootElement.GetArrayLength().ShouldBe(2);
    }

    #endregion

    #region XmlExportFormatWriter

    [Fact]
    public async Task XmlWriter_WriteAsync_EmptyData_ReturnsEmptyRoot()
    {
        var sut = new XmlExportFormatWriter();

        var result = await sut.WriteAsync(Array.Empty<PersonalDataLocation>());

        result.IsRight.ShouldBeTrue();
        var data = result.RightAsEnumerable().First();
        data.ContentType.ShouldBe("application/xml");
        data.Format.ShouldBe(ExportFormat.XML);
        data.FieldCount.ShouldBe(0);
        data.FileName.ShouldContain(".xml");
    }

    [Fact]
    public async Task XmlWriter_WriteAsync_WithData_ProducesValidXml()
    {
        var sut = new XmlExportFormatWriter();

        var result = await sut.WriteAsync(TestData);

        result.IsRight.ShouldBeTrue();
        var data = result.RightAsEnumerable().First();
        data.FieldCount.ShouldBe(2);

        // Verify it's valid XML
        var content = Encoding.UTF8.GetString(data.Content);
        content.ShouldContain("<PersonalData>");
        content.ShouldContain("<DataField>");
        content.ShouldContain("<FieldName>Email</FieldName>");
    }

    #endregion
}
