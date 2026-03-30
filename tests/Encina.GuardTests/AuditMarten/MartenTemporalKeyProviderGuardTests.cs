using Encina.Audit.Marten.Crypto;
using Marten;

namespace Encina.GuardTests.AuditMarten;

public class MartenTemporalKeyProviderGuardTests
{
    [Fact]
    public void Constructor_NullSession_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new MartenTemporalKeyProvider(
                null!,
                TimeProvider.System,
                NullLogger<MartenTemporalKeyProvider>.Instance));
    }

    [Fact]
    public void Constructor_NullTimeProvider_Throws()
    {
        var session = Substitute.For<IDocumentSession>();

        Should.Throw<ArgumentNullException>(() =>
            new MartenTemporalKeyProvider(
                session,
                null!,
                NullLogger<MartenTemporalKeyProvider>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var session = Substitute.For<IDocumentSession>();

        Should.Throw<ArgumentNullException>(() =>
            new MartenTemporalKeyProvider(
                session,
                TimeProvider.System,
                null!));
    }

    [Fact]
    public async Task GetOrCreateKeyAsync_NullPeriod_Throws()
    {
        var session = Substitute.For<IDocumentSession>();
        var sut = new MartenTemporalKeyProvider(
            session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);

        await Should.ThrowAsync<ArgumentException>(async () =>
            await sut.GetOrCreateKeyAsync(null!));
    }

    [Fact]
    public async Task GetOrCreateKeyAsync_EmptyPeriod_Throws()
    {
        var session = Substitute.For<IDocumentSession>();
        var sut = new MartenTemporalKeyProvider(
            session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);

        await Should.ThrowAsync<ArgumentException>(async () =>
            await sut.GetOrCreateKeyAsync(""));
    }

    [Fact]
    public async Task GetOrCreateKeyAsync_WhitespacePeriod_Throws()
    {
        var session = Substitute.For<IDocumentSession>();
        var sut = new MartenTemporalKeyProvider(
            session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);

        await Should.ThrowAsync<ArgumentException>(async () =>
            await sut.GetOrCreateKeyAsync("  "));
    }
}
