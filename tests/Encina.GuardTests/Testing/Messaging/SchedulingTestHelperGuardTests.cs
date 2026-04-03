using Encina.Testing.Fakes.Stores;
using Encina.Testing.Messaging;
using Encina.Testing.Time;

namespace Encina.GuardTests.Testing.Messaging;

public class SchedulingTestHelperGuardTests : IDisposable
{
    private readonly SchedulingTestHelper _sut = new();

    public void Dispose() => _sut.Dispose();

    public class ConstructorGuards
    {
        [Fact]
        public void NullStore_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SchedulingTestHelper(null!, new FakeTimeProvider()));
        }

        [Fact]
        public void NullTimeProvider_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SchedulingTestHelper(new FakeScheduledMessageStore(), null!));
        }
    }

    public class GivenScheduledMessageGuards : SchedulingTestHelperGuardTests
    {
        [Fact]
        public void NullRequest_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenScheduledMessage<TestCommand>(null!, TimeSpan.FromMinutes(5)));
        }
    }

    public class GivenRecurringMessageGuards : SchedulingTestHelperGuardTests
    {
        [Fact]
        public void NullRequest_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenRecurringMessage<TestCommand>(null!, "0 * * * *", TimeSpan.FromMinutes(5)));
        }

        [Fact]
        public void NullCronExpression_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenRecurringMessage(new TestCommand(), null!, TimeSpan.FromMinutes(5)));
        }
    }

    public class GivenDueMessageGuards : SchedulingTestHelperGuardTests
    {
        [Fact]
        public void NullRequest_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenDueMessage<TestCommand>(null!));
        }
    }

    public class GivenProcessedMessageGuards : SchedulingTestHelperGuardTests
    {
        [Fact]
        public void NullRequest_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenProcessedMessage<TestCommand>(null!));
        }
    }

    public class GivenFailedMessageGuards : SchedulingTestHelperGuardTests
    {
        [Fact]
        public void NullRequest_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenFailedMessage<TestCommand>(null!));
        }
    }

    public class GivenCancelledMessageGuards : SchedulingTestHelperGuardTests
    {
        [Fact]
        public void NullRequest_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenCancelledMessage<TestCommand>(null!));
        }
    }

    public class WhenMessageScheduledGuards : SchedulingTestHelperGuardTests
    {
        [Fact]
        public void NullRequest_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.WhenMessageScheduled<TestCommand>(null!, TimeSpan.FromMinutes(5)));
        }
    }

    public class WhenRecurringMessageScheduledGuards : SchedulingTestHelperGuardTests
    {
        [Fact]
        public void NullRequest_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.WhenRecurringMessageScheduled<TestCommand>(null!, "0 * * * *", TimeSpan.FromMinutes(5)));
        }

        [Fact]
        public void NullCronExpression_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.WhenRecurringMessageScheduled(new TestCommand(), null!, TimeSpan.FromMinutes(5)));
        }
    }

    public class WhenCustomActionGuards : SchedulingTestHelperGuardTests
    {
        [Fact]
        public void NullAction_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.When((Action<FakeScheduledMessageStore>)null!));
        }

        [Fact]
        public void NullAsyncAction_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.WhenAsync(null!));
        }
    }

    public class ThenBeforeWhenGuards : SchedulingTestHelperGuardTests
    {
        [Fact]
        public void ThenNoException_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenNoException());
        }

        [Fact]
        public void ThenMessageWasScheduled_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenMessageWasScheduled<TestCommand>());
        }

        [Fact]
        public void ThenMessageIsDue_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenMessageIsDue<TestCommand>());
        }

        [Fact]
        public void ThenNoScheduledMessages_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenNoScheduledMessages());
        }

        [Fact]
        public void ThenScheduledMessageCount_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenScheduledMessageCount(0));
        }

        [Fact]
        public void ThenThrows_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenThrows<InvalidOperationException>());
        }

        [Fact]
        public void GetScheduledMessage_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.GetScheduledMessage<TestCommand>());
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
        public void ThenMessageWasCancelled_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenMessageWasCancelled(Guid.NewGuid()));
        }

        [Fact]
        public void ThenMessageWasRescheduled_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenMessageWasRescheduled(Guid.NewGuid()));
        }

        [Fact]
        public void ThenMessageIsRecurring_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenMessageIsRecurring(Guid.NewGuid()));
        }

        [Fact]
        public void ThenMessageHasCron_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenMessageHasCron(Guid.NewGuid(), "0 * * * *"));
        }

        [Fact]
        public void ThenMessageIsNotDue_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenMessageIsNotDue(Guid.NewGuid()));
        }
    }

    public class AdvanceTimeGuards : SchedulingTestHelperGuardTests
    {
        [Fact]
        public void AdvanceTimeUntilDue_NonExistentMessage_ThrowsKeyNotFound()
        {
            Should.Throw<KeyNotFoundException>(() =>
                _sut.AdvanceTimeUntilDue(Guid.NewGuid()));
        }
    }

    private sealed class TestCommand
    {
        public string Name { get; set; } = "Test";
    }
}
