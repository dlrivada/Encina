using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.AuditMarten;

public class MartenReadAuditStoreGuardTests
{
    [Fact]
    public void Constructor_NullSession_Throws()
    {
        var keyProvider = new InMemoryTemporalKeyProvider(TimeProvider.System,
            NullLogger<InMemoryTemporalKeyProvider>.Instance);
        var encryptor = new AuditEventEncryptor(keyProvider,
            Options.Create(new MartenAuditOptions()),
            NullLogger<AuditEventEncryptor>.Instance);

        Should.Throw<ArgumentNullException>(() =>
            new MartenReadAuditStore(null!, encryptor, keyProvider,
                Options.Create(new MartenAuditOptions()),
                NullLogger<MartenReadAuditStore>.Instance));
    }

    [Fact]
    public void Constructor_NullEncryptor_Throws()
    {
        var session = Substitute.For<IDocumentSession>();
        var keyProvider = new InMemoryTemporalKeyProvider(TimeProvider.System,
            NullLogger<InMemoryTemporalKeyProvider>.Instance);

        Should.Throw<ArgumentNullException>(() =>
            new MartenReadAuditStore(session, null!, keyProvider,
                Options.Create(new MartenAuditOptions()),
                NullLogger<MartenReadAuditStore>.Instance));
    }

    [Fact]
    public void Constructor_NullKeyProvider_Throws()
    {
        var session = Substitute.For<IDocumentSession>();
        var keyProvider = new InMemoryTemporalKeyProvider(TimeProvider.System,
            NullLogger<InMemoryTemporalKeyProvider>.Instance);
        var encryptor = new AuditEventEncryptor(keyProvider,
            Options.Create(new MartenAuditOptions()),
            NullLogger<AuditEventEncryptor>.Instance);

        Should.Throw<ArgumentNullException>(() =>
            new MartenReadAuditStore(session, encryptor, null!,
                Options.Create(new MartenAuditOptions()),
                NullLogger<MartenReadAuditStore>.Instance));
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        var session = Substitute.For<IDocumentSession>();
        var keyProvider = new InMemoryTemporalKeyProvider(TimeProvider.System,
            NullLogger<InMemoryTemporalKeyProvider>.Instance);
        var encryptor = new AuditEventEncryptor(keyProvider,
            Options.Create(new MartenAuditOptions()),
            NullLogger<AuditEventEncryptor>.Instance);

        Should.Throw<ArgumentNullException>(() =>
            new MartenReadAuditStore(session, encryptor, keyProvider,
                null!, NullLogger<MartenReadAuditStore>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var session = Substitute.For<IDocumentSession>();
        var keyProvider = new InMemoryTemporalKeyProvider(TimeProvider.System,
            NullLogger<InMemoryTemporalKeyProvider>.Instance);
        var encryptor = new AuditEventEncryptor(keyProvider,
            Options.Create(new MartenAuditOptions()),
            NullLogger<AuditEventEncryptor>.Instance);

        Should.Throw<ArgumentNullException>(() =>
            new MartenReadAuditStore(session, encryptor, keyProvider,
                Options.Create(new MartenAuditOptions()), null!));
    }
}
