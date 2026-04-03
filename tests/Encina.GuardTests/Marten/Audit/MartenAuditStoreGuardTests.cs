using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Marten.Audit;

/// <summary>
/// Guard tests for <see cref="MartenAuditStore"/> covering constructor null checks
/// and method-level validation.
/// </summary>
public class MartenAuditStoreGuardTests
{
    private static readonly IDocumentSession Session = Substitute.For<IDocumentSession>();
    private static readonly ITemporalKeyProvider KeyProvider = Substitute.For<ITemporalKeyProvider>();
    private static readonly IOptions<MartenAuditOptions> AuditOptions = Microsoft.Extensions.Options.Options.Create(new MartenAuditOptions());
    private static readonly ILogger<MartenAuditStore> Logger = NullLogger<MartenAuditStore>.Instance;

    private static AuditEventEncryptor CreateEncryptor()
        => new(KeyProvider, AuditOptions, NullLogger<AuditEventEncryptor>.Instance);

    #region Constructor Guards

    [Fact]
    public void Constructor_NullSession_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenAuditStore(null!, CreateEncryptor(), KeyProvider, AuditOptions, Logger));

    [Fact]
    public void Constructor_NullEncryptor_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenAuditStore(Session, null!, KeyProvider, AuditOptions, Logger));

    [Fact]
    public void Constructor_NullKeyProvider_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenAuditStore(Session, CreateEncryptor(), null!, AuditOptions, Logger));

    [Fact]
    public void Constructor_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenAuditStore(Session, CreateEncryptor(), KeyProvider, null!, Logger));

    [Fact]
    public void Constructor_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenAuditStore(Session, CreateEncryptor(), KeyProvider, AuditOptions, null!));

    #endregion

    #region RecordAsync Guards

    [Fact]
    public async Task RecordAsync_NullEntry_Throws()
    {
        var store = new MartenAuditStore(Session, CreateEncryptor(), KeyProvider, AuditOptions, Logger);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.RecordAsync(null!));
    }

    #endregion
}
