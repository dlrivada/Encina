using Encina.Marten;

namespace Encina.UnitTests.Marten;

/// <summary>Unit tests for <see cref="EventReadScope"/>.</summary>
public sealed class EventReadScopeTests
{
    [Fact]
    public void IsDecryptionSuppressed_IsFalseByDefault()
    {
        EventReadScope.IsDecryptionSuppressed.ShouldBeFalse();
    }

    [Fact]
    public void SuppressDecryption_SuppressesUntilDisposed_AndRestoresTheOuterState()
    {
        using (EventReadScope.SuppressDecryption())
        {
            EventReadScope.IsDecryptionSuppressed.ShouldBeTrue();

            using (EventReadScope.SuppressDecryption())
            {
                EventReadScope.IsDecryptionSuppressed.ShouldBeTrue();
            }

            EventReadScope.IsDecryptionSuppressed.ShouldBeTrue();
        }

        EventReadScope.IsDecryptionSuppressed.ShouldBeFalse();
    }

    [Fact]
    public async Task SuppressDecryption_FlowsIntoAwaitedWork()
    {
        using var scope = EventReadScope.SuppressDecryption();

        var seen = await Task.Run(async () =>
        {
            await Task.Yield();
            return EventReadScope.IsDecryptionSuppressed;
        });

        seen.ShouldBeTrue();
    }
}
