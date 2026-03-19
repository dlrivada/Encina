using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Marten.Audit;

/// <summary>
/// Guard clause tests for <see cref="AuditEventEncryptor"/>.
/// Verifies null checks on constructor parameters and public methods.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "Marten")]
public sealed class AuditEventEncryptorGuardTests
{
    [Fact]
    public void Constructor_NullKeyProvider_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new AuditEventEncryptor(
                null!,
                Options.Create(new MartenAuditOptions()),
                Substitute.For<ILogger<AuditEventEncryptor>>()));
        ex.ParamName.ShouldBe("keyProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new AuditEventEncryptor(
                Substitute.For<ITemporalKeyProvider>(),
                null!,
                Substitute.For<ILogger<AuditEventEncryptor>>()));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new AuditEventEncryptor(
                Substitute.For<ITemporalKeyProvider>(),
                Options.Create(new MartenAuditOptions()),
                null!));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task EncryptAuditEntryAsync_NullEntry_ThrowsArgumentNullException()
    {
        var sut = new AuditEventEncryptor(
            Substitute.For<ITemporalKeyProvider>(),
            Options.Create(new MartenAuditOptions()),
            Substitute.For<ILogger<AuditEventEncryptor>>());

        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            sut.EncryptAuditEntryAsync(null!).AsTask());
        ex.ParamName.ShouldBe("entry");
    }

    [Fact]
    public async Task EncryptReadAuditEntryAsync_NullEntry_ThrowsArgumentNullException()
    {
        var sut = new AuditEventEncryptor(
            Substitute.For<ITemporalKeyProvider>(),
            Options.Create(new MartenAuditOptions()),
            Substitute.For<ILogger<AuditEventEncryptor>>());

        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            sut.EncryptReadAuditEntryAsync(null!).AsTask());
        ex.ParamName.ShouldBe("entry");
    }
}
