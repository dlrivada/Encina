using Encina.Compliance.DataSubjectRights;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="CsvExportFormatWriter"/> verifying null data argument.
/// </summary>
public class CsvExportFormatWriterGuardTests
{
    private readonly CsvExportFormatWriter _sut = new();

    [Fact]
    public async Task WriteAsync_NullData_ThrowsArgumentNullException()
    {
        var act = () => _sut.WriteAsync(null!).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("data");
    }

    [Fact]
    public void SupportedFormat_ReturnsCsv()
    {
        _sut.SupportedFormat.ShouldBe(ExportFormat.CSV);
    }

    [Fact]
    public async Task WriteAsync_EmptyData_ReturnsRight()
    {
        var result = await _sut.WriteAsync([]);
        result.IsRight.ShouldBeTrue();
    }
}
