using Encina.Compliance.Consent;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Consent;

/// <summary>
/// Unit tests for <see cref="InMemoryConsentVersionManager"/>.
/// </summary>
public class InMemoryConsentVersionManagerTests
{
    private readonly IConsentStore _consentStore;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<InMemoryConsentVersionManager> _logger;
    private readonly InMemoryConsentVersionManager _manager;

    public InMemoryConsentVersionManagerTests()
    {
        _consentStore = Substitute.For<IConsentStore>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero));
        _logger = Substitute.For<ILogger<InMemoryConsentVersionManager>>();
        _manager = new InMemoryConsentVersionManager(_consentStore, _timeProvider, _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConsentStore_ShouldThrow()
    {
        var act = () => new InMemoryConsentVersionManager(null!, _timeProvider, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("consentStore");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        var act = () => new InMemoryConsentVersionManager(_consentStore, null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new InMemoryConsentVersionManager(_consentStore, _timeProvider, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region GetCurrentVersionAsync Tests

    [Fact]
    public async Task GetCurrentVersionAsync_ExistingVersion_ShouldReturnVersion()
    {
        // Arrange
        var version = CreateVersion("marketing-v1", ConsentPurposes.Marketing);
        await _manager.PublishNewVersionAsync(version);

        // Act
        var result = await _manager.GetCurrentVersionAsync(ConsentPurposes.Marketing);

        // Assert
        result.IsRight.Should().BeTrue();
        var retrieved = (ConsentVersion)result;
        retrieved.VersionId.Should().Be("marketing-v1");
    }

    [Fact]
    public async Task GetCurrentVersionAsync_NonExistentPurpose_ShouldReturnError()
    {
        // Act
        var result = await _manager.GetCurrentVersionAsync("unknown-purpose");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentVersionAsync_NullPurpose_ShouldThrow()
    {
        var act = async () => await _manager.GetCurrentVersionAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region PublishNewVersionAsync Tests

    [Fact]
    public async Task PublishNewVersionAsync_NewVersion_ShouldStore()
    {
        // Arrange
        var version = CreateVersion("v1", ConsentPurposes.Marketing);

        // Act
        var result = await _manager.PublishNewVersionAsync(version);

        // Assert
        result.IsRight.Should().BeTrue();
        _manager.Count.Should().Be(1);
    }

    [Fact]
    public async Task PublishNewVersionAsync_SamePurpose_ShouldOverwrite()
    {
        // Arrange
        await _manager.PublishNewVersionAsync(CreateVersion("v1", ConsentPurposes.Marketing));

        // Act
        await _manager.PublishNewVersionAsync(CreateVersion("v2", ConsentPurposes.Marketing));

        // Assert
        _manager.Count.Should().Be(1);
        var result = await _manager.GetCurrentVersionAsync(ConsentPurposes.Marketing);
        var version = (ConsentVersion)result;
        version.VersionId.Should().Be("v2");
    }

    [Fact]
    public async Task PublishNewVersionAsync_NullVersion_ShouldThrow()
    {
        var act = async () => await _manager.PublishNewVersionAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region RequiresReconsentAsync Tests

    [Fact]
    public async Task RequiresReconsentAsync_NoVersionRegistered_ShouldReturnFalse()
    {
        // Act
        var result = await _manager.RequiresReconsentAsync("user-1", "unknown-purpose");

        // Assert
        result.IsRight.Should().BeTrue();
        ((bool)result).Should().BeFalse();
    }

    [Fact]
    public async Task RequiresReconsentAsync_VersionDoesNotRequireReconsent_ShouldReturnFalse()
    {
        // Arrange
        var version = CreateVersion("v2", ConsentPurposes.Marketing, requiresReconsent: false);
        await _manager.PublishNewVersionAsync(version);

        // Act
        var result = await _manager.RequiresReconsentAsync("user-1", ConsentPurposes.Marketing);

        // Assert
        result.IsRight.Should().BeTrue();
        ((bool)result).Should().BeFalse();
    }

    [Fact]
    public async Task RequiresReconsentAsync_NoConsentExists_ShouldReturnFalse()
    {
        // Arrange
        var version = CreateVersion("v2", ConsentPurposes.Marketing, requiresReconsent: true);
        await _manager.PublishNewVersionAsync(version);

#pragma warning disable CA2012
        _consentStore.GetConsentAsync("user-1", ConsentPurposes.Marketing, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ConsentRecord>>>(
                Right<EncinaError, Option<ConsentRecord>>(None)));
#pragma warning restore CA2012

        // Act
        var result = await _manager.RequiresReconsentAsync("user-1", ConsentPurposes.Marketing);

        // Assert
        result.IsRight.Should().BeTrue();
        ((bool)result).Should().BeFalse();
    }

    [Fact]
    public async Task RequiresReconsentAsync_ConsentVersionMismatch_ShouldReturnTrue()
    {
        // Arrange
        var version = CreateVersion("v2", ConsentPurposes.Marketing, requiresReconsent: true);
        await _manager.PublishNewVersionAsync(version);

        var consent = new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = "user-1",
            Purpose = ConsentPurposes.Marketing,
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1", // Old version
            GivenAtUtc = _timeProvider.GetUtcNow().AddDays(-30),
            Source = "test",
            Metadata = new Dictionary<string, object?>()
        };

#pragma warning disable CA2012
        _consentStore.GetConsentAsync("user-1", ConsentPurposes.Marketing, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ConsentRecord>>>(
                Right<EncinaError, Option<ConsentRecord>>(Some(consent))));
#pragma warning restore CA2012

        // Act
        var result = await _manager.RequiresReconsentAsync("user-1", ConsentPurposes.Marketing);

        // Assert
        result.IsRight.Should().BeTrue();
        ((bool)result).Should().BeTrue();
    }

    [Fact]
    public async Task RequiresReconsentAsync_ConsentVersionMatch_ShouldReturnFalse()
    {
        // Arrange
        var version = CreateVersion("v2", ConsentPurposes.Marketing, requiresReconsent: true);
        await _manager.PublishNewVersionAsync(version);

        var consent = new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = "user-1",
            Purpose = ConsentPurposes.Marketing,
            Status = ConsentStatus.Active,
            ConsentVersionId = "v2", // Same version
            GivenAtUtc = _timeProvider.GetUtcNow().AddDays(-1),
            Source = "test",
            Metadata = new Dictionary<string, object?>()
        };

#pragma warning disable CA2012
        _consentStore.GetConsentAsync("user-1", ConsentPurposes.Marketing, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ConsentRecord>>>(
                Right<EncinaError, Option<ConsentRecord>>(Some(consent))));
#pragma warning restore CA2012

        // Act
        var result = await _manager.RequiresReconsentAsync("user-1", ConsentPurposes.Marketing);

        // Assert
        result.IsRight.Should().BeTrue();
        ((bool)result).Should().BeFalse();
    }

    #endregion

    #region Testing Utilities

    [Fact]
    public async Task Clear_ShouldRemoveAllVersions()
    {
        // Arrange
        await _manager.PublishNewVersionAsync(CreateVersion("v1", ConsentPurposes.Marketing));
        await _manager.PublishNewVersionAsync(CreateVersion("v1", ConsentPurposes.Analytics));

        // Act
        _manager.Clear();

        // Assert
        _manager.Count.Should().Be(0);
        _manager.GetAllVersions().Should().BeEmpty();
    }

    #endregion

    #region Helpers

    private ConsentVersion CreateVersion(string versionId, string purpose, bool requiresReconsent = false) => new()
    {
        VersionId = versionId,
        Purpose = purpose,
        EffectiveFromUtc = _timeProvider.GetUtcNow(),
        Description = $"Version {versionId}",
        RequiresExplicitReconsent = requiresReconsent
    };

    #endregion
}
