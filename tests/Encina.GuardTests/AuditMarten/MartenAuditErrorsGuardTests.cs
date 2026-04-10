using Encina.Audit.Marten;

namespace Encina.GuardTests.AuditMarten;

/// <summary>
/// Guard tests covering <see cref="MartenAuditErrors"/> factory methods, exercising every
/// public factory method with and without exception arguments.
/// </summary>
public class MartenAuditErrorsGuardTests
{
    [Fact]
    public void EncryptionFailed_WithoutException_ReturnsError()
    {
        var error = MartenAuditErrors.EncryptionFailed(Guid.NewGuid(), "2026-03");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void EncryptionFailed_WithException_AttachesException()
    {
        var ex = new InvalidOperationException("fail");
        var error = MartenAuditErrors.EncryptionFailed(Guid.NewGuid(), "2026-03", ex);
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void DecryptionFailed_WithoutException_ReturnsError()
    {
        var error = MartenAuditErrors.DecryptionFailed(Guid.NewGuid(), "2026-03");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void DecryptionFailed_WithException_AttachesException()
    {
        var ex = new InvalidOperationException("dec");
        var error = MartenAuditErrors.DecryptionFailed(Guid.NewGuid(), "2026-03", ex);
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void KeyNotFound_ReturnsError()
    {
        var error = MartenAuditErrors.KeyNotFound("2026-03");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void KeyDestructionFailed_WithoutException_ReturnsError()
    {
        var error = MartenAuditErrors.KeyDestructionFailed(DateTime.UtcNow);
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void KeyDestructionFailed_WithException_AttachesException()
    {
        var ex = new TimeoutException("timeout");
        var error = MartenAuditErrors.KeyDestructionFailed(DateTime.UtcNow, ex);
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void ProjectionFailed_WithoutException_ReturnsError()
    {
        var error = MartenAuditErrors.ProjectionFailed("AuditEntryProjection");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ProjectionFailed_WithException_AttachesException()
    {
        var ex = new InvalidOperationException("proj");
        var error = MartenAuditErrors.ProjectionFailed("P", ex);
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void QueryFailed_WithoutException_ReturnsError()
    {
        var error = MartenAuditErrors.QueryFailed("ByEntity");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void QueryFailed_WithException_AttachesException()
    {
        var ex = new InvalidOperationException("q");
        var error = MartenAuditErrors.QueryFailed("ByEntity", ex);
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void StoreUnavailable_WithoutException_ReturnsError()
    {
        var error = MartenAuditErrors.StoreUnavailable("RecordAsync");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void StoreUnavailable_WithException_AttachesException()
    {
        var ex = new InvalidOperationException("store");
        var error = MartenAuditErrors.StoreUnavailable("op", ex);
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void InvalidPeriod_WithValidString_ReturnsError()
    {
        var error = MartenAuditErrors.InvalidPeriod("bad");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void InvalidPeriod_WithNull_ReturnsError()
    {
        var error = MartenAuditErrors.InvalidPeriod(null);
        error.Message.ShouldContain("(null)");
    }

    [Fact]
    public void ShreddedEntry_ReturnsError()
    {
        var error = MartenAuditErrors.ShreddedEntry(Guid.NewGuid(), "2020-01");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void AllErrorCodes_StartWithAuditMarten()
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
}
