using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimpleMediator.EntityFrameworkCore.Inbox;
using SimpleMediator.EntityFrameworkCore.Outbox;
using SimpleMediator.EntityFrameworkCore.Sagas;
using SimpleMediator.EntityFrameworkCore.Scheduling;
using SimpleMediator.Messaging;
using SimpleMediator.Messaging.Inbox;
using SimpleMediator.Messaging.Outbox;
using SimpleMediator.Messaging.Sagas;
using SimpleMediator.Messaging.Scheduling;

namespace SimpleMediator.EntityFrameworkCore;

/// <summary>
/// Extension methods for configuring SimpleMediator Entity Framework Core integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Entity Framework Core messaging patterns support to SimpleMediator with opt-in configuration.
    /// </summary>
    /// <typeparam name="TDbContext">The type of DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for enabling messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method provides a unified configuration for all messaging patterns.
    /// All patterns are opt-in (disabled by default). Enable only what you need:
    /// </para>
    /// <para>
    /// <b>Available Patterns</b>:
    /// <list type="bullet">
    /// <item><description><b>Transactions</b>: Automatic database transaction management</description></item>
    /// <item><description><b>Outbox</b>: Reliable event publishing (at-least-once delivery)</description></item>
    /// <item><description><b>Inbox</b>: Idempotent message processing (exactly-once semantics)</description></item>
    /// <item><description><b>Sagas</b>: Distributed transactions with compensation</description></item>
    /// <item><description><b>Scheduling</b>: Delayed/recurring command execution</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Requirements</b>:
    /// <list type="bullet">
    /// <item><description><typeparamref name="TDbContext"/> must be registered in DI</description></item>
    /// <item><description>SimpleMediator must be configured first</description></item>
    /// <item><description>For each enabled pattern, the corresponding DbSet and configuration must be added to your DbContext</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple app - only transactions
    /// services.AddSimpleMediatorEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseTransactions = true;
    /// });
    ///
    /// // Complex distributed system - all patterns
    /// services.AddSimpleMediatorEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseTransactions = true;
    ///     config.UseOutbox = true;
    ///     config.UseInbox = true;
    ///     config.UseSagas = true;
    ///     config.UseScheduling = true;
    ///
    ///     // Configure pattern-specific options
    ///     config.OutboxOptions.ProcessingInterval = TimeSpan.FromSeconds(30);
    ///     config.InboxOptions.MaxRetries = 5;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddSimpleMediatorEntityFrameworkCore<TDbContext>(
        this IServiceCollection services,
        Action<MessagingConfiguration> configure)
        where TDbContext : DbContext
    {
        var config = new MessagingConfiguration();
        configure(config);

        // Register the DbContext as DbContext (non-generic) for behaviors
        services.TryAddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());

        // Register enabled patterns
        if (config.UseTransactions)
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionPipelineBehavior<,>));
        }

        if (config.UseOutbox)
        {
            services.AddSingleton(config.OutboxOptions);
            services.AddScoped<IOutboxStore, OutboxStoreEF>();
            services.AddScoped(typeof(IRequestPostProcessor<,>), typeof(OutboxPostProcessor<,>));
            services.AddHostedService<OutboxProcessor>();
        }

        if (config.UseInbox)
        {
            services.AddSingleton(config.InboxOptions);
            services.AddScoped<IInboxStore, InboxStoreEF>();
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InboxPipelineBehavior<,>));
        }

        if (config.UseSagas)
        {
            services.AddScoped<ISagaStore, SagaStoreEF>();
        }

        if (config.UseScheduling)
        {
            services.AddSingleton(config.SchedulingOptions);
            services.AddScoped<IScheduledMessageStore, ScheduledMessageStoreEF>();
            // TODO: Add SchedulingProcessor as HostedService when implemented
        }

        return services;
    }

    /// <summary>
    /// Adds Entity Framework Core messaging patterns support with default configuration (no patterns enabled).
    /// </summary>
    /// <typeparam name="TDbContext">The type of DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This overload registers the DbContext mapping but doesn't enable any patterns.
    /// Use the overload with configuration action to enable patterns.
    /// </remarks>
    public static IServiceCollection AddSimpleMediatorEntityFrameworkCore<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        // Just register DbContext mapping, no patterns enabled
        services.TryAddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());
        return services;
    }
}
