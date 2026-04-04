using Encina.Compliance.DataSubjectRights;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for export format writers (CSV, JSON, XML) verifying null parameter handling.
/// </summary>
public class ExportFormatWriterGuardTests
{
    #region CsvExportFormatWriter Guards

    [Fact]
    public async Task CsvWriter_WriteAsync_NullData_ThrowsArgumentNullException()
    {
        var sut = new CsvExportFormatWriter();

        var act = () => sut.WriteAsync(null!).AsTask();

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public void CsvWriter_SupportedFormat_ReturnsCsv()
    {
        var sut = new CsvExportFormatWriter();

        sut.SupportedFormat.ShouldBe(ExportFormat.CSV);
    }

    #endregion

    #region JsonExportFormatWriter Guards

    [Fact]
    public async Task JsonWriter_WriteAsync_NullData_ThrowsArgumentNullException()
    {
        var sut = new JsonExportFormatWriter();

        var act = () => sut.WriteAsync(null!).AsTask();

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public void JsonWriter_SupportedFormat_ReturnsJson()
    {
        var sut = new JsonExportFormatWriter();

        sut.SupportedFormat.ShouldBe(ExportFormat.JSON);
    }

    #endregion

    #region XmlExportFormatWriter Guards

    [Fact]
    public async Task XmlWriter_WriteAsync_NullData_ThrowsArgumentNullException()
    {
        var sut = new XmlExportFormatWriter();

        var act = () => sut.WriteAsync(null!).AsTask();

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public void XmlWriter_SupportedFormat_ReturnsXml()
    {
        var sut = new XmlExportFormatWriter();

        sut.SupportedFormat.ShouldBe(ExportFormat.XML);
    }

    #endregion
}
