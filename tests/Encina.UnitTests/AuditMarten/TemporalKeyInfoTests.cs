using Encina.Audit.Marten.Crypto;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="TemporalKeyInfo"/>.
/// </summary>
public sealed class TemporalKeyInfoTests
{
    [Fact]
    public void KeyId_ShouldCombinePeriodAndVersion()
    {
        var info = new TemporalKeyInfo
        {
            Period = "2026-03",
            KeyMaterial = new byte[32],
            Version = 2,
            Status = TemporalKeyStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        info.KeyId.ShouldBe("temporal:2026-03:v2");
    }

    [Fact]
    public void KeyId_WithVersion1_ShouldFormatCorrectly()
    {
        var info = new TemporalKeyInfo
        {
            Period = "2026-Q1",
            KeyMaterial = new byte[32],
            Version = 1,
            Status = TemporalKeyStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        info.KeyId.ShouldBe("temporal:2026-Q1:v1");
    }

    [Fact]
    public void DestroyedAtUtc_DefaultsToNull()
    {
        var info = new TemporalKeyInfo
        {
            Period = "2026",
            KeyMaterial = new byte[32],
            Version = 1,
            Status = TemporalKeyStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        info.DestroyedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Status_CanBeDestroyed()
    {
        var info = new TemporalKeyInfo
        {
            Period = "2025-06",
            KeyMaterial = new byte[32],
            Version = 1,
            Status = TemporalKeyStatus.Destroyed,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            DestroyedAtUtc = DateTimeOffset.UtcNow
        };

        info.Status.ShouldBe(TemporalKeyStatus.Destroyed);
        info.DestroyedAtUtc.ShouldNotBeNull();
    }
}
