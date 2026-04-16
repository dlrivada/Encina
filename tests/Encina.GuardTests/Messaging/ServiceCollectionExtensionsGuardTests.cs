using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Testing.Fakes.Factories;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace Encina.GuardTests.Messaging;

/// <summary>
/// Guard clause tests for MessagingServiceCollectionExtensions.
/// </summary>
public class ServiceCollectionExtensionsGuardTests
{
    #region AddMessagingServices (Full)

    [Fact]
    public void AddMessagingServices_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;
        var config = new MessagingConfiguration();

        var act = () => services.AddMessagingServices<
            FakeOutboxStore, FakeOutboxMessageFactory,
            FakeInboxStore, StubInboxFactory,
            FakeSagaStore, StubSagaFactory,
            FakeScheduledMessageStore, StubScheduledFactory,
            StubOutboxProcessor>(config);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddMessagingServices_NullConfig_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddMessagingServices<
            FakeOutboxStore, FakeOutboxMessageFactory,
            FakeInboxStore, StubInboxFactory,
            FakeSagaStore, StubSagaFactory,
            FakeScheduledMessageStore, StubScheduledFactory,
            StubOutboxProcessor>(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("config");
    }

    #endregion

    #region AddMessagingServicesCore

    [Fact]
    public void AddMessagingServicesCore_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;
        var config = new MessagingConfiguration();

        var act = () => services.AddMessagingServicesCore<
            FakeOutboxStore, FakeOutboxMessageFactory,
            FakeInboxStore, StubInboxFactory,
            StubOutboxProcessor>(config);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddMessagingServicesCore_NullConfig_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddMessagingServicesCore<
            FakeOutboxStore, FakeOutboxMessageFactory,
            FakeInboxStore, StubInboxFactory,
            StubOutboxProcessor>(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("config");
    }

    #endregion

    #region Stub Types (minimal implementations for generic constraints)

    internal class StubInboxFactory : IInboxMessageFactory
    {
        public IInboxMessage Create(string messageId, string requestType, DateTime receivedAtUtc, DateTime expiresAtUtc, InboxMetadata? metadata)
            => Substitute.For<IInboxMessage>();
    }

    internal class StubSagaFactory : ISagaStateFactory
    {
        public ISagaState Create(Guid sagaId, string sagaType, string data, string status, int currentStep, DateTime startedAtUtc, DateTime? timeoutAtUtc = null)
            => Substitute.For<ISagaState>();
    }

    internal class StubScheduledFactory : IScheduledMessageFactory
    {
        public IScheduledMessage Create(Guid id, string requestType, string content, DateTime scheduledAtUtc, DateTime createdAtUtc, bool isRecurring, string? cronExpression)
            => Substitute.For<IScheduledMessage>();
    }

    internal class StubOutboxProcessor : IHostedService
    {
        public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }

    #endregion
}
