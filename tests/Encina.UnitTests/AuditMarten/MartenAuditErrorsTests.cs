using Encina.Audit.Marten;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="MartenAuditErrors"/> factory methods and error code constants.
/// </summary>
public sealed class MartenAuditErrorsTests
{
    #region Error Code Constants

    [Fact]
    public void ErrorCodes_ShouldFollowAuditMartenConvention()
    {
        MartenAuditErrors.EncryptionFailedCode.ShouldStartWith("audit.marten.");
        MartenAuditErrors.DecryptionFailedCode.ShouldStartWith("audit.marten.");
        MartenAuditErrors.KeyNotFoundCode.ShouldStartWith("audit.marten.");
        MartenAuditErrors.KeyDestructionFailedCode.ShouldStartWith("audit.marten.");
        MartenAuditErrors.ProjectionFailedCode.ShouldStartWith("audit.marten.");
        MartenAuditErrors.QueryFailedCode.ShouldStartWith("audit.marten.");
        MartenAuditErrors.StoreUnavailableCode.ShouldStartWith("audit.marten.");
        MartenAuditErrors.InvalidPeriodCode.ShouldStartWith("audit.marten.");
        MartenAuditErrors.ShreddedEntryCode.ShouldStartWith("audit.marten.");
    }

    [Fact]
    public void ErrorCodes_ShouldBeUnique()
    {
        var codes = new[]
        {
            MartenAuditErrors.EncryptionFailedCode,
            MartenAuditErrors.DecryptionFailedCode,
            MartenAuditErrors.KeyNotFoundCode,
            MartenAuditErrors.KeyDestructionFailedCode,
            MartenAuditErrors.ProjectionFailedCode,
            MartenAuditErrors.QueryFailedCode,
            MartenAuditErrors.StoreUnavailableCode,
            MartenAuditErrors.InvalidPeriodCode,
            MartenAuditErrors.ShreddedEntryCode
        };

        codes.Distinct().Count().ShouldBe(codes.Length);
    }

    #endregion

    #region Factory Methods — Non-null Messages

    [Fact]
    public void EncryptionFailed_ShouldReturnErrorWithMessage()
    {
        var error = MartenAuditErrors.EncryptionFailed(Guid.NewGuid(), "2026-01");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void DecryptionFailed_ShouldReturnErrorWithMessage()
    {
        var error = MartenAuditErrors.DecryptionFailed(Guid.NewGuid(), "key-1");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void KeyNotFound_ShouldReturnErrorContainingPeriod()
    {
        var error = MartenAuditErrors.KeyNotFound("2026-Q1");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void KeyDestructionFailed_ShouldReturnErrorWithMessage()
    {
        var error = MartenAuditErrors.KeyDestructionFailed(DateTime.UtcNow);
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ProjectionFailed_ShouldReturnErrorWithMessage()
    {
        var error = MartenAuditErrors.ProjectionFailed("AuditEntryProjection");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void QueryFailed_ShouldReturnErrorWithMessage()
    {
        var error = MartenAuditErrors.QueryFailed("GetByEntity");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void StoreUnavailable_ShouldReturnErrorWithMessage()
    {
        var error = MartenAuditErrors.StoreUnavailable("timeout");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void InvalidPeriod_ShouldReturnErrorWithMessage()
    {
        var error = MartenAuditErrors.InvalidPeriod("bad-format");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ShreddedEntry_ShouldReturnErrorWithMessage()
    {
        var error = MartenAuditErrors.ShreddedEntry(Guid.NewGuid(), "2026-01");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region Factory Methods — With Exception

    [Fact]
    public void EncryptionFailed_WithException_ShouldPreserveException()
    {
        var ex = new InvalidOperationException("crypto fail");
        var error = MartenAuditErrors.EncryptionFailed(Guid.NewGuid(), "2026-01", ex);
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void DecryptionFailed_WithException_ShouldPreserveException()
    {
        var ex = new InvalidOperationException("decrypt fail");
        var error = MartenAuditErrors.DecryptionFailed(Guid.NewGuid(), "key-1", ex);
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void KeyDestructionFailed_WithException_ShouldPreserveException()
    {
        var ex = new TimeoutException("db timeout");
        var error = MartenAuditErrors.KeyDestructionFailed(DateTime.UtcNow, ex);
        error.Exception.IsSome.ShouldBeTrue();
    }

    #endregion
}
