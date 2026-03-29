using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.AuditMarten;

public class AuditEventEncryptorGuardTests
{
    private static readonly ITemporalKeyProvider KeyProvider = new InMemoryTemporalKeyProvider(
        TimeProvider.System, NullLogger<InMemoryTemporalKeyProvider>.Instance);

    private static readonly MartenAuditOptions Options = new();

    [Fact]
    public void Constructor_NullKeyProvider_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AuditEventEncryptor(null!, Microsoft.Extensions.Options.Options.Create(Options),
                NullLogger<AuditEventEncryptor>.Instance));
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AuditEventEncryptor(KeyProvider, null!,
                NullLogger<AuditEventEncryptor>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AuditEventEncryptor(KeyProvider, Microsoft.Extensions.Options.Options.Create(Options),
                null!));
    }

    [Fact]
    public async Task EncryptAuditEntryAsync_NullEntry_Throws()
    {
        var encryptor = new AuditEventEncryptor(KeyProvider,
            Microsoft.Extensions.Options.Options.Create(Options),
            NullLogger<AuditEventEncryptor>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await encryptor.EncryptAuditEntryAsync(null!));
    }

    [Fact]
    public async Task EncryptReadAuditEntryAsync_NullEntry_Throws()
    {
        var encryptor = new AuditEventEncryptor(KeyProvider,
            Microsoft.Extensions.Options.Options.Create(Options),
            NullLogger<AuditEventEncryptor>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await encryptor.EncryptReadAuditEntryAsync(null!));
    }
}
