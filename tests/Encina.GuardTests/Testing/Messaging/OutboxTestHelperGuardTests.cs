using Encina.Testing.Fakes.Stores;
using Encina.Testing.Messaging;
using Encina.Testing.Time;

namespace Encina.GuardTests.Testing.Messaging;

public class OutboxTestHelperGuardTests : IDisposable
{
    private readonly OutboxTestHelper _sut = new();

    public void Dispose() => _sut.Dispose();

    public class ConstructorGuards
    {
        [Fact]
        public void NullStore_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                new OutboxTestHelper(null!, new FakeTimeProvider()));
        }

        [Fact]
        public void NullTimeProvider_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                new OutboxTestHelper(new FakeOutboxStore(), null!));
        }
    }

    public class GivenGuards : OutboxTestHelperGuardTests
    {
        [Fact]
        public void GivenMessages_NullArray_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenMessages(null!));
        }

        [Fact]
        public void GivenPendingMessage_NullNotification_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenPendingMessage<TestNotification>(null!));
        }

        [Fact]
        public void GivenProcessedMessage_NullNotification_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenProcessedMessage<TestNotification>(null!));
        }

        [Fact]
        public void GivenFailedMessage_NullNotification_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenFailedMessage<TestNotification>(null!));
        }
    }

    public class WhenGuards : OutboxTestHelperGuardTests
    {
        [Fact]
        public void WhenMessageAdded_NullNotification_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.WhenMessageAdded<TestNotification>(null!));
        }

        [Fact]
        public void When_NullAction_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.When((Action<FakeOutboxStore>)null!));
        }

        [Fact]
        public void WhenAsync_NullAction_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.WhenAsync(null!));
        }
    }

    public class ThenGuards : OutboxTestHelperGuardTests
    {
        [Fact]
        public void ThenOutboxContains_NullPredicate_Throws()
        {
            _sut.GivenEmptyOutbox()
                .WhenMessageAdded(new TestNotification());

            Should.Throw<ArgumentNullException>(() =>
                _sut.ThenOutboxContains<TestNotification>(null!));
        }
    }

    public class ThenBeforeWhenGuards : OutboxTestHelperGuardTests
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
        public void ThenOutboxContains_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenOutboxContains<TestNotification>());
        }

        [Fact]
        public void ThenOutboxIsEmpty_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenOutboxIsEmpty());
        }

        [Fact]
        public void ThenOutboxHasCount_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenOutboxHasCount(0));
        }

        [Fact]
        public void ThenMessageWasProcessed_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenMessageWasProcessed(Guid.NewGuid()));
        }

        [Fact]
        public void ThenMessageWasFailed_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenMessageWasFailed(Guid.NewGuid()));
        }

        [Fact]
        public void GetMessage_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.GetMessage<TestNotification>());
        }

        [Fact]
        public void GetMessages_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.GetMessages<TestNotification>());
        }
    }

    private sealed class TestNotification
    {
        public string Name { get; set; } = "Test";
    }
}
