using Encina.Compliance.Consent;
using Shouldly;

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
        ConsentErrors.MissingConsentCode.ShouldStartWith("consent.");
        ConsentErrors.ConsentExpiredCode.ShouldStartWith("consent.");
        ConsentErrors.ConsentWithdrawnCode.ShouldStartWith("consent.");
        ConsentErrors.RequiresReconsentCode.ShouldStartWith("consent.");
        ConsentErrors.VersionMismatchCode.ShouldStartWith("consent.");
        ConsentErrors.ConsentNotFoundCode.ShouldStartWith("consent.");
        ConsentErrors.InvalidStateTransitionCode.ShouldStartWith("consent.");
        ConsentErrors.ServiceErrorCode.ShouldStartWith("consent.");
        ConsentErrors.EventHistoryUnavailableCode.ShouldStartWith("consent.");
    }

    [Fact]
    public void ErrorCodes_ShouldHaveExpectedValues()
    {
        ConsentErrors.MissingConsentCode.ShouldBe("consent.missing");
        ConsentErrors.ConsentExpiredCode.ShouldBe("consent.expired");
        ConsentErrors.ConsentWithdrawnCode.ShouldBe("consent.withdrawn");
        ConsentErrors.RequiresReconsentCode.ShouldBe("consent.requires_reconsent");
        ConsentErrors.VersionMismatchCode.ShouldBe("consent.version_mismatch");
        ConsentErrors.ConsentNotFoundCode.ShouldBe("consent.not_found");
        ConsentErrors.InvalidStateTransitionCode.ShouldBe("consent.invalid_state_transition");
        ConsentErrors.ServiceErrorCode.ShouldBe("consent.service_error");
        ConsentErrors.EventHistoryUnavailableCode.ShouldBe("consent.event_history_unavailable");
    }

    #endregion

    #region MissingConsent Factory

    [Fact]
    public void MissingConsent_ShouldContainSubjectIdAndPurpose()
    {
        // Act
        var error = ConsentErrors.MissingConsent("user-123", "marketing");

        // Assert
        error.Message.ShouldContain("user-123");
        error.Message.ShouldContain("marketing");
        error.Message.ShouldContain("Article 6(1)(a)");
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
        error.Message.ShouldContain("user-456");
        error.Message.ShouldContain("analytics");
        error.Message.ShouldContain("2026-01-15");
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
        error.Message.ShouldContain("user-789");
        error.Message.ShouldContain("profiling");
        error.Message.ShouldContain("Article 7(3)");
    }

    #endregion

    #region RequiresReconsent Factory

    [Fact]
    public void RequiresReconsent_ShouldContainVersionInfo()
    {
        // Act
        var error = ConsentErrors.RequiresReconsent("user-100", "marketing", "v3", "v1");

        // Assert
        error.Message.ShouldContain("user-100");
        error.Message.ShouldContain("marketing");
        error.Message.ShouldContain("v3");
        error.Message.ShouldContain("v1");
    }

    #endregion

    #region VersionMismatch Factory

    [Fact]
    public void VersionMismatch_ShouldContainExpectedAndActualVersions()
    {
        // Act
        var error = ConsentErrors.VersionMismatch("user-200", "analytics", "v5", "v2");

        // Assert
        error.Message.ShouldContain("user-200");
        error.Message.ShouldContain("analytics");
        error.Message.ShouldContain("v5");
        error.Message.ShouldContain("v2");
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
        error.Message.ShouldContain(consentId.ToString());
    }

    #endregion

    #region InvalidStateTransition Factory

    [Fact]
    public void InvalidStateTransition_ShouldContainFromAndToStates()
    {
        // Act
        var error = ConsentErrors.InvalidStateTransition("Active", "Active");

        // Assert
        error.Message.ShouldContain("Active");
        error.Message.ShouldContain("Active");
        error.Message.ShouldContain("Invalid");
    }

    [Fact]
    public void InvalidStateTransition_ShouldContainDistinctStates()
    {
        // Act
        var error = ConsentErrors.InvalidStateTransition("Withdrawn", "Expired");

        // Assert
        error.Message.ShouldContain("Withdrawn");
        error.Message.ShouldContain("Expired");
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
        error.Message.ShouldContain("GrantConsent");
        error.Message.ShouldContain("Connection refused");
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
        error.Message.ShouldContain(consentId.ToString());
    }

    #endregion
}
