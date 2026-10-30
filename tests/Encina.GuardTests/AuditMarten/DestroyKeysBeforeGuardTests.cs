using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Events;

using Marten;

namespace Encina.GuardTests.AuditMarten;

/// <summary>
/// Guard tests for <c>ITemporalKeyProvider.DestroyKeysBeforeAsync</c> (issue #2770): an invalid erasure reference
/// is a business failure (<c>Left</c>), never an exception, and nothing is destroyed.
/// </summary>
public sealed class DestroyKeysBeforeGuardTests
{
    private static readonly ErasureReference Invalid = new("", default, "");

    [Fact]
    public async Task Marten_InvalidReference_ReturnsLeftWithoutQuerying()
    {
        var session = Substitute.For<IDocumentSession>();
        var sut = new MartenTemporalKeyProvider(
            session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);

        var result = await sut.DestroyKeysBeforeAsync(DateTime.UtcNow, TemporalKeyGranularity.Monthly, Invalid);

        result.IsLeft.ShouldBeTrue();
        session.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task InMemory_InvalidReference_ReturnsLeft()
    {
        var sut = new InMemoryTemporalKeyProvider(TimeProvider.System, NullLogger<InMemoryTemporalKeyProvider>.Instance);

        var result = await sut.DestroyKeysBeforeAsync(DateTime.UtcNow, TemporalKeyGranularity.Monthly, Invalid);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task InMemory_NullReference_IsAccepted()
    {
        var sut = new InMemoryTemporalKeyProvider(TimeProvider.System, NullLogger<InMemoryTemporalKeyProvider>.Instance);

        var result = await sut.DestroyKeysBeforeAsync(DateTime.UtcNow, TemporalKeyGranularity.Monthly, reference: null);

        result.IsRight.ShouldBeTrue();
    }

    [Theory]
    [InlineData(EncryptedFieldDecryptionFailureReason.EnvelopeMalformed)]
    [InlineData(EncryptedFieldDecryptionFailureReason.KeyDestroyedWithoutErasureRecord)]
    public void DecryptionException_FixedMessageCarriesOnlyTheCode(EncryptedFieldDecryptionFailureReason reason)
    {
        var ex = new EncryptedFieldDecryptionException(reason);

        ex.Reason.ShouldBe(reason);
        ex.Message.ShouldContain(EncryptedFieldDecryptionException.Code);
        ex.InnerException.ShouldBeNull();
    }
}
