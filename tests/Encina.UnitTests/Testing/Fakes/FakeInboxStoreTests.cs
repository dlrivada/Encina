using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;

namespace Encina.UnitTests.Testing.Fakes;

/// <summary>
/// Pins the retry contract of <see cref="FakeInboxStore"/> (#2084): each failed attempt adds exactly one retry.
/// </summary>
public sealed class FakeInboxStoreTests
{
    [Fact]
    public async Task MarkAsFailedAsync_EachCallIncrementsRetryCountByOne()
    {
        var store = new FakeInboxStore();
        await store.AddAsync(new FakeInboxMessage { MessageId = "m1", RequestType = "Req" });

        (await store.MarkAsFailedAsync("m1", "first", null)).IsRight.ShouldBeTrue();
        (await store.MarkAsFailedAsync("m1", "second", null)).IsRight.ShouldBeTrue();

        store.GetMessages().Single().RetryCount.ShouldBe(2);
    }
}
