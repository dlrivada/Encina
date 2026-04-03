using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Marten.Audit;

/// <summary>
/// Guard tests for <see cref="MartenReadAuditStore"/> covering constructor and method guards.
/// </summary>
public class MartenReadAuditStoreGuardTests
{
    private static readonly IDocumentSession Session = Substitute.For<IDocumentSession>();
    private static readonly ITemporalKeyProvider KeyProvider = Substitute.For<ITemporalKeyProvider>();
    private static readonly IOptions<MartenAuditOptions> AuditOptions = Microsoft.Extensions.Options.Options.Create(new MartenAuditOptions());
    private static readonly ILogger<MartenReadAuditStore> Logger = NullLogger<MartenReadAuditStore>.Instance;

    private static AuditEventEncryptor CreateEncryptor()
        => new(KeyProvider, AuditOptions, NullLogger<AuditEventEncryptor>.Instance);

    #region Constructor Guards

    [Fact]
    public void Constructor_NullSession_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenReadAuditStore(null!, CreateEncryptor(), KeyProvider, AuditOptions, Logger));

    [Fact]
    public void Constructor_NullEncryptor_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenReadAuditStore(Session, null!, KeyProvider, AuditOptions, Logger));

    [Fact]
    public void Constructor_NullKeyProvider_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenReadAuditStore(Session, CreateEncryptor(), null!, AuditOptions, Logger));

    [Fact]
    public void Constructor_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenReadAuditStore(Session, CreateEncryptor(), KeyProvider, null!, Logger));

    [Fact]
    public void Constructor_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenReadAuditStore(Session, CreateEncryptor(), KeyProvider, AuditOptions, null!));

    #endregion

    #region LogReadAsync Guards

    [Fact]
    public async Task LogReadAsync_NullEntry_Throws()
    {
        var store = new MartenReadAuditStore(Session, CreateEncryptor(), KeyProvider, AuditOptions, Logger);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.LogReadAsync(null!));
    }

    #endregion
}
