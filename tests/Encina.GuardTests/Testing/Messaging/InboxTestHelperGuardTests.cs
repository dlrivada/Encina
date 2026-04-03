using Encina.Testing.Fakes.Stores;
using Encina.Testing.Messaging;
using Encina.Testing.Time;

namespace Encina.GuardTests.Testing.Messaging;

public class InboxTestHelperGuardTests : IDisposable
{
    private readonly InboxTestHelper _sut = new();

    public void Dispose() => _sut.Dispose();

    public class ConstructorGuards
    {
        [Fact]
        public void NullStore_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                new InboxTestHelper(null!, new FakeTimeProvider()));
        }

        [Fact]
        public void NullTimeProvider_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                new InboxTestHelper(new FakeInboxStore(), null!));
        }
    }

    public class GivenGuards : InboxTestHelperGuardTests
    {
        [Fact]
        public void GivenProcessedMessage_NullMessageId_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenProcessedMessage<TestResponse>(null!, new TestResponse()));
        }

        [Fact]
        public void GivenProcessedMessage_NullResponse_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenProcessedMessage<TestResponse>("msg-1", null!));
        }

        [Fact]
        public void GivenPendingMessage_NullMessageId_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenPendingMessage(null!));
        }

        [Fact]
        public void GivenFailedMessage_NullMessageId_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenFailedMessage(null!));
        }

        [Fact]
        public void GivenExpiredMessage_NullMessageId_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenExpiredMessage(null!));
        }
    }

    public class WhenGuards : InboxTestHelperGuardTests
    {
        [Fact]
        public void WhenMessageReceived_NullMessageId_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.WhenMessageReceived(null!));
        }

        [Fact]
        public void WhenMessageRegistered_NullMessageId_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.WhenMessageRegistered(null!));
        }

        [Fact]
        public void WhenMessageProcessed_NullMessageId_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.WhenMessageProcessed<TestResponse>(null!, new TestResponse()));
        }

        [Fact]
        public void WhenMessageProcessed_NullResponse_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.WhenMessageProcessed("msg-1", (TestResponse)null!));
        }

        [Fact]
        public void WhenMessageFailed_NullMessageId_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.WhenMessageFailed(null!));
        }

        [Fact]
        public void When_NullAction_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.When((Action<FakeInboxStore>)null!));
        }

        [Fact]
        public void WhenAsync_NullAction_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.WhenAsync(null!));
        }
    }

    public class ThenGuards : InboxTestHelperGuardTests
    {
        [Fact]
        public void ThenCachedResponseIs_NullPredicate_Throws()
        {
            _sut.GivenProcessedMessage("msg-1", new TestResponse())
                .WhenMessageReceived("msg-1");

            Should.Throw<ArgumentNullException>(() =>
                _sut.ThenCachedResponseIs<TestResponse>(null!));
        }
    }

    public class ThenBeforeWhenGuards : InboxTestHelperGuardTests
    {
        [Fact]
        public void ThenNoException_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenNoException());
        }

        [Fact]
        public void ThenThrows_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenThrows<InvalidOperationException>());
        }

        [Fact]
        public void ThenMessageWasAlreadyProcessed_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenMessageWasAlreadyProcessed("msg-1"));
        }

        [Fact]
        public void ThenMessageIsNew_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenMessageIsNew("msg-1"));
        }

        [Fact]
        public void ThenInboxIsEmpty_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenInboxIsEmpty());
        }

        [Fact]
        public void ThenInboxHasCount_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenInboxHasCount(0));
        }

        [Fact]
        public void ThenMessageWasProcessed_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenMessageWasProcessed("msg-1"));
        }

        [Fact]
        public void ThenMessageWasFailed_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenMessageWasFailed("msg-1"));
        }

        [Fact]
        public void GetCachedResponse_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.GetCachedResponse<TestResponse>("msg-1"));
        }
    }

    private sealed class TestResponse
    {
        public bool Success { get; set; } = true;
    }
}
