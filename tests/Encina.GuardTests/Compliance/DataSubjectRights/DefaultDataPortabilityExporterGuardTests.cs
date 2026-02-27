using Encina.Compliance.DataSubjectRights;

using NSubstitute;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="DefaultDataPortabilityExporter"/> to verify null and invalid parameter handling.
/// </summary>
public class DefaultDataPortabilityExporterGuardTests
{
    private readonly DefaultDataPortabilityExporter _exporter;

    public DefaultDataPortabilityExporterGuardTests()
    {
        _exporter = new DefaultDataPortabilityExporter(
            Substitute.For<IPersonalDataLocator>(),
            Array.Empty<IExportFormatWriter>(),
            TimeProvider.System,
            NullLogger<DefaultDataPortabilityExporter>.Instance);
    }

    #region Constructor Guard Tests

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null locator.
    /// </summary>
    [Fact]
    public void Constructor_NullLocator_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataPortabilityExporter(
            null!,
            Array.Empty<IExportFormatWriter>(),
            TimeProvider.System,
            NullLogger<DefaultDataPortabilityExporter>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("locator");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null writers.
    /// </summary>
    [Fact]
    public void Constructor_NullWriters_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataPortabilityExporter(
            Substitute.For<IPersonalDataLocator>(),
            null!,
            TimeProvider.System,
            NullLogger<DefaultDataPortabilityExporter>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("writers");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null timeProvider.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataPortabilityExporter(
            Substitute.For<IPersonalDataLocator>(),
            Array.Empty<IExportFormatWriter>(),
            null!,
            NullLogger<DefaultDataPortabilityExporter>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null logger.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataPortabilityExporter(
            Substitute.For<IPersonalDataLocator>(),
            Array.Empty<IExportFormatWriter>(),
            TimeProvider.System,
            null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region ExportAsync Guard Tests

    /// <summary>
    /// Verifies that ExportAsync throws <see cref="ArgumentException"/> for invalid subjectId.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExportAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var act = async () => await _exporter.ExportAsync(subjectId!, ExportFormat.JSON);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("subjectId");
    }

    #endregion
}
