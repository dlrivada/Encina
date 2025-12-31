using Encina.Messaging.DeadLetter;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Testing.Fakes.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaFakes_RegistersAllFakes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaFakes();

        // Assert
        using (var provider = services.BuildServiceProvider())
        {
            provider.GetService<FakeEncina>().ShouldNotBeNull();
            provider.GetService<IEncina>().ShouldNotBeNull();
            provider.GetService<FakeOutboxStore>().ShouldNotBeNull();
            provider.GetService<IOutboxStore>().ShouldNotBeNull();
            provider.GetService<FakeInboxStore>().ShouldNotBeNull();
            provider.GetService<IInboxStore>().ShouldNotBeNull();
            provider.GetService<FakeSagaStore>().ShouldNotBeNull();
            provider.GetService<ISagaStore>().ShouldNotBeNull();
            provider.GetService<FakeScheduledMessageStore>().ShouldNotBeNull();
            provider.GetService<IScheduledMessageStore>().ShouldNotBeNull();
            provider.GetService<FakeDeadLetterStore>().ShouldNotBeNull();
            provider.GetService<IDeadLetterStore>().ShouldNotBeNull();
        }
    }

    [Fact]
    public void AddEncinaFakes_RegistersSameInstances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaFakes();

        // Act & Assert
        using (var provider = services.BuildServiceProvider())
        {
            var fakeEncina = provider.GetRequiredService<FakeEncina>();
            var encina = provider.GetRequiredService<IEncina>();

            // Same instance
            fakeEncina.ShouldBeSameAs(encina);
        }
    }

    [Fact]
    public void AddEncinaFakes_RegistersAsSingletons()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaFakes();

        // Act & Assert
        using (var provider = services.BuildServiceProvider())
        {
            var fake1 = provider.GetRequiredService<FakeEncina>();
            var fake2 = provider.GetRequiredService<FakeEncina>();

            fake1.ShouldBeSameAs(fake2);
        }
    }

    [Fact]
    public void AddFakeEncina_RegistersFakeEncinaOnly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFakeEncina();

        // Assert
        using (var provider = services.BuildServiceProvider())
        {
            provider.GetService<FakeEncina>().ShouldNotBeNull();
            provider.GetService<IEncina>().ShouldNotBeNull();
            provider.GetService<IOutboxStore>().ShouldBeNull();
        }
    }

    [Fact]
    public void ReplaceWithFakes_ReplacesExistingRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IEncina, FakeEncina>(); // Existing registration

        // Act
        services.ReplaceWithFakes();

        // Assert
        using (var provider = services.BuildServiceProvider())
        {
            var fakeEncina = provider.GetRequiredService<FakeEncina>();
            var encina = provider.GetRequiredService<IEncina>();

            // Should be the same instance (replaced, not duplicated)
            fakeEncina.ShouldBeSameAs(encina);
        }
    }

    [Fact]
    public void AddFakeOutboxStore_RegistersFakeOutboxStoreOnly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFakeOutboxStore();

        // Assert
        using (var provider = services.BuildServiceProvider())
        {
            provider.GetService<FakeOutboxStore>().ShouldNotBeNull();
            provider.GetService<IOutboxStore>().ShouldNotBeNull();
            provider.GetService<FakeInboxStore>().ShouldBeNull();
        }
    }
}
