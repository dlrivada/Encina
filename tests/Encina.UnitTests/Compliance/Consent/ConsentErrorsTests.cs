using Encina.Compliance.Consent;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Consent;

/// <summary>
/// Unit tests for <see cref="ConsentErrors"/>.
/// </summary>
public class ConsentErrorsTests
{
    #region Error Code Constants

    [Fact]
    public void ErrorCodes_ShouldFollowConsentPrefix()
    {
        ConsentErrors.MissingConsentCode.Should().StartWith("consent.");
        ConsentErrors.ConsentExpiredCode.Should().StartWith("consent.");
        ConsentErrors.ConsentWithdrawnCode.Should().StartWith("consent.");
        ConsentErrors.RequiresReconsentCode.Should().StartWith("consent.");
        ConsentErrors.VersionMismatchCode.Should().StartWith("consent.");
        ConsentErrors.ConsentNotFoundCode.Should().StartWith("consent.");
        ConsentErrors.InvalidStateTransitionCode.Should().StartWith("consent.");
        ConsentErrors.ServiceErrorCode.Should().StartWith("consent.");
        ConsentErrors.EventHistoryUnavailableCode.Should().StartWith("consent.");
    }

    [Fact]
    public void ErrorCodes_ShouldHaveExpectedValues()
    {
        ConsentErrors.MissingConsentCode.Should().Be("consent.missing");
        ConsentErrors.ConsentExpiredCode.Should().Be("consent.expired");
        ConsentErrors.ConsentWithdrawnCode.Should().Be("consent.withdrawn");
        ConsentErrors.RequiresReconsentCode.Should().Be("consent.requires_reconsent");
        ConsentErrors.VersionMismatchCode.Should().Be("consent.version_mismatch");
        ConsentErrors.ConsentNotFoundCode.Should().Be("consent.not_found");
        ConsentErrors.InvalidStateTransitionCode.Should().Be("consent.invalid_state_transition");
        ConsentErrors.ServiceErrorCode.Should().Be("consent.service_error");
        ConsentErrors.EventHistoryUnavailableCode.Should().Be("consent.event_history_unavailable");
    }

    #endregion

    #region MissingConsent Factory

    [Fact]
    public void MissingConsent_ShouldContainSubjectIdAndPurpose()
    {
        // Act
        var error = ConsentErrors.MissingConsent("user-123", "marketing");

        // Assert
        error.Message.Should().Contain("user-123");
        error.Message.Should().Contain("marketing");
        error.Message.Should().Contain("Article 6(1)(a)");
    }

    #endregion

    #region ConsentExpired Factory

    [Fact]
    public void ConsentExpired_ShouldContainSubjectIdPurposeAndTimestamp()
    {
        // Arrange
        var expiredAt = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

        // Act
        var error = ConsentErrors.ConsentExpired("user-456", "analytics", expiredAt);

        // Assert
        error.Message.Should().Contain("user-456");
        error.Message.Should().Contain("analytics");
        error.Message.Should().Contain("2026-01-15");
    }

    #endregion

    #region ConsentWithdrawn Factory

    [Fact]
    public void ConsentWithdrawn_ShouldContainSubjectIdPurposeAndTimestamp()
    {
        // Arrange
        var withdrawnAt = new DateTimeOffset(2026, 2, 1, 8, 0, 0, TimeSpan.Zero);

        // Act
        var error = ConsentErrors.ConsentWithdrawn("user-789", "profiling", withdrawnAt);

        // Assert
        error.Message.Should().Contain("user-789");
        error.Message.Should().Contain("profiling");
        error.Message.Should().Contain("Article 7(3)");
    }

    #endregion

    #region RequiresReconsent Factory

    [Fact]
    public void RequiresReconsent_ShouldContainVersionInfo()
    {
        // Act
        var error = ConsentErrors.RequiresReconsent("user-100", "marketing", "v3", "v1");

        // Assert
        error.Message.Should().Contain("user-100");
        error.Message.Should().Contain("marketing");
        error.Message.Should().Contain("v3");
        error.Message.Should().Contain("v1");
    }

    #endregion

    #region VersionMismatch Factory

    [Fact]
    public void VersionMismatch_ShouldContainExpectedAndActualVersions()
    {
        // Act
        var error = ConsentErrors.VersionMismatch("user-200", "analytics", "v5", "v2");

        // Assert
        error.Message.Should().Contain("user-200");
        error.Message.Should().Contain("analytics");
        error.Message.Should().Contain("v5");
        error.Message.Should().Contain("v2");
    }

    #endregion

    #region ConsentNotFound Factory

    [Fact]
    public void ConsentNotFound_ShouldContainConsentId()
    {
        // Arrange
        var consentId = Guid.NewGuid();

        // Act
        var error = ConsentErrors.ConsentNotFound(consentId);

        // Assert
        error.Message.Should().Contain(consentId.ToString());
    }

    #endregion

    #region InvalidStateTransition Factory

    [Fact]
    public void InvalidStateTransition_ShouldContainFromAndToStates()
    {
        // Act
        var error = ConsentErrors.InvalidStateTransition("Active", "Active");

        // Assert
        error.Message.Should().Contain("Active");
        error.Message.Should().Contain("Active");
        error.Message.Should().Contain("Invalid");
    }

    [Fact]
    public void InvalidStateTransition_ShouldContainDistinctStates()
    {
        // Act
        var error = ConsentErrors.InvalidStateTransition("Withdrawn", "Expired");

        // Assert
        error.Message.Should().Contain("Withdrawn");
        error.Message.Should().Contain("Expired");
    }

    #endregion

    #region ServiceError Factory

    [Fact]
    public void ServiceError_ShouldContainOperationAndExceptionMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Connection refused");

        // Act
        var error = ConsentErrors.ServiceError("GrantConsent", exception);

        // Assert
        error.Message.Should().Contain("GrantConsent");
        error.Message.Should().Contain("Connection refused");
    }

    #endregion

    #region EventHistoryUnavailable Factory

    [Fact]
    public void EventHistoryUnavailable_ShouldContainConsentId()
    {
        // Arrange
        var consentId = Guid.NewGuid();

        // Act
        var error = ConsentErrors.EventHistoryUnavailable(consentId);

        // Assert
        error.Message.Should().Contain(consentId.ToString());
    }

    #endregion
}
