using Encina.Compliance.Consent;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Consent;

/// <summary>
/// Unit tests for <see cref="DefaultConsentValidator"/>.
/// </summary>
public class DefaultConsentValidatorTests
{
    private readonly IConsentStore _consentStore;
    private readonly IConsentVersionManager _versionManager;
    private readonly FakeTimeProvider _timeProvider;
    private readonly DefaultConsentValidator _validator;

    public DefaultConsentValidatorTests()
    {
        _consentStore = Substitute.For<IConsentStore>();
        _versionManager = Substitute.For<IConsentVersionManager>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero));
        _validator = new DefaultConsentValidator(_consentStore, _versionManager, _timeProvider);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConsentStore_ShouldThrow()
    {
        var act = () => new DefaultConsentValidator(null!, _versionManager, _timeProvider);
        act.Should().Throw<ArgumentNullException>().WithParameterName("consentStore");
    }

    [Fact]
    public void Constructor_NullVersionManager_ShouldThrow()
    {
        var act = () => new DefaultConsentValidator(_consentStore, null!, _timeProvider);
        act.Should().Throw<ArgumentNullException>().WithParameterName("versionManager");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        var act = () => new DefaultConsentValidator(_consentStore, _versionManager, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    #endregion

    #region ValidateAsync - All Valid

    [Fact]
    public async Task ValidateAsync_AllPurposesHaveActiveConsent_ShouldReturnValid()
    {
        // Arrange
        SetupConsent("user-1", "marketing", ConsentStatus.Active, "v1");
        SetupNoReconsent("user-1", "marketing");

        // Act
        var result = await _validator.ValidateAsync("user-1", ["marketing"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.Should().BeTrue();
        validation.Errors.Should().BeEmpty();
        validation.MissingPurposes.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_MultiplePurposesAllValid_ShouldReturnValid()
    {
        // Arrange
        SetupConsent("user-1", "marketing", ConsentStatus.Active, "v1");
        SetupConsent("user-1", "analytics", ConsentStatus.Active, "v1");
        SetupNoReconsent("user-1", "marketing");
        SetupNoReconsent("user-1", "analytics");

        // Act
        var result = await _validator.ValidateAsync("user-1", ["marketing", "analytics"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.Should().BeTrue();
    }

    #endregion

    #region ValidateAsync - Missing Consent

    [Fact]
    public async Task ValidateAsync_NoConsentRecord_ShouldReturnInvalid()
    {
        // Arrange
        SetupNoConsent("user-1", "marketing");

        // Act
        var result = await _validator.ValidateAsync("user-1", ["marketing"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.Should().BeFalse();
        validation.MissingPurposes.Should().Contain("marketing");
        validation.Errors.Should().ContainSingle();
    }

    #endregion

    #region ValidateAsync - Withdrawn Consent

    [Fact]
    public async Task ValidateAsync_WithdrawnConsent_ShouldReturnInvalid()
    {
        // Arrange
        SetupConsent("user-1", "marketing", ConsentStatus.Withdrawn, "v1");

        // Act
        var result = await _validator.ValidateAsync("user-1", ["marketing"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.Should().BeFalse();
        validation.MissingPurposes.Should().Contain("marketing");
    }

    #endregion

    #region ValidateAsync - Expired Consent

    [Fact]
    public async Task ValidateAsync_ExpiredConsentByStatus_ShouldReturnInvalid()
    {
        // Arrange
        SetupConsent("user-1", "marketing", ConsentStatus.Expired, "v1");

        // Act
        var result = await _validator.ValidateAsync("user-1", ["marketing"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.Should().BeFalse();
        validation.MissingPurposes.Should().Contain("marketing");
    }

    [Fact]
    public async Task ValidateAsync_ExpiredByTimestamp_ShouldReturnInvalid()
    {
        // Arrange - consent has Active status but ExpiresAtUtc is in the past
        var expiredAt = _timeProvider.GetUtcNow().AddHours(-1);
        SetupConsentWithExpiry("user-1", "marketing", ConsentStatus.Active, "v1", expiredAt);
        SetupNoReconsent("user-1", "marketing");

        // Act
        var result = await _validator.ValidateAsync("user-1", ["marketing"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.Should().BeFalse();
        validation.MissingPurposes.Should().Contain("marketing");
    }

    #endregion

    #region ValidateAsync - RequiresReconsent

    [Fact]
    public async Task ValidateAsync_RequiresReconsentStatus_ShouldReturnInvalid()
    {
        // Arrange
        SetupConsent("user-1", "marketing", ConsentStatus.RequiresReconsent, "v1");

        // Act
        var result = await _validator.ValidateAsync("user-1", ["marketing"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.Should().BeFalse();
        validation.MissingPurposes.Should().Contain("marketing");
    }

    [Fact]
    public async Task ValidateAsync_VersionManagerRequiresReconsent_ShouldReturnInvalid()
    {
        // Arrange
        SetupConsent("user-1", "marketing", ConsentStatus.Active, "v1");
        SetupReconsent("user-1", "marketing");

        // Act
        var result = await _validator.ValidateAsync("user-1", ["marketing"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.Should().BeFalse();
        validation.MissingPurposes.Should().Contain("marketing");
    }

    #endregion

    #region ValidateAsync - Version Manager Error

    [Fact]
    public async Task ValidateAsync_VersionManagerError_ShouldReturnWarning()
    {
        // Arrange
        SetupConsent("user-1", "marketing", ConsentStatus.Active, "v1");

#pragma warning disable CA2012
        _versionManager.RequiresReconsentAsync("user-1", "marketing", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(
                Left<EncinaError, bool>(EncinaError.New("Version manager unavailable"))));
#pragma warning restore CA2012

        // Act
        var result = await _validator.ValidateAsync("user-1", ["marketing"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.Should().BeTrue();
        validation.Warnings.Should().NotBeEmpty();
    }

    #endregion

    #region ValidateAsync - Store Infrastructure Error

    [Fact]
    public async Task ValidateAsync_StoreError_ShouldReturnError()
    {
        // Arrange
#pragma warning disable CA2012
        _consentStore.GetConsentAsync("user-1", "marketing", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ConsentRecord>>>(
                Left<EncinaError, Option<ConsentRecord>>(EncinaError.New("Database error"))));
#pragma warning restore CA2012

        // Act
        var result = await _validator.ValidateAsync("user-1", ["marketing"]);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region ValidateAsync - Guard Clauses

    [Fact]
    public async Task ValidateAsync_NullSubjectId_ShouldThrow()
    {
        var act = async () => await _validator.ValidateAsync(null!, ["marketing"]);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ValidateAsync_NullPurposes_ShouldThrow()
    {
        var act = async () => await _validator.ValidateAsync("user-1", null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Helpers

    private void SetupConsent(string subjectId, string purpose, ConsentStatus status, string versionId)
    {
        var record = new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            Purpose = purpose,
            Status = status,
            ConsentVersionId = versionId,
            GivenAtUtc = _timeProvider.GetUtcNow().AddDays(-7),
            Source = "test",
            Metadata = new Dictionary<string, object?>()
        };

#pragma warning disable CA2012
        _consentStore.GetConsentAsync(subjectId, purpose, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ConsentRecord>>>(
                Right<EncinaError, Option<ConsentRecord>>(Some(record))));
#pragma warning restore CA2012
    }

    private void SetupConsentWithExpiry(string subjectId, string purpose, ConsentStatus status, string versionId, DateTimeOffset expiresAt)
    {
        var record = new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            Purpose = purpose,
            Status = status,
            ConsentVersionId = versionId,
            GivenAtUtc = _timeProvider.GetUtcNow().AddDays(-7),
            ExpiresAtUtc = expiresAt,
            Source = "test",
            Metadata = new Dictionary<string, object?>()
        };

#pragma warning disable CA2012
        _consentStore.GetConsentAsync(subjectId, purpose, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ConsentRecord>>>(
                Right<EncinaError, Option<ConsentRecord>>(Some(record))));
#pragma warning restore CA2012
    }

    private void SetupNoConsent(string subjectId, string purpose)
    {
#pragma warning disable CA2012
        _consentStore.GetConsentAsync(subjectId, purpose, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ConsentRecord>>>(
                Right<EncinaError, Option<ConsentRecord>>(None)));
#pragma warning restore CA2012
    }

    private void SetupNoReconsent(string subjectId, string purpose)
    {
#pragma warning disable CA2012
        _versionManager.RequiresReconsentAsync(subjectId, purpose, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(
                Right<EncinaError, bool>(false)));
#pragma warning restore CA2012
    }

    private void SetupReconsent(string subjectId, string purpose)
    {
#pragma warning disable CA2012
        _versionManager.RequiresReconsentAsync(subjectId, purpose, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(
                Right<EncinaError, bool>(true)));
#pragma warning restore CA2012
    }

    #endregion
}
