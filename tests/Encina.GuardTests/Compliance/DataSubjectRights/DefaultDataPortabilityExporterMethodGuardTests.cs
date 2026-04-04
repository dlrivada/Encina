using Encina.Compliance.DataSubjectRights;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Deep method-level guard tests for <see cref="DefaultDataPortabilityExporter"/>.
/// </summary>
public class DefaultDataPortabilityExporterMethodGuardTests
{
    private readonly DefaultDataPortabilityExporter _sut;

    public DefaultDataPortabilityExporterMethodGuardTests()
    {
        _sut = new DefaultDataPortabilityExporter(
            Substitute.For<IPersonalDataLocator>(),
            Enumerable.Empty<IExportFormatWriter>(),
            TimeProvider.System,
            NullLogger<DefaultDataPortabilityExporter>.Instance);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExportAsync_NullOrWhitespaceSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var act = () => _sut.ExportAsync(subjectId!, ExportFormat.JSON).AsTask();
        await Should.ThrowAsync<ArgumentException>(act);
    }
}
