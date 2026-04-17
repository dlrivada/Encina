using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Abstractions;
using Encina.Compliance.Consent.ReadModels;
using LanguageExt;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Consent;

/// <summary>
/// Unit tests for <see cref="DefaultConsentValidator"/>.
/// </summary>
public class DefaultConsentValidatorTests
{
    private readonly IConsentService _consentService;
    private readonly FakeTimeProvider _timeProvider;
    private readonly DefaultConsentValidator _validator;

    public DefaultConsentValidatorTests()
    {
        _consentService = Substitute.For<IConsentService>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero));
        _validator = new DefaultConsentValidator(_consentService, _timeProvider);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConsentService_ShouldThrow()
    {
        var act = () => new DefaultConsentValidator(null!, _timeProvider);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("consentService");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        var act = () => new DefaultConsentValidator(_consentService, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    #endregion

    #region ValidateAsync - All Valid

    [Fact]
    public async Task ValidateAsync_AllPurposesHaveActiveConsent_ShouldReturnValid()
    {
        // Arrange
        SetupConsent("user-1", "marketing", ConsentStatus.Active, "v1");

        // Act
        var result = await _validator.ValidateAsync("user-1", ["marketing"]);

        // Assert
        result.IsRight.ShouldBeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.ShouldBeTrue();
        validation.Errors.ShouldBeEmpty();
        validation.MissingPurposes.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_MultiplePurposesAllValid_ShouldReturnValid()
    {
        // Arrange
        SetupConsent("user-1", "marketing", ConsentStatus.Active, "v1");
        SetupConsent("user-1", "analytics", ConsentStatus.Active, "v1");

        // Act
        var result = await _validator.ValidateAsync("user-1", ["marketing", "analytics"]);

        // Assert
        result.IsRight.ShouldBeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.ShouldBeTrue();
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
        result.IsRight.ShouldBeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.ShouldBeFalse();
        validation.MissingPurposes.ShouldContain("marketing");
        validation.Errors.ShouldHaveSingleItem();
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
        result.IsRight.ShouldBeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.ShouldBeFalse();
        validation.MissingPurposes.ShouldContain("marketing");
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
        result.IsRight.ShouldBeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.ShouldBeFalse();
        validation.MissingPurposes.ShouldContain("marketing");
    }

    [Fact]
    public async Task ValidateAsync_ExpiredByTimestamp_ShouldReturnInvalid()
    {
        // Arrange - consent has Active status but ExpiresAtUtc is in the past
        var expiredAt = _timeProvider.GetUtcNow().AddHours(-1);
        SetupConsentWithExpiry("user-1", "marketing", ConsentStatus.Active, "v1", expiredAt);

        // Act
        var result = await _validator.ValidateAsync("user-1", ["marketing"]);

        // Assert
        result.IsRight.ShouldBeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.ShouldBeFalse();
        validation.MissingPurposes.ShouldContain("marketing");
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
        result.IsRight.ShouldBeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.ShouldBeFalse();
        validation.MissingPurposes.ShouldContain("marketing");
    }

    #endregion

    #region ValidateAsync - Service Infrastructure Error

    [Fact]
    public async Task ValidateAsync_ServiceError_ShouldReturnError()
    {
        // Arrange
#pragma warning disable CA2012
        _consentService.GetConsentBySubjectAndPurposeAsync("user-1", "marketing", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ConsentReadModel>>>(
                Left<EncinaError, Option<ConsentReadModel>>(EncinaError.New("Database error"))));
#pragma warning restore CA2012

        // Act
        var result = await _validator.ValidateAsync("user-1", ["marketing"]);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region ValidateAsync - Guard Clauses

    [Fact]
    public async Task ValidateAsync_NullSubjectId_ShouldThrow()
    {
        var act = async () => await _validator.ValidateAsync(null!, ["marketing"]);
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task ValidateAsync_NullPurposes_ShouldThrow()
    {
        var act = async () => await _validator.ValidateAsync("user-1", null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion

    #region Helpers

    private void SetupConsent(string subjectId, string purpose, ConsentStatus status, string versionId)
    {
        var readModel = new ConsentReadModel
        {
            Id = Guid.NewGuid(),
            DataSubjectId = subjectId,
            Purpose = purpose,
            Status = status,
            ConsentVersionId = versionId,
            GivenAtUtc = _timeProvider.GetUtcNow().AddDays(-7),
            Source = "test",
            Metadata = new Dictionary<string, object?>()
        };

#pragma warning disable CA2012
        _consentService.GetConsentBySubjectAndPurposeAsync(subjectId, purpose, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ConsentReadModel>>>(
                Right<EncinaError, Option<ConsentReadModel>>(Some(readModel))));
#pragma warning restore CA2012
    }

    private void SetupConsentWithExpiry(string subjectId, string purpose, ConsentStatus status, string versionId, DateTimeOffset expiresAt)
    {
        var readModel = new ConsentReadModel
        {
            Id = Guid.NewGuid(),
            DataSubjectId = subjectId,
            Purpose = purpose,
            Status = status,
            ConsentVersionId = versionId,
            GivenAtUtc = _timeProvider.GetUtcNow().AddDays(-7),
            ExpiresAtUtc = expiresAt,
            Source = "test",
            Metadata = new Dictionary<string, object?>()
        };

#pragma warning disable CA2012
        _consentService.GetConsentBySubjectAndPurposeAsync(subjectId, purpose, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ConsentReadModel>>>(
                Right<EncinaError, Option<ConsentReadModel>>(Some(readModel))));
#pragma warning restore CA2012
    }

    private void SetupNoConsent(string subjectId, string purpose)
    {
#pragma warning disable CA2012
        _consentService.GetConsentBySubjectAndPurposeAsync(subjectId, purpose, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ConsentReadModel>>>(
                Right<EncinaError, Option<ConsentReadModel>>(None)));
#pragma warning restore CA2012
    }

    #endregion
}
