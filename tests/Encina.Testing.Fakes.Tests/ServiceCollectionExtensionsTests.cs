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
            provider.GetService<FakeEncina>().Should().NotBeNull();
            provider.GetService<IEncina>().Should().NotBeNull();
            provider.GetService<FakeOutboxStore>().Should().NotBeNull();
            provider.GetService<IOutboxStore>().Should().NotBeNull();
            provider.GetService<FakeInboxStore>().Should().NotBeNull();
            provider.GetService<IInboxStore>().Should().NotBeNull();
            provider.GetService<FakeSagaStore>().Should().NotBeNull();
            provider.GetService<ISagaStore>().Should().NotBeNull();
            provider.GetService<FakeScheduledMessageStore>().Should().NotBeNull();
            provider.GetService<IScheduledMessageStore>().Should().NotBeNull();
            provider.GetService<FakeDeadLetterStore>().Should().NotBeNull();
            provider.GetService<IDeadLetterStore>().Should().NotBeNull();
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
            fakeEncina.Should().BeSameAs(encina);
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

            fake1.Should().BeSameAs(fake2);
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
            provider.GetService<FakeEncina>().Should().NotBeNull();
            provider.GetService<IEncina>().Should().NotBeNull();
            provider.GetService<IOutboxStore>().Should().BeNull();
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
            fakeEncina.Should().BeSameAs(encina);
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
            provider.GetService<FakeOutboxStore>().Should().NotBeNull();
            provider.GetService<IOutboxStore>().Should().NotBeNull();
            provider.GetService<FakeInboxStore>().Should().BeNull();
        }
    }
}
