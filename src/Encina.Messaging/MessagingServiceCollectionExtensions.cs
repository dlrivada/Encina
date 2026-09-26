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
using Encina.Messaging.Serialization;
using Encina.Messaging.SoftDelete;
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

        services.AddOutboxInboxSagaSchedulingServices<TOutboxStore, TOutboxFactory, TInboxStore, TInboxFactory, TSagaStore, TSagaFactory, TScheduledStore, TScheduledFactory, TOutboxProcessor>(
            config.UseOutbox, config.OutboxOptions,
            config.UseInbox, config.InboxOptions,
            config.UseSagas, config.SagaOptions,
            config.UseScheduling, config.SchedulingOptions);

        if (config.UseTransactions)
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionPipelineBehavior<,>));
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

        if (config.UseSoftDelete)
        {
            RegisterSoftDeleteServices(services, config);
        }

        return services;
    }

    /// <summary>
    /// Registers the Outbox, Inbox, Saga and Scheduling patterns from individual flags and
    /// options instances, without requiring a <see cref="MessagingConfiguration"/>.
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
    /// <param name="useOutbox">Whether to register the Outbox pattern.</param>
    /// <param name="outboxOptions">The outbox options.</param>
    /// <param name="useInbox">Whether to register the Inbox pattern.</param>
    /// <param name="inboxOptions">The inbox options.</param>
    /// <param name="useSagas">Whether to register the Saga pattern.</param>
    /// <param name="sagaOptions">The saga options.</param>
    /// <param name="useScheduling">Whether to register the Scheduling pattern.</param>
    /// <param name="schedulingOptions">The scheduling options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// <see cref="AddMessagingServices{TOutboxStore, TOutboxFactory, TInboxStore, TInboxFactory, TSagaStore, TSagaFactory, TScheduledStore, TScheduledFactory, TOutboxProcessor}"/>
    /// calls this method for ADO.NET and Dapper, whose <see cref="MessagingConfiguration"/> also
    /// drives Transactions, Routing Slips, Recoverability, Content Router, Scatter-Gather and Soft
    /// Delete through the generic <c>Encina.Messaging.TransactionPipelineBehavior{TRequest, TResponse}</c>
    /// and the other shared, provider-agnostic behaviors. EF Core and MongoDB call this method
    /// directly instead: EF Core has its own DbContext-bound transaction behavior and does not
    /// (yet) support those other patterns, and MongoDB's <c>EncinaMongoDbOptions</c> is not a
    /// <see cref="MessagingConfiguration"/> at all, though it exposes the same Outbox, Inbox, Saga
    /// and Scheduling flags and option types. Either way, the Outbox, Inbox and Saga
    /// registrations - including <see cref="ISagaRunner"/> and <see cref="ISagaNotFoundDispatcher"/> -
    /// never drift between providers (#1333).
    /// </para>
    /// </remarks>
    [SuppressMessage("SonarQube", "S2436:Classes and methods should not have too many generic parameters",
        Justification = "Nine generic parameters are required to support provider-specific implementations for all messaging patterns (Outbox, Inbox, Saga, Scheduling). This is an internal API used by provider packages.")]
    public static IServiceCollection AddOutboxInboxSagaSchedulingServices<TOutboxStore, TOutboxFactory, TInboxStore, TInboxFactory, TSagaStore, TSagaFactory, TScheduledStore, TScheduledFactory, TOutboxProcessor>(
        this IServiceCollection services,
        bool useOutbox,
        OutboxOptions outboxOptions,
        bool useInbox,
        InboxOptions inboxOptions,
        bool useSagas,
        SagaOptions sagaOptions,
        bool useScheduling,
        SchedulingOptions schedulingOptions)
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
        ArgumentNullException.ThrowIfNull(outboxOptions);
        ArgumentNullException.ThrowIfNull(inboxOptions);
        ArgumentNullException.ThrowIfNull(sagaOptions);
        ArgumentNullException.ThrowIfNull(schedulingOptions);

        // Register TimeProvider for consistent timestamps across all messaging components
        services.TryAddSingleton(TimeProvider.System);

        // Register the ambient request context accessor. AddEncina() also registers it (TryAdd is
        // idempotent), but provider packages that wire messaging without the core mediator still
        // need it resolvable, since SagaRunner and other consumers require it.
        services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();

        // Outbox, inbox, saga and scheduling components take IMessageSerializer as a required
        // dependency; TryAdd keeps a registration made by AddEncinaMessageEncryption.
        services.TryAddDefaultMessageSerializer();

        if (useOutbox)
        {
            services.AddSingleton(outboxOptions);
            services.AddScoped<IOutboxStore, TOutboxStore>();
            services.AddScoped<IOutboxMessageFactory, TOutboxFactory>();
            services.AddScoped<OutboxOrchestrator>();
            services.AddScoped(typeof(IRequestPostProcessor<,>), typeof(OutboxPostProcessor<,>));
            services.AddHostedService<TOutboxProcessor>();
        }

        if (useInbox)
        {
            services.AddSingleton(inboxOptions);
            services.AddScoped<IInboxStore, TInboxStore>();
            services.AddScoped<IInboxMessageFactory, TInboxFactory>();
            services.AddScoped<InboxOrchestrator>();
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InboxPipelineBehavior<,>));
        }

        if (useSagas)
        {
            services.AddSingleton(sagaOptions);
            services.AddScoped<ISagaStore, TSagaStore>();
            services.AddScoped<ISagaStateFactory, TSagaFactory>();
            services.AddScoped<SagaOrchestrator>();
            services.AddScoped<ISagaNotFoundDispatcher, SagaNotFoundDispatcher>();

            // Low-ceremony saga runner
            services.AddScoped<ISagaRunner, SagaRunner>();
        }

        if (useScheduling)
        {
            services.AddSingleton(schedulingOptions);
            services.AddScoped<IScheduledMessageStore, TScheduledStore>();
            services.AddScoped<IScheduledMessageFactory, TScheduledFactory>();
            services.TryAddSingleton<IScheduledMessageRetryPolicy>(
                sp => new ExponentialBackoffRetryPolicy(sp.GetRequiredService<SchedulingOptions>()));
            services.TryAddScoped<IScheduledMessageDispatcher>(
                sp => new CompiledExpressionScheduledMessageDispatcher(sp.GetRequiredService<IEncina>()));
            services.AddScoped<SchedulerOrchestrator>();

            if (schedulingOptions.EnableProcessor)
            {
                services.AddHostedService<ScheduledMessageProcessor>();
            }
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

        // Register TimeProvider for consistent timestamps across all messaging components
        services.TryAddSingleton(TimeProvider.System);

        // Register the ambient request context accessor. AddEncina() also registers it (TryAdd is
        // idempotent), but provider packages that wire messaging without the core mediator still
        // need it resolvable, since SagaRunner and other consumers require it.
        services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();

        // Outbox, inbox, saga and scheduling components take IMessageSerializer as a required
        // dependency; TryAdd keeps a registration made by AddEncinaMessageEncryption.
        services.TryAddDefaultMessageSerializer();

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

        if (config.UseSoftDelete)
        {
            RegisterSoftDeleteServices(services, config);
        }

        return services;
    }

    /// <summary>
    /// Registers <see cref="JsonMessageSerializer"/> as the <see cref="IMessageSerializer"/>
    /// unless a serializer is already registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Every component that persists or reads back a message payload (outbox post-processor
    /// and orchestrator, inbox, saga and scheduler orchestrators, dead letter queue, delayed
    /// retries, the CDC outbox handler) takes <see cref="IMessageSerializer"/> as a required
    /// dependency. Each provider registration (ADO.NET, Dapper, EF Core, MongoDB) and each
    /// standalone registration (dead letter queue, recoverability, CDC) calls this method so the
    /// serializer is always resolvable, whichever provider the application uses.
    /// </para>
    /// <para>
    /// The registration is a <c>TryAdd</c>, so it never replaces an existing one. That keeps
    /// <c>AddEncinaMessageEncryption</c> (which decorates the serializer with
    /// <c>EncryptingMessageSerializer</c>) effective in either order: called after the provider,
    /// it wraps the <see cref="JsonMessageSerializer"/> registered here; called before, its own
    /// registration is already present and this method does nothing.
    /// </para>
    /// </remarks>
    public static IServiceCollection TryAddDefaultMessageSerializer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IMessageSerializer, JsonMessageSerializer>();

        return services;
    }

    /// <summary>
    /// Registers soft delete filter services and pipeline behavior.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The messaging configuration.</param>
    /// <remarks>
    /// <para>
    /// Registers the following services:
    /// <list type="bullet">
    /// <item><description><see cref="SoftDeleteOptions"/>: Singleton configuration options</description></item>
    /// <item><description><see cref="ISoftDeleteFilterContext"/>: Scoped filter state context</description></item>
    /// <item><description><see cref="SoftDeleteQueryFilterBehavior{TRequest, TResponse}"/>: Pipeline behavior for filter configuration</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Pipeline Behavior Order</b>: The soft delete filter behavior should run early in the pipeline,
    /// before validation and authorization behaviors, to ensure the filter context is configured
    /// before any data access occurs.
    /// </para>
    /// </remarks>
    private static void RegisterSoftDeleteServices(
        IServiceCollection services,
        MessagingConfiguration config)
    {
        // Register options as singleton
        services.AddSingleton(config.SoftDeleteOptions);

        // Register the scoped filter context for communicating filter state
        // from pipeline behaviors to repositories
        services.TryAddScoped<ISoftDeleteFilterContext, SoftDeleteFilterContext>();

        // Register the pipeline behavior to configure filter context based on request type
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SoftDeleteQueryFilterBehavior<,>));
    }
}
