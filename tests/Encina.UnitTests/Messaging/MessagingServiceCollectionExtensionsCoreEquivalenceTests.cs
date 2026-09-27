using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Testing.Fakes.Factories;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Messaging;

/// <summary>
/// Regression test proving that <see cref="MessagingServiceCollectionExtensions.AddMessagingServicesCore{TOutboxStore, TOutboxFactory, TInboxStore, TInboxFactory, TOutboxProcessor}"/>
/// still produces the exact same <see cref="ServiceDescriptor"/> set (service type,
/// implementation type and lifetime) after #1406 replaced its inline registration blocks with
/// calls to the shared <c>Register*</c> helpers that <c>AddMessagingServices</c> already uses.
/// The expected snapshots below were captured from the pre-#1406 inline implementation with all
/// patterns enabled and with all patterns disabled; any drift between the two registration paths
/// (the defect class #1333 fixed) will fail one of these tests.
/// </summary>
public sealed class MessagingServiceCollectionExtensionsCoreEquivalenceTests
{
    [Fact]
    public void AddMessagingServicesCore_AllPatternsEnabled_MatchesPreRefactorSnapshot()
    {
        var config = new MessagingConfiguration
        {
            UseTransactions = true,
            UseOutbox = true,
            UseInbox = true,
            UseRoutingSlips = true,
            UseRecoverability = true,
            UseContentRouter = true,
            UseScatterGather = true,
            UseSoftDelete = true,
        };
        config.RecoverabilityOptions.EnableDelayedRetries = true;

        var snapshot = Snapshot(config);

        snapshot.ShouldBe(AllEnabledSnapshot);
    }

    [Fact]
    public void AddMessagingServicesCore_AllPatternsDisabled_MatchesPreRefactorSnapshot()
    {
        var config = new MessagingConfiguration();

        var snapshot = Snapshot(config);

        snapshot.ShouldBe(AllDisabledSnapshot);
    }

    private static List<string> Snapshot(MessagingConfiguration config)
    {
        var services = new ServiceCollection();

        services.AddMessagingServicesCore<
            FakeOutboxStore, FakeOutboxMessageFactory,
            FakeInboxStore, StubInboxFactory,
            StubOutboxProcessor>(config);

        return services
            .Select(d => $"{d.ServiceType}|{Describe(d)}|{d.Lifetime}")
            .ToList();
    }

    private static string Describe(ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationType is not null)
        {
            return descriptor.ImplementationType.ToString();
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return "factory";
        }

        if (descriptor.ImplementationInstance is not null)
        {
            return descriptor.ImplementationInstance.GetType().ToString();
        }

        return "unknown";
    }

    /// <summary>
    /// Snapshot captured from the pre-#1406 <c>AddMessagingServicesCore</c> with every pattern
    /// (Transactions, Outbox, Inbox, Routing Slips, Recoverability with delayed retries,
    /// Content Router, Scatter-Gather, Soft Delete) enabled.
    /// </summary>
    private static readonly List<string> AllEnabledSnapshot =
    [
        "System.TimeProvider|System.TimeProvider+SystemTimeProvider|Singleton",
        "Encina.IRequestContextAccessor|Encina.RequestContextAccessor|Singleton",
        "Encina.Messaging.Serialization.IMessageSerializer|Encina.Messaging.Serialization.JsonMessageSerializer|Singleton",
        "Encina.IPipelineBehavior`2[TRequest,TResponse]|Encina.Messaging.TransactionPipelineBehavior`2[TRequest,TResponse]|Scoped",
        "Encina.Messaging.Outbox.OutboxOptions|Encina.Messaging.Outbox.OutboxOptions|Singleton",
        "Encina.Messaging.Outbox.IOutboxStore|Encina.Testing.Fakes.Stores.FakeOutboxStore|Scoped",
        "Encina.Messaging.Outbox.IOutboxMessageFactory|Encina.Testing.Fakes.Factories.FakeOutboxMessageFactory|Scoped",
        "Encina.Messaging.Outbox.OutboxOrchestrator|Encina.Messaging.Outbox.OutboxOrchestrator|Scoped",
        "Encina.IRequestPostProcessor`2[TRequest,TResponse]|Encina.Messaging.Outbox.OutboxPostProcessor`2[TRequest,TResponse]|Scoped",
        "Microsoft.Extensions.Hosting.IHostedService|Encina.UnitTests.Messaging.MessagingServiceCollectionExtensionsCoreEquivalenceTests+StubOutboxProcessor|Singleton",
        "Encina.Messaging.Inbox.InboxOptions|Encina.Messaging.Inbox.InboxOptions|Singleton",
        "Encina.Messaging.Inbox.IInboxStore|Encina.Testing.Fakes.Stores.FakeInboxStore|Scoped",
        "Encina.Messaging.Inbox.IInboxMessageFactory|Encina.UnitTests.Messaging.MessagingServiceCollectionExtensionsCoreEquivalenceTests+StubInboxFactory|Scoped",
        "Encina.Messaging.Inbox.InboxOrchestrator|Encina.Messaging.Inbox.InboxOrchestrator|Scoped",
        "Encina.IPipelineBehavior`2[TRequest,TResponse]|Encina.Messaging.Inbox.InboxPipelineBehavior`2[TRequest,TResponse]|Scoped",
        "Encina.Messaging.RoutingSlip.RoutingSlipOptions|Encina.Messaging.RoutingSlip.RoutingSlipOptions|Singleton",
        "Encina.Messaging.RoutingSlip.IRoutingSlipRunner|Encina.Messaging.RoutingSlip.RoutingSlipRunner|Scoped",
        "Encina.Messaging.Recoverability.RecoverabilityOptions|Encina.Messaging.Recoverability.RecoverabilityOptions|Singleton",
        "Encina.Messaging.Recoverability.IErrorClassifier|Encina.Messaging.Recoverability.DefaultErrorClassifier|Singleton",
        "Encina.IPipelineBehavior`2[TRequest,TResponse]|Encina.Messaging.Recoverability.RecoverabilityPipelineBehavior`2[TRequest,TResponse]|Scoped",
        "Encina.Messaging.Recoverability.IDelayedRetryScheduler|Encina.Messaging.Recoverability.DelayedRetryScheduler|Scoped",
        "Microsoft.Extensions.Hosting.IHostedService|Encina.Messaging.Recoverability.DelayedRetryProcessor|Singleton",
        "Encina.Messaging.ContentRouter.ContentRouterOptions|Encina.Messaging.ContentRouter.ContentRouterOptions|Singleton",
        "Encina.Messaging.ContentRouter.IContentRouter|Encina.Messaging.ContentRouter.ContentRouter|Scoped",
        "Encina.Messaging.ScatterGather.ScatterGatherOptions|Encina.Messaging.ScatterGather.ScatterGatherOptions|Singleton",
        "Encina.Messaging.ScatterGather.IScatterGatherRunner|Encina.Messaging.ScatterGather.ScatterGatherRunner|Scoped",
        "Encina.Messaging.SoftDelete.SoftDeleteOptions|Encina.Messaging.SoftDelete.SoftDeleteOptions|Singleton",
        "Encina.Messaging.SoftDelete.ISoftDeleteFilterContext|Encina.Messaging.SoftDelete.SoftDeleteFilterContext|Scoped",
        "Encina.IPipelineBehavior`2[TRequest,TResponse]|Encina.Messaging.SoftDelete.SoftDeleteQueryFilterBehavior`2[TRequest,TResponse]|Scoped",
    ];

    /// <summary>
    /// Snapshot captured from the pre-#1406 <c>AddMessagingServicesCore</c> with every pattern
    /// disabled (the default <see cref="MessagingConfiguration"/>): only the always-on
    /// infrastructure registrations remain.
    /// </summary>
    private static readonly List<string> AllDisabledSnapshot =
    [
        "System.TimeProvider|System.TimeProvider+SystemTimeProvider|Singleton",
        "Encina.IRequestContextAccessor|Encina.RequestContextAccessor|Singleton",
        "Encina.Messaging.Serialization.IMessageSerializer|Encina.Messaging.Serialization.JsonMessageSerializer|Singleton",
    ];

    #region Stub Types (minimal implementations for generic constraints)

    private sealed class StubInboxFactory : IInboxMessageFactory
    {
        public IInboxMessage Create(string messageId, string requestType, DateTime receivedAtUtc, DateTime expiresAtUtc, InboxMetadata? metadata)
            => Substitute.For<IInboxMessage>();
    }

    private sealed class StubOutboxProcessor : IHostedService
    {
        public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }

    #endregion
}
