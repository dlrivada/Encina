using System.Diagnostics.CodeAnalysis;
using Encina.Messaging.ContentRouter;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Recoverability;
using Encina.Messaging.RoutingSlip;
using Encina.Messaging.Sagas;
using Encina.Messaging.Sagas.LowCeremony;
using Encina.Messaging.ScatterGather;
using Encina.Messaging.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Messaging;

/// <summary>
/// Helper methods for registering messaging services in DI.
/// Used by provider-specific extensions (Dapper, ADO.NET, EF Core).
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers common messaging orchestrators and behaviors based on configuration.
    /// Provider-specific stores must be registered by the calling extension method.
    /// </summary>
    /// <typeparam name="TOutboxStore">The outbox store implementation type.</typeparam>
    /// <typeparam name="TOutboxFactory">The outbox message factory implementation type.</typeparam>
    /// <typeparam name="TInboxStore">The inbox store implementation type.</typeparam>
    /// <typeparam name="TInboxFactory">The inbox message factory implementation type.</typeparam>
    /// <typeparam name="TSagaStore">The saga store implementation type.</typeparam>
    /// <typeparam name="TSagaFactory">The saga state factory implementation type.</typeparam>
    /// <typeparam name="TScheduledStore">The scheduled message store implementation type.</typeparam>
    /// <typeparam name="TScheduledFactory">The scheduled message factory implementation type.</typeparam>
    /// <typeparam name="TOutboxProcessor">The outbox processor hosted service type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The messaging configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    [SuppressMessage("SonarQube", "S2436:Classes and methods should not have too many generic parameters",
        Justification = "Nine generic parameters are required to support provider-specific implementations for all messaging patterns (Outbox, Inbox, Saga, Scheduling). This is an internal API used by provider packages.")]
    public static IServiceCollection AddMessagingServices<TOutboxStore, TOutboxFactory, TInboxStore, TInboxFactory, TSagaStore, TSagaFactory, TScheduledStore, TScheduledFactory, TOutboxProcessor>(
        this IServiceCollection services,
        MessagingConfiguration config)
        where TOutboxStore : class, IOutboxStore
        where TOutboxFactory : class, IOutboxMessageFactory
        where TInboxStore : class, IInboxStore
        where TInboxFactory : class, IInboxMessageFactory
        where TSagaStore : class, ISagaStore
        where TSagaFactory : class, ISagaStateFactory
        where TScheduledStore : class, IScheduledMessageStore
        where TScheduledFactory : class, IScheduledMessageFactory
        where TOutboxProcessor : class, Microsoft.Extensions.Hosting.IHostedService
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        if (config.UseTransactions)
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionPipelineBehavior<,>));
        }

        if (config.UseOutbox)
        {
            services.AddSingleton(config.OutboxOptions);
            services.AddScoped<IOutboxStore, TOutboxStore>();
            services.AddScoped<IOutboxMessageFactory, TOutboxFactory>();
            services.AddScoped<OutboxOrchestrator>();
            services.AddScoped(typeof(IRequestPostProcessor<,>), typeof(OutboxPostProcessor<,>));
            services.AddHostedService<TOutboxProcessor>();
        }

        if (config.UseInbox)
        {
            services.AddSingleton(config.InboxOptions);
            services.AddScoped<IInboxStore, TInboxStore>();
            services.AddScoped<IInboxMessageFactory, TInboxFactory>();
            services.AddScoped<InboxOrchestrator>();
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InboxPipelineBehavior<,>));
        }

        if (config.UseSagas)
        {
            services.AddSingleton(config.SagaOptions);
            services.AddScoped<ISagaStore, TSagaStore>();
            services.AddScoped<ISagaStateFactory, TSagaFactory>();
            services.AddScoped<SagaOrchestrator>();
            services.AddScoped<ISagaNotFoundDispatcher, SagaNotFoundDispatcher>();

            // Low-ceremony saga runner
            services.AddScoped<ISagaRunner, SagaRunner>();
        }

        if (config.UseRoutingSlips)
        {
            services.AddSingleton(config.RoutingSlipOptions);
            services.AddScoped<IRoutingSlipRunner, RoutingSlipRunner>();
        }

        if (config.UseScheduling)
        {
            services.AddSingleton(config.SchedulingOptions);
            services.AddScoped<IScheduledMessageStore, TScheduledStore>();
            services.AddScoped<IScheduledMessageFactory, TScheduledFactory>();
            services.AddScoped<SchedulerOrchestrator>();
        }

        if (config.UseRecoverability)
        {
            services.AddSingleton(config.RecoverabilityOptions);
            services.TryAddSingleton<IErrorClassifier>(
                config.RecoverabilityOptions.ErrorClassifier ?? new DefaultErrorClassifier());
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RecoverabilityPipelineBehavior<,>));

            if (config.RecoverabilityOptions.EnableDelayedRetries)
            {
                services.TryAddScoped<IDelayedRetryScheduler, DelayedRetryScheduler>();
                services.AddHostedService<DelayedRetryProcessor>();
            }
        }

        if (config.UseContentRouter)
        {
            services.AddSingleton(config.ContentRouterOptions);
            services.AddScoped<IContentRouter, ContentRouter.ContentRouter>();
        }

        if (config.UseScatterGather)
        {
            services.AddSingleton(config.ScatterGatherOptions);
            services.AddScoped<IScatterGatherRunner, ScatterGatherRunner>();
        }

        return services;
    }

    /// <summary>
    /// Registers messaging services for ADO.NET providers (Outbox, Inbox, Transactions only).
    /// </summary>
    /// <typeparam name="TOutboxStore">The outbox store implementation type.</typeparam>
    /// <typeparam name="TOutboxFactory">The outbox message factory implementation type.</typeparam>
    /// <typeparam name="TInboxStore">The inbox store implementation type.</typeparam>
    /// <typeparam name="TInboxFactory">The inbox message factory implementation type.</typeparam>
    /// <typeparam name="TOutboxProcessor">The outbox processor hosted service type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The messaging configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    [SuppressMessage("SonarQube", "S2436:Classes and methods should not have too many generic parameters",
        Justification = "Five generic parameters are required to support provider-specific implementations for core messaging patterns (Outbox, Inbox). This is an internal API used by provider packages.")]
    public static IServiceCollection AddMessagingServicesCore<TOutboxStore, TOutboxFactory, TInboxStore, TInboxFactory, TOutboxProcessor>(
        this IServiceCollection services,
        MessagingConfiguration config)
        where TOutboxStore : class, IOutboxStore
        where TOutboxFactory : class, IOutboxMessageFactory
        where TInboxStore : class, IInboxStore
        where TInboxFactory : class, IInboxMessageFactory
        where TOutboxProcessor : class, Microsoft.Extensions.Hosting.IHostedService
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        if (config.UseTransactions)
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionPipelineBehavior<,>));
        }

        if (config.UseOutbox)
        {
            services.AddSingleton(config.OutboxOptions);
            services.AddScoped<IOutboxStore, TOutboxStore>();
            services.AddScoped<IOutboxMessageFactory, TOutboxFactory>();
            services.AddScoped<OutboxOrchestrator>();
            services.AddScoped(typeof(IRequestPostProcessor<,>), typeof(OutboxPostProcessor<,>));
            services.AddHostedService<TOutboxProcessor>();
        }

        if (config.UseInbox)
        {
            services.AddSingleton(config.InboxOptions);
            services.AddScoped<IInboxStore, TInboxStore>();
            services.AddScoped<IInboxMessageFactory, TInboxFactory>();
            services.AddScoped<InboxOrchestrator>();
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InboxPipelineBehavior<,>));
        }

        if (config.UseRoutingSlips)
        {
            services.AddSingleton(config.RoutingSlipOptions);
            services.AddScoped<IRoutingSlipRunner, RoutingSlipRunner>();
        }

        if (config.UseRecoverability)
        {
            services.AddSingleton(config.RecoverabilityOptions);
            services.TryAddSingleton<IErrorClassifier>(
                config.RecoverabilityOptions.ErrorClassifier ?? new DefaultErrorClassifier());
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RecoverabilityPipelineBehavior<,>));

            // Note: Delayed retries require IDelayedRetryStore which must be
            // registered by the provider (Dapper, EF Core, ADO.NET)
            if (config.RecoverabilityOptions.EnableDelayedRetries)
            {
                services.TryAddScoped<IDelayedRetryScheduler, DelayedRetryScheduler>();
                services.AddHostedService<DelayedRetryProcessor>();
            }
        }

        if (config.UseContentRouter)
        {
            services.AddSingleton(config.ContentRouterOptions);
            services.AddScoped<IContentRouter, ContentRouter.ContentRouter>();
        }

        if (config.UseScatterGather)
        {
            services.AddSingleton(config.ScatterGatherOptions);
            services.AddScoped<IScatterGatherRunner, ScatterGatherRunner>();
        }

        return services;
    }
}
