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

        RegisterTransactions(services, config.UseTransactions);
        RegisterRoutingSlips(services, config);
        RegisterRecoverability(services, config);
        RegisterContentRouter(services, config);
        RegisterScatterGather(services, config);
        RegisterSoftDeleteServices(services, config);

        return services;
    }

    /// <summary>
    /// Registers <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/> when
    /// <paramref name="useTransactions"/> is enabled.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="useTransactions">Whether the Transactions pattern is enabled.</param>
    private static void RegisterTransactions(IServiceCollection services, bool useTransactions)
    {
        if (!useTransactions) return;

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionPipelineBehavior<,>));
    }

    /// <summary>
    /// Registers the Routing Slip pattern when <see cref="MessagingConfiguration.UseRoutingSlips"/>
    /// is enabled.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The messaging configuration.</param>
    private static void RegisterRoutingSlips(IServiceCollection services, MessagingConfiguration config)
    {
        if (!config.UseRoutingSlips) return;

        services.AddSingleton(config.RoutingSlipOptions);
        services.AddScoped<IRoutingSlipRunner, RoutingSlipRunner>();
    }

    /// <summary>
    /// Registers the Recoverability pipeline when <see cref="MessagingConfiguration.UseRecoverability"/>
    /// is enabled, including delayed retries when configured.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The messaging configuration.</param>
    private static void RegisterRecoverability(IServiceCollection services, MessagingConfiguration config)
    {
        if (!config.UseRecoverability) return;

        services.AddSingleton(config.RecoverabilityOptions);
        services.TryAddSingleton<IErrorClassifier>(
            config.RecoverabilityOptions.ErrorClassifier ?? new DefaultErrorClassifier());
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RecoverabilityPipelineBehavior<,>));

        RegisterDelayedRetries(services, config.RecoverabilityOptions);
    }

    /// <summary>
    /// Registers the delayed retry scheduler and processor when
    /// <see cref="RecoverabilityOptions.EnableDelayedRetries"/> is enabled.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The recoverability options.</param>
    private static void RegisterDelayedRetries(IServiceCollection services, RecoverabilityOptions options)
    {
        if (!options.EnableDelayedRetries) return;

        services.TryAddScoped<IDelayedRetryScheduler, DelayedRetryScheduler>();
        services.AddHostedService<DelayedRetryProcessor>();
    }

    /// <summary>
    /// Registers the Content-Based Router pattern when
    /// <see cref="MessagingConfiguration.UseContentRouter"/> is enabled.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The messaging configuration.</param>
    private static void RegisterContentRouter(IServiceCollection services, MessagingConfiguration config)
    {
        if (!config.UseContentRouter) return;

        services.AddSingleton(config.ContentRouterOptions);
        services.AddScoped<IContentRouter, ContentRouter.ContentRouter>();
    }

    /// <summary>
    /// Registers the Scatter-Gather pattern when <see cref="MessagingConfiguration.UseScatterGather"/>
    /// is enabled.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The messaging configuration.</param>
    private static void RegisterScatterGather(IServiceCollection services, MessagingConfiguration config)
    {
        if (!config.UseScatterGather) return;

        services.AddSingleton(config.ScatterGatherOptions);
        services.AddScoped<IScatterGatherRunner, ScatterGatherRunner>();
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

        RegisterOutbox<TOutboxStore, TOutboxFactory, TOutboxProcessor>(services, useOutbox, outboxOptions);
        RegisterInbox<TInboxStore, TInboxFactory>(services, useInbox, inboxOptions);
        RegisterSagas<TSagaStore, TSagaFactory>(services, useSagas, sagaOptions);
        RegisterScheduling<TScheduledStore, TScheduledFactory>(services, useScheduling, schedulingOptions);

        return services;
    }

    /// <summary>
    /// Registers the Outbox pattern's store, factory, orchestrator and background processor when
    /// <paramref name="useOutbox"/> is enabled.
    /// </summary>
    private static void RegisterOutbox<TOutboxStore, TOutboxFactory, TOutboxProcessor>(
        IServiceCollection services, bool useOutbox, OutboxOptions outboxOptions)
        where TOutboxStore : class, IOutboxStore
        where TOutboxFactory : class, IOutboxMessageFactory
        where TOutboxProcessor : class, Microsoft.Extensions.Hosting.IHostedService
    {
        if (!useOutbox) return;

        services.AddSingleton(outboxOptions);
        services.AddScoped<IOutboxStore, TOutboxStore>();
        services.AddScoped<IOutboxMessageFactory, TOutboxFactory>();
        services.AddScoped<OutboxOrchestrator>();
        services.AddScoped(typeof(IRequestPostProcessor<,>), typeof(OutboxPostProcessor<,>));
        services.AddHostedService<TOutboxProcessor>();
    }

    /// <summary>
    /// Registers the Inbox pattern's store, factory, orchestrator and pipeline behavior when
    /// <paramref name="useInbox"/> is enabled.
    /// </summary>
    private static void RegisterInbox<TInboxStore, TInboxFactory>(
        IServiceCollection services, bool useInbox, InboxOptions inboxOptions)
        where TInboxStore : class, IInboxStore
        where TInboxFactory : class, IInboxMessageFactory
    {
        if (!useInbox) return;

        services.AddSingleton(inboxOptions);
        services.AddScoped<IInboxStore, TInboxStore>();
        services.AddScoped<IInboxMessageFactory, TInboxFactory>();
        services.AddScoped<InboxOrchestrator>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InboxPipelineBehavior<,>));
    }

    /// <summary>
    /// Registers the Saga pattern's store, factory, orchestrator, low-ceremony runner and
    /// not-found dispatcher when <paramref name="useSagas"/> is enabled.
    /// </summary>
    private static void RegisterSagas<TSagaStore, TSagaFactory>(
        IServiceCollection services, bool useSagas, SagaOptions sagaOptions)
        where TSagaStore : class, ISagaStore
        where TSagaFactory : class, ISagaStateFactory
    {
        if (!useSagas) return;

        services.AddSingleton(sagaOptions);
        services.AddScoped<ISagaStore, TSagaStore>();
        services.AddScoped<ISagaStateFactory, TSagaFactory>();
        services.AddScoped<SagaOrchestrator>();
        services.AddScoped<ISagaNotFoundDispatcher, SagaNotFoundDispatcher>();

        // Low-ceremony saga runner
        services.AddScoped<ISagaRunner, SagaRunner>();
    }

    /// <summary>
    /// Registers the Scheduling pattern's store, factory, retry policy, dispatcher and orchestrator
    /// when <paramref name="useScheduling"/> is enabled, including the background processor when
    /// configured.
    /// </summary>
    private static void RegisterScheduling<TScheduledStore, TScheduledFactory>(
        IServiceCollection services, bool useScheduling, SchedulingOptions schedulingOptions)
        where TScheduledStore : class, IScheduledMessageStore
        where TScheduledFactory : class, IScheduledMessageFactory
    {
        if (!useScheduling) return;

        services.AddSingleton(schedulingOptions);
        services.AddScoped<IScheduledMessageStore, TScheduledStore>();
        services.AddScoped<IScheduledMessageFactory, TScheduledFactory>();
        services.TryAddSingleton<IScheduledMessageRetryPolicy>(
            sp => new ExponentialBackoffRetryPolicy(sp.GetRequiredService<SchedulingOptions>()));
        services.TryAddScoped<IScheduledMessageDispatcher>(
            sp => new CompiledExpressionScheduledMessageDispatcher(sp.GetRequiredService<IEncina>()));
        services.AddScoped<SchedulerOrchestrator>();

        RegisterScheduledMessageProcessor(services, schedulingOptions);
    }

    /// <summary>
    /// Registers the background <see cref="ScheduledMessageProcessor"/> hosted service when
    /// <see cref="SchedulingOptions.EnableProcessor"/> is enabled.
    /// </summary>
    private static void RegisterScheduledMessageProcessor(IServiceCollection services, SchedulingOptions schedulingOptions)
    {
        if (!schedulingOptions.EnableProcessor) return;

        services.AddHostedService<ScheduledMessageProcessor>();
    }

    /// <summary>
    /// Registers messaging services for ADO.NET providers: Transactions, Outbox, Inbox, Routing
    /// Slips, Recoverability, Content Router, Scatter-Gather and Soft Delete, driven by the
    /// individual flags on <see cref="MessagingConfiguration"/>.
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

        RegisterTransactions(services, config.UseTransactions);
        RegisterOutbox<TOutboxStore, TOutboxFactory, TOutboxProcessor>(services, config.UseOutbox, config.OutboxOptions);
        RegisterInbox<TInboxStore, TInboxFactory>(services, config.UseInbox, config.InboxOptions);
        RegisterRoutingSlips(services, config);
        RegisterRecoverability(services, config);
        RegisterContentRouter(services, config);
        RegisterScatterGather(services, config);
        RegisterSoftDeleteServices(services, config);

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
        if (!config.UseSoftDelete) return;

        // Register options as singleton
        services.AddSingleton(config.SoftDeleteOptions);

        // Register the scoped filter context for communicating filter state
        // from pipeline behaviors to repositories
        services.TryAddScoped<ISoftDeleteFilterContext, SoftDeleteFilterContext>();

        // Register the pipeline behavior to configure filter context based on request type
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SoftDeleteQueryFilterBehavior<,>));
    }
}
