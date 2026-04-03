using Encina.Testing;

namespace Encina.GuardTests.Testing;

public class EncinaTestFixtureGuardTests
{
    public class WithServiceGuards
    {
        [Fact]
        public void NullImplementation_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<ArgumentNullException>(() =>
                fixture.WithService<ITestService>(null!));
        }
    }

    public class ConfigureGuards
    {
        [Fact]
        public void NullAction_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<ArgumentNullException>(() =>
                fixture.Configure(null!));
        }
    }

    public class ConfigureServicesGuards
    {
        [Fact]
        public void NullAction_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<ArgumentNullException>(() =>
                fixture.ConfigureServices(null!));
        }
    }

    public class SendAsyncGuards
    {
        [Fact]
        public async Task NullRequest_Throws()
        {
            var fixture = new EncinaTestFixture();

            await Should.ThrowAsync<ArgumentNullException>(async () =>
                await fixture.SendAsync<string>(null!));
        }
    }

    public class PublishAsyncGuards
    {
        [Fact]
        public async Task NullNotification_Throws()
        {
            var fixture = new EncinaTestFixture();

            await Should.ThrowAsync<ArgumentNullException>(async () =>
                await fixture.PublishAsync(null!));
        }
    }

    public class StoreAccessGuards
    {
        [Fact]
        public void Outbox_NotConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = fixture.Outbox;
            });
        }

        [Fact]
        public void Inbox_NotConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = fixture.Inbox;
            });
        }

        [Fact]
        public void SagaStore_NotConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = fixture.SagaStore;
            });
        }

        [Fact]
        public void ScheduledMessageStore_NotConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = fixture.ScheduledMessageStore;
            });
        }

        [Fact]
        public void DeadLetterStore_NotConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = fixture.DeadLetterStore;
            });
        }

        [Fact]
        public void TimeProvider_NotConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = fixture.TimeProvider;
            });
        }

        [Fact]
        public void ServiceProvider_NotBuilt_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = fixture.ServiceProvider;
            });
        }
    }

    public class TimeControlGuards
    {
        [Fact]
        public void AdvanceTimeBy_NoTimeProvider_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<InvalidOperationException>(() =>
                fixture.AdvanceTimeBy(TimeSpan.FromMinutes(1)));
        }

        [Fact]
        public void AdvanceTimeByMinutes_NoTimeProvider_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<InvalidOperationException>(() =>
                fixture.AdvanceTimeByMinutes(5));
        }

        [Fact]
        public void AdvanceTimeByHours_NoTimeProvider_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<InvalidOperationException>(() =>
                fixture.AdvanceTimeByHours(1));
        }

        [Fact]
        public void AdvanceTimeByDays_NoTimeProvider_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<InvalidOperationException>(() =>
                fixture.AdvanceTimeByDays(1));
        }

        [Fact]
        public void SetTimeTo_NoTimeProvider_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<InvalidOperationException>(() =>
                fixture.SetTimeTo(DateTimeOffset.UtcNow));
        }

        [Fact]
        public void GetCurrentTime_NoTimeProvider_Throws()
        {
            var fixture = new EncinaTestFixture();

            Should.Throw<InvalidOperationException>(() =>
                fixture.GetCurrentTime());
        }
    }

    private interface ITestService
    {
        void DoWork();
    }
}
