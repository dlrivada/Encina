using Aspire.Hosting.Testing;
using Encina.Aspire.Testing;
using Encina.Testing.Fakes;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Aspire.Testing;

/// <summary>
/// Unit tests for <see cref="DistributedApplicationTestingBuilderExtensions"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests validate the null-check behavior of the extension method.
/// Full integration testing of WithEncinaTestSupport requires a real
/// IDistributedApplicationTestingBuilder which is only available through
/// DistributedApplicationTestingBuilder.CreateAsync() and requires an actual AppHost project.
/// </para>
/// <para>
/// The core registration logic is tested through EncinaFakesServiceCollectionExtensions tests,
/// as WithEncinaTestSupport delegates to AddEncinaFakes() for service registration.
/// </para>
/// </remarks>
public sealed class DistributedApplicationTestingBuilderExtensionsTests
{
    [Fact]
    public void WithEncinaTestSupport_NullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IDistributedApplicationTestingBuilder builder = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.WithEncinaTestSupport());
    }

    [Fact]
    public void WithEncinaTestSupport_NullBuilderWithConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        IDistributedApplicationTestingBuilder builder = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.WithEncinaTestSupport(opts =>
        {
            opts.ClearOutboxBeforeTest = false;
        }));
    }

    /// <summary>
    /// Tests that AddEncinaFakes correctly registers all required fake stores.
    /// This tests the core functionality that WithEncinaTestSupport delegates to.
    /// </summary>
    [Fact]
    public void AddEncinaFakes_RegistersAllFakeStores()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaFakes();

        // Assert
        var provider = services.BuildServiceProvider();
        provider.GetService<FakeOutboxStore>().ShouldNotBeNull();
        provider.GetService<FakeInboxStore>().ShouldNotBeNull();
        provider.GetService<FakeSagaStore>().ShouldNotBeNull();
        provider.GetService<FakeScheduledMessageStore>().ShouldNotBeNull();
        provider.GetService<FakeDeadLetterStore>().ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that EncinaTestSupportOptions has correct default values.
    /// </summary>
    [Fact]
    public void EncinaTestSupportOptions_HasCorrectDefaults()
    {
        // Arrange & Act
        var options = new EncinaTestSupportOptions();

        // Assert
        options.ClearOutboxBeforeTest.ShouldBeTrue();
        options.ClearInboxBeforeTest.ShouldBeTrue();
        options.ResetSagasBeforeTest.ShouldBeTrue();
        options.ClearScheduledMessagesBeforeTest.ShouldBeTrue();
        options.ClearDeadLetterBeforeTest.ShouldBeTrue();
        options.DefaultWaitTimeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Tests that EncinaTestSupportOptions can be configured.
    /// </summary>
    [Fact]
    public void EncinaTestSupportOptions_CanBeConfigured()
    {
        // Arrange
        var options = new EncinaTestSupportOptions();

        // Act
        options.ClearOutboxBeforeTest = false;
        options.DefaultWaitTimeout = TimeSpan.FromMinutes(5);

        // Assert
        options.ClearOutboxBeforeTest.ShouldBeFalse();
        options.DefaultWaitTimeout.ShouldBe(TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Tests that EncinaTestContext can be constructed and provides access to fake stores.
    /// </summary>
    [Fact]
    public void EncinaTestContext_CanBeConstructed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaFakes();
        var provider = services.BuildServiceProvider();

        var options = new EncinaTestSupportOptions();
        var outboxStore = provider.GetRequiredService<FakeOutboxStore>();
        var inboxStore = provider.GetRequiredService<FakeInboxStore>();
        var sagaStore = provider.GetRequiredService<FakeSagaStore>();
        var scheduledStore = provider.GetRequiredService<FakeScheduledMessageStore>();
        var deadLetterStore = provider.GetRequiredService<FakeDeadLetterStore>();

        // Act
        var context = new EncinaTestContext(
            options,
            outboxStore,
            inboxStore,
            sagaStore,
            scheduledStore,
            deadLetterStore);

        // Assert
        context.ShouldNotBeNull();
        context.OutboxStore.ShouldBeSameAs(outboxStore);
        context.InboxStore.ShouldBeSameAs(inboxStore);
        context.SagaStore.ShouldBeSameAs(sagaStore);
        context.ScheduledMessageStore.ShouldBeSameAs(scheduledStore);
        context.DeadLetterStore.ShouldBeSameAs(deadLetterStore);
    }
}
