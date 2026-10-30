using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Purge;
using Encina.Testing.Fakes.Factories;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Encina.UnitTests.Messaging.Purge;

/// <summary>
/// The registration of <see cref="MessagePurgeService"/> by the shared messaging registration (#3013): present only
/// with the inbox on and its automatic purge on, complete under <c>ValidateOnBuild</c> and <c>ValidateScopes</c>,
/// whatever the order options are configured in, and never duplicated.
/// </summary>
public sealed class MessagePurgeRegistrationTests
{
    private static readonly ServiceProviderOptions Strict = new() { ValidateOnBuild = true, ValidateScopes = true };

    [Fact]
    public void InboxAndAutomaticPurgeOn_RegistersOneServiceThatBuildsUnderValidation()
    {
        var services = Register(new MessagingConfiguration { UseInbox = true });

        using var provider = services.BuildServiceProvider(Strict);

        provider.GetServices<IHostedService>().OfType<MessagePurgeService>().ShouldHaveSingleItem();
        provider.GetRequiredService<MessagePurgeSchedule>().InboxPurge.ShouldNotBeNull();
    }

    [Fact]
    public void AutomaticPurgeOff_RegistersNothingForThePurge()
    {
        var config = new MessagingConfiguration { UseInbox = true };
        config.InboxOptions.EnableAutomaticPurge = false;

        var services = Register(config);

        services.ShouldNotContain(d => d.ImplementationType == typeof(MessagePurgeService));
        services.ShouldNotContain(d => d.ServiceType == typeof(MessagePurgeSchedule));
        using var provider = services.BuildServiceProvider(Strict);
        provider.GetServices<IHostedService>().OfType<MessagePurgeService>().ShouldBeEmpty();
    }

    [Fact]
    public void InboxOff_RegistersNothingForThePurge()
    {
        var services = Register(new MessagingConfiguration { UseInbox = false });

        services.ShouldNotContain(d => d.ImplementationType == typeof(MessagePurgeService));
    }

    [Fact]
    public void OptionsConfiguredBeforeOrAfterRegistration_AreReadWhenTheScheduleIsResolved()
    {
        var config = new MessagingConfiguration { UseInbox = true };
        config.InboxOptions.PurgeInterval = TimeSpan.FromHours(2);
        var services = Register(config);

        // Mutated after the registration call, before the provider is built.
        config.InboxOptions.PurgeBatchSize = 7;

        using var provider = services.BuildServiceProvider(Strict);
        provider.GetRequiredService<MessagePurgeSchedule>().InboxPurge
            .ShouldBe(new PurgeEntry(TimeSpan.FromHours(2), 7));
    }

    [Fact]
    public void AutomaticPurgeSwitchedOffAfterRegistration_YieldsAScheduleWithoutEntries()
    {
        var config = new MessagingConfiguration { UseInbox = true };
        var services = Register(config);
        config.InboxOptions.EnableAutomaticPurge = false;

        using var provider = services.BuildServiceProvider(Strict);

        provider.GetRequiredService<MessagePurgeSchedule>().HasEntries.ShouldBeFalse();
    }

    [Fact]
    public void RegisteredTwice_StillRunsOneLoop()
    {
        var config = new MessagingConfiguration { UseInbox = true };
        var services = Register(config);

        services.AddMessagingServicesCore<
            FakeOutboxStore, FakeOutboxMessageFactory, FakeInboxStore, StubInboxFactory, StubProcessor>(config);

        services.Count(d => d.ImplementationType == typeof(MessagePurgeService)).ShouldBe(1);
    }

    [Fact]
    public void ApplicationTimeProviderRegisteredFirst_IsKept()
    {
        var clock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(clock);

        Register(new MessagingConfiguration { UseInbox = true }, services);

        using var provider = services.BuildServiceProvider(Strict);
        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(clock);
    }

    private static ServiceCollection Register(MessagingConfiguration config, ServiceCollection? services = null)
    {
        services ??= [];
        services.AddLogging();

        // The scope's shared transaction accessor resolves the scoped connection of an ADO.NET or Dapper provider.
        services.AddSingleton(Substitute.For<System.Data.IDbConnection>());
        services.AddMessagingServicesCore<
            FakeOutboxStore, FakeOutboxMessageFactory, FakeInboxStore, StubInboxFactory, StubProcessor>(config);
        return services;
    }

    private sealed class StubInboxFactory : IInboxMessageFactory
    {
        public IInboxMessage Create(string messageId, string requestType, DateTime receivedAtUtc, DateTime expiresAtUtc, InboxMetadata? metadata)
            => Substitute.For<IInboxMessage>();
    }

    private sealed class StubProcessor : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
