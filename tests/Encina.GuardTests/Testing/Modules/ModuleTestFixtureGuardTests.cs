using Encina.Modules;
using Encina.Testing.Modules;

namespace Encina.GuardTests.Testing.Modules;

public class ModuleTestFixtureGuardTests
{
    public class WithMockedModuleGuards
    {
        [Fact]
        public void NullConfigureAction_Throws()
        {
            var fixture = new ModuleTestFixture<TestModule>();

            Should.Throw<ArgumentNullException>(() =>
                fixture.WithMockedModule((Action<MockModuleApi<ITestModuleApi>>)null!));
        }

        [Fact]
        public void NullImplementation_Throws()
        {
            var fixture = new ModuleTestFixture<TestModule>();

            Should.Throw<ArgumentNullException>(() =>
                fixture.WithMockedModule<ITestModuleApi>((ITestModuleApi)null!));
        }
    }

    public class WithFakeModuleGuards
    {
        [Fact]
        public void NullInstance_Throws()
        {
            var fixture = new ModuleTestFixture<TestModule>();

            Should.Throw<ArgumentNullException>(() =>
                fixture.WithFakeModule<ITestModuleApi, FakeTestModuleApi>(null!));
        }
    }

    public class WithServiceGuards
    {
        [Fact]
        public void NullImplementation_Throws()
        {
            var fixture = new ModuleTestFixture<TestModule>();

            Should.Throw<ArgumentNullException>(() =>
                fixture.WithService<ITestService>(null!));
        }
    }

    public class ConfigureGuards
    {
        [Fact]
        public void NullConfigureAction_Throws()
        {
            var fixture = new ModuleTestFixture<TestModule>();

            Should.Throw<ArgumentNullException>(() =>
                fixture.Configure(null!));
        }
    }

    public class ConfigureServicesGuards
    {
        [Fact]
        public void NullAction_Throws()
        {
            var fixture = new ModuleTestFixture<TestModule>();

            Should.Throw<ArgumentNullException>(() =>
                fixture.ConfigureServices(null!));
        }
    }

    public class SendAsyncGuards
    {
        [Fact]
        public async Task NullRequest_Throws()
        {
            var fixture = new ModuleTestFixture<TestModule>();

            await Should.ThrowAsync<ArgumentNullException>(async () =>
                await fixture.SendAsync<string>(null!));
        }
    }

    public class PublishAsyncGuards
    {
        [Fact]
        public async Task NullNotification_Throws()
        {
            var fixture = new ModuleTestFixture<TestModule>();

            await Should.ThrowAsync<ArgumentNullException>(async () =>
                await fixture.PublishAsync(null!));
        }
    }

    public class StoreAccessGuards
    {
        [Fact]
        public void Outbox_NotConfigured_Throws()
        {
            var fixture = new ModuleTestFixture<TestModule>();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = fixture.Outbox;
            });
        }

        [Fact]
        public void Inbox_NotConfigured_Throws()
        {
            var fixture = new ModuleTestFixture<TestModule>();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = fixture.Inbox;
            });
        }

        [Fact]
        public void SagaStore_NotConfigured_Throws()
        {
            var fixture = new ModuleTestFixture<TestModule>();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = fixture.SagaStore;
            });
        }

        [Fact]
        public void ScheduledMessageStore_NotConfigured_Throws()
        {
            var fixture = new ModuleTestFixture<TestModule>();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = fixture.ScheduledMessageStore;
            });
        }

        [Fact]
        public void DeadLetterStore_NotConfigured_Throws()
        {
            var fixture = new ModuleTestFixture<TestModule>();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = fixture.DeadLetterStore;
            });
        }

        [Fact]
        public void TimeProvider_NotConfigured_Throws()
        {
            var fixture = new ModuleTestFixture<TestModule>();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = fixture.TimeProvider;
            });
        }

        [Fact]
        public void ServiceProvider_NotBuilt_Throws()
        {
            var fixture = new ModuleTestFixture<TestModule>();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = fixture.ServiceProvider;
            });
        }
    }

    // Test types
    public class TestModule : IModule
    {
        public string Name => "TestModule";
        public void ConfigureServices(IServiceCollection services) { }
    }

    public interface ITestModuleApi
    {
        Task<string> GetDataAsync();
    }

    public class FakeTestModuleApi : ITestModuleApi
    {
        public Task<string> GetDataAsync() => Task.FromResult("fake");
    }

    private interface ITestService
    {
        void DoWork();
    }
}
