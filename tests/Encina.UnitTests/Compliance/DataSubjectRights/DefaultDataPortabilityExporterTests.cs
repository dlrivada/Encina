using Encina.Compliance.DataSubjectRights;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for <see cref="DefaultDataPortabilityExporter"/>.
/// </summary>
public class DefaultDataPortabilityExporterTests
{
    private readonly IPersonalDataLocator _locator;
    private readonly IExportFormatWriter _jsonWriter;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<DefaultDataPortabilityExporter> _logger;
    private readonly DefaultDataPortabilityExporter _exporter;

    public DefaultDataPortabilityExporterTests()
    {
        _locator = Substitute.For<IPersonalDataLocator>();
        _jsonWriter = Substitute.For<IExportFormatWriter>();
        _jsonWriter.SupportedFormat.Returns(ExportFormat.JSON);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 2, 27, 12, 0, 0, TimeSpan.Zero));
        _logger = Substitute.For<ILogger<DefaultDataPortabilityExporter>>();
        _exporter = new DefaultDataPortabilityExporter(_locator, [_jsonWriter], _timeProvider, _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLocator_ShouldThrow()
    {
        var act = () => new DefaultDataPortabilityExporter(null!, [_jsonWriter], _timeProvider, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("locator");
    }

    [Fact]
    public void Constructor_NullWriters_ShouldThrow()
    {
        var act = () => new DefaultDataPortabilityExporter(_locator, null!, _timeProvider, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("writers");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        var act = () => new DefaultDataPortabilityExporter(_locator, [_jsonWriter], null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new DefaultDataPortabilityExporter(_locator, [_jsonWriter], _timeProvider, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region ExportAsync Tests - Format Resolution

    [Fact]
    public async Task ExportAsync_UnsupportedFormat_ShouldReturnError()
    {
        // Act
        var result = await _exporter.ExportAsync("subject-1", ExportFormat.CSV);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(DSRErrors.FormatNotSupportedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task ExportAsync_SupportedFormat_ShouldResolveWriter()
    {
        // Arrange
        var locations = CreatePortableLocations(2);
        SetupLocator("subject-1", locations);
        SetupWriterSuccess();

        // Act
        var result = await _exporter.ExportAsync("subject-1", ExportFormat.JSON);

        // Assert
        result.IsRight.Should().BeTrue();
        await _jsonWriter.Received(1).WriteAsync(Arg.Any<IReadOnlyList<PersonalDataLocation>>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExportAsync Tests - Portable Filtering

    [Fact]
    public async Task ExportAsync_ShouldOnlyExportPortableData()
    {
        // Arrange
        var locations = new List<PersonalDataLocation>
        {
            CreateLocation("Email", isPortable: true),
            CreateLocation("TaxId", isPortable: false),
            CreateLocation("Name", isPortable: true),
        };
        SetupLocator("subject-1", locations);

        IReadOnlyList<PersonalDataLocation>? capturedPortable = null;
        _jsonWriter.WriteAsync(Arg.Do<IReadOnlyList<PersonalDataLocation>>(l => capturedPortable = l), Arg.Any<CancellationToken>())
            .Returns(callInfo => ValueTask.FromResult<Either<EncinaError, ExportedData>>(Right(CreateExportedData())));

        // Act
        await _exporter.ExportAsync("subject-1", ExportFormat.JSON);

        // Assert
        capturedPortable.Should().NotBeNull();
        capturedPortable!.Should().HaveCount(2);
        capturedPortable.Should().OnlyContain(l => l.IsPortable);
    }

    #endregion

    #region ExportAsync Tests - Success

    [Fact]
    public async Task ExportAsync_ValidRequest_ShouldReturnPortabilityResponse()
    {
        // Arrange
        var locations = CreatePortableLocations(3);
        SetupLocator("subject-1", locations);
        SetupWriterSuccess();

        // Act
        var result = await _exporter.ExportAsync("subject-1", ExportFormat.JSON);

        // Assert
        result.IsRight.Should().BeTrue();
        var response = (PortabilityResponse)result;
        response.SubjectId.Should().Be("subject-1");
        response.ExportedData.Should().NotBeNull();
        response.GeneratedAtUtc.Should().Be(_timeProvider.GetUtcNow());
    }

    #endregion

    #region ExportAsync Tests - Locator Failure

    [Fact]
    public async Task ExportAsync_LocatorFails_ShouldReturnError()
    {
        // Arrange
        var error = DSRErrors.LocatorFailed("subject-1", "Timeout");
        _locator.LocateAllDataAsync("subject-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>>(error));

        // Act
        var result = await _exporter.ExportAsync("subject-1", ExportFormat.JSON);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region ExportAsync Tests - Invalid SubjectId

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExportAsync_InvalidSubjectId_ShouldThrow(string? subjectId)
    {
        var act = async () => await _exporter.ExportAsync(subjectId!, ExportFormat.JSON);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Helpers

    private void SetupLocator(string subjectId, List<PersonalDataLocation> locations)
    {
        IReadOnlyList<PersonalDataLocation> readOnly = locations.AsReadOnly();
        _locator.LocateAllDataAsync(subjectId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>>(Right(readOnly)));
    }

    private void SetupWriterSuccess()
    {
        _jsonWriter.WriteAsync(Arg.Any<IReadOnlyList<PersonalDataLocation>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => ValueTask.FromResult<Either<EncinaError, ExportedData>>(Right(CreateExportedData())));
    }

    private static ExportedData CreateExportedData() =>
        new()
        {
            Content = "{}"u8.ToArray(),
            ContentType = "application/json",
            FileName = "data.json",
            Format = ExportFormat.JSON,
            FieldCount = 2
        };

    private static List<PersonalDataLocation> CreatePortableLocations(int count) =>
        Enumerable.Range(1, count)
            .Select(i => CreateLocation($"Field{i}", isPortable: true))
            .ToList();

    private static PersonalDataLocation CreateLocation(
        string fieldName,
        bool isPortable = false,
        PersonalDataCategory category = PersonalDataCategory.Contact) =>
        new()
        {
            EntityType = typeof(object),
            EntityId = "entity-001",
            FieldName = fieldName,
            Category = category,
            IsErasable = true,
            IsPortable = isPortable,
            HasLegalRetention = false,
            CurrentValue = $"value-{fieldName}"
        };

    #endregion
}
