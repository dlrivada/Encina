using Encina.Compliance.DataSubjectRights;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="JsonExportFormatWriter"/> verifying null data argument.
/// </summary>
public class JsonExportFormatWriterGuardTests
{
    private readonly JsonExportFormatWriter _sut = new();

    [Fact]
    public async Task WriteAsync_NullData_ThrowsArgumentNullException()
    {
        var act = () => _sut.WriteAsync(null!).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("data");
    }

    [Fact]
    public void SupportedFormat_ReturnsJson()
    {
        _sut.SupportedFormat.ShouldBe(ExportFormat.JSON);
    }

    [Fact]
    public async Task WriteAsync_EmptyData_ReturnsRight()
    {
        var result = await _sut.WriteAsync([]);
        result.IsRight.ShouldBeTrue();
    }
}
