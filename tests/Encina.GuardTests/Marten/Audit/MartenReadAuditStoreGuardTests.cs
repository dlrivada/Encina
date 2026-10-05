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
    private static readonly IOptions<MartenOperationAuditOptions> OperationAuditOptions = Microsoft.Extensions.Options.Options.Create(new MartenOperationAuditOptions());
    private static readonly ILogger<MartenReadAuditStore> Logger = NullLogger<MartenReadAuditStore>.Instance;

    private static AuditEventEncryptor CreateEncryptor()
        => new(KeyProvider, OperationAuditOptions, NullLogger<AuditEventEncryptor>.Instance);

    #region Constructor Guards

    [Fact]
    public void Constructor_NullSession_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenReadAuditStore(null!, CreateEncryptor(), KeyProvider, OperationAuditOptions, Logger));

    [Fact]
    public void Constructor_NullEncryptor_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenReadAuditStore(Session, null!, KeyProvider, OperationAuditOptions, Logger));

    [Fact]
    public void Constructor_NullKeyProvider_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenReadAuditStore(Session, CreateEncryptor(), null!, OperationAuditOptions, Logger));

    [Fact]
    public void Constructor_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenReadAuditStore(Session, CreateEncryptor(), KeyProvider, null!, Logger));

    [Fact]
    public void Constructor_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenReadAuditStore(Session, CreateEncryptor(), KeyProvider, OperationAuditOptions, null!));

    #endregion

    #region LogReadAsync Guards

    [Fact]
    public async Task LogReadAsync_NullEntry_Throws()
    {
        var store = new MartenReadAuditStore(Session, CreateEncryptor(), KeyProvider, OperationAuditOptions, Logger);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.LogReadAsync(null!));
    }

    #endregion
}
