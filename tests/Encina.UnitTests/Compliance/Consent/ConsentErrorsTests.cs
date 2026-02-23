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
    }

    [Fact]
    public void ErrorCodes_ShouldHaveExpectedValues()
    {
        ConsentErrors.MissingConsentCode.Should().Be("consent.missing");
        ConsentErrors.ConsentExpiredCode.Should().Be("consent.expired");
        ConsentErrors.ConsentWithdrawnCode.Should().Be("consent.withdrawn");
        ConsentErrors.RequiresReconsentCode.Should().Be("consent.requires_reconsent");
        ConsentErrors.VersionMismatchCode.Should().Be("consent.version_mismatch");
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
}
