using System.Reflection;
using System.Text;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.DomainModeling;

/// <summary>
/// Marker attribute for entities belonging to a bounded context.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute to document which bounded context a type belongs to.
/// This is useful for understanding the domain model and maintaining
/// clear boundaries between contexts.
/// </para>
/// <example>
/// <code>
/// [BoundedContext("Orders", Description = "Order management and fulfillment")]
/// public sealed class Order : AggregateRoot&lt;OrderId&gt;
/// {
///     // ...
/// }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class BoundedContextAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the bounded context.
    /// </summary>
    public string ContextName { get; }

    /// <summary>
    /// Gets or sets an optional description of the bounded context.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundedContextAttribute"/> class.
    /// </summary>
    /// <param name="contextName">The name of the bounded context.</param>
    public BoundedContextAttribute(string contextName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contextName);
        ContextName = contextName;
    }
}

/// <summary>
/// Relationship types between bounded contexts according to DDD strategic design.
/// </summary>
public enum ContextRelationship
{
    /// <summary>
    /// Downstream context conforms to upstream model without translation.
    /// </summary>
    Conformist,

    /// <summary>
    /// Downstream uses Anti-Corruption Layer to translate between models.
    /// </summary>
    AntiCorruptionLayer,

    /// <summary>
    /// Shared kernel of domain model between contexts.
    /// </summary>
    SharedKernel,

    /// <summary>
    /// Customer-Supplier relationship where downstream is customer.
    /// </summary>
    CustomerSupplier,

    /// <summary>
    /// Partnership between teams with mutual cooperation.
    /// </summary>
    Partnership,

    /// <summary>
    /// Published language for integration (e.g., integration events).
    /// </summary>
    PublishedLanguage,

    /// <summary>
    /// Separate Ways - no integration between contexts.
    /// </summary>
    SeparateWays,

    /// <summary>
    /// Open Host Service - upstream provides public API.
    /// </summary>
    OpenHostService
}

/// <summary>
/// Represents a relationship between two bounded contexts.
/// </summary>
/// <param name="UpstreamContext">The upstream (supplier) context name.</param>
/// <param name="DownstreamContext">The downstream (consumer) context name.</param>
/// <param name="Relationship">The type of relationship.</param>
/// <param name="Description">Optional description of the relationship.</param>
public sealed record ContextRelation(
    string UpstreamContext,
    string DownstreamContext,
    ContextRelationship Relationship,
    string? Description = null);

/// <summary>
/// Maps relationships between bounded contexts.
/// </summary>
/// <remarks>
/// <para>
/// A context map documents the relationships between bounded contexts,
/// helping teams understand how contexts interact and what translation
/// is needed at boundaries.
/// </para>
/// <example>
/// <code>
/// var contextMap = new ContextMap()
///     .AddRelation("Inventory", "Orders", ContextRelationship.CustomerSupplier,
///         "Orders consumes inventory availability")
///     .AddRelation("Orders", "Shipping", ContextRelationship.PublishedLanguage,
///         "Orders publishes OrderPlaced integration events")
///     .AddSharedKernel("Orders", "Billing", "Money value objects");
/// </code>
/// </example>
/// </remarks>
public sealed class ContextMap
{
    private readonly List<ContextRelation> _relations = [];

    /// <summary>
    /// Gets the list of context relations.
    /// </summary>
    public IReadOnlyList<ContextRelation> Relations => _relations.AsReadOnly();

    /// <summary>
    /// Adds a relationship between two bounded contexts.
    /// </summary>
    /// <param name="upstream">The upstream context name.</param>
    /// <param name="downstream">The downstream context name.</param>
    /// <param name="relationship">The type of relationship.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ContextMap AddRelation(
        string upstream,
        string downstream,
        ContextRelationship relationship,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(upstream);
        ArgumentException.ThrowIfNullOrWhiteSpace(downstream);

        _relations.Add(new ContextRelation(upstream, downstream, relationship, description));
        return this;
    }

    /// <summary>
    /// Adds a shared kernel relationship between two contexts.
    /// </summary>
    /// <param name="context1">The first context name.</param>
    /// <param name="context2">The second context name.</param>
    /// <param name="kernelName">The name of the shared kernel.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ContextMap AddSharedKernel(string context1, string context2, string kernelName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context1);
        ArgumentException.ThrowIfNullOrWhiteSpace(context2);
        ArgumentException.ThrowIfNullOrWhiteSpace(kernelName);

        _relations.Add(new ContextRelation(context1, context2, ContextRelationship.SharedKernel, kernelName));
        return this;
    }

    /// <summary>
    /// Adds a customer-supplier relationship.
    /// </summary>
    /// <param name="supplier">The supplier (upstream) context name.</param>
    /// <param name="customer">The customer (downstream) context name.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ContextMap AddCustomerSupplier(string supplier, string customer, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(supplier);
        ArgumentException.ThrowIfNullOrWhiteSpace(customer);

        _relations.Add(new ContextRelation(supplier, customer, ContextRelationship.CustomerSupplier, description));
        return this;
    }

    /// <summary>
    /// Adds a published language relationship (integration events).
    /// </summary>
    /// <param name="publisher">The publisher context name.</param>
    /// <param name="subscriber">The subscriber context name.</param>
    /// <param name="description">Optional description of published events.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ContextMap AddPublishedLanguage(string publisher, string subscriber, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publisher);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriber);

        _relations.Add(new ContextRelation(publisher, subscriber, ContextRelationship.PublishedLanguage, description));
        return this;
    }

    /// <summary>
    /// Gets all context names referenced in this map.
    /// </summary>
    /// <returns>A set of unique context names.</returns>
    public IReadOnlySet<string> GetContextNames()
    {
        var names = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (var relation in _relations)
        {
            names.Add(relation.UpstreamContext);
            names.Add(relation.DownstreamContext);
        }
        return names;
    }

    /// <summary>
    /// Gets all relationships for a specific context.
    /// </summary>
    /// <param name="contextName">The context name.</param>
    /// <returns>Relations where the context is upstream or downstream.</returns>
    public IEnumerable<ContextRelation> GetRelationsFor(string contextName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contextName);

        return _relations.Where(r =>
            string.Equals(r.UpstreamContext, contextName, StringComparison.Ordinal) ||
            string.Equals(r.DownstreamContext, contextName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Gets upstream dependencies for a context.
    /// </summary>
    /// <param name="contextName">The context name.</param>
    /// <returns>Relations where this context is downstream.</returns>
    public IEnumerable<ContextRelation> GetUpstreamDependencies(string contextName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contextName);

        return _relations.Where(r =>
            string.Equals(r.DownstreamContext, contextName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Gets downstream consumers for a context.
    /// </summary>
    /// <param name="contextName">The context name.</param>
    /// <returns>Relations where this context is upstream.</returns>
    public IEnumerable<ContextRelation> GetDownstreamConsumers(string contextName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contextName);

        return _relations.Where(r =>
            string.Equals(r.UpstreamContext, contextName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Generates a Mermaid diagram of the context map.
    /// </summary>
    /// <returns>A Mermaid flowchart string.</returns>
    public string ToMermaidDiagram()
    {
        var sb = new StringBuilder();
        sb.AppendLine("flowchart LR");

        foreach (var relation in _relations)
        {
            var arrow = relation.Relationship switch
            {
                ContextRelationship.SharedKernel => "<-->",
                ContextRelationship.Partnership => "<-->",
                _ => "-->"
            };

            var label = relation.Description ?? relation.Relationship.ToString();
            sb.Append("    ")
              .Append(relation.UpstreamContext)
              .Append(' ')
              .Append(arrow)
              .Append('|')
              .Append(label)
              .Append("| ")
              .AppendLine(relation.DownstreamContext);
        }

        return sb.ToString();
    }
}

/// <summary>
/// Base class for bounded context modules.
/// </summary>
/// <remarks>
/// <para>
/// A bounded context is a semantic boundary where terms have specific meanings.
/// This base class provides a foundation for organizing services and handlers
/// within a bounded context.
/// </para>
/// <example>
/// <code>
/// public sealed class OrdersContext : BoundedContextModule
/// {
///     public override string ContextName => "Orders";
///     public override string? Description => "Order management and fulfillment";
///
///     public override void Configure(IServiceCollection services)
///     {
///         services.AddScoped&lt;IOrderRepository, OrderRepository&gt;();
///         services.AddScoped&lt;OrderPricingService&gt;();
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public abstract class BoundedContextModule
{
    /// <summary>
    /// Gets the name of this bounded context.
    /// </summary>
    public abstract string ContextName { get; }

    /// <summary>
    /// Gets an optional description of this bounded context.
    /// </summary>
    public virtual string? Description => null;

    /// <summary>
    /// Configures the services for this bounded context.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public abstract void Configure(IServiceCollection services);
}

/// <summary>
/// Interface for bounded context modules with explicit contracts.
/// </summary>
/// <remarks>
/// <para>
/// This interface extends <see cref="BoundedContextModule"/> with explicit
/// declarations of integration events and ports, enabling validation
/// of context boundaries and automatic documentation.
/// </para>
/// </remarks>
public interface IBoundedContextModule
{
    /// <summary>
    /// Gets the name of this bounded context.
    /// </summary>
    string ContextName { get; }

    /// <summary>
    /// Gets an optional description of this bounded context.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the integration events this context publishes (its public API).
    /// </summary>
    IEnumerable<Type> PublishedIntegrationEvents { get; }

    /// <summary>
    /// Gets the integration events this context subscribes to (its dependencies).
    /// </summary>
    IEnumerable<Type> ConsumedIntegrationEvents { get; }

    /// <summary>
    /// Gets the ports exposed by this context for other contexts to use.
    /// </summary>
    IEnumerable<Type> ExposedPorts { get; }

    /// <summary>
    /// Configures the services for this bounded context.
    /// </summary>
    /// <param name="services">The service collection.</param>
    void Configure(IServiceCollection services);
}

/// <summary>
/// Error type for bounded context validation failures.
/// </summary>
/// <param name="Message">Description of the validation failure.</param>
/// <param name="ErrorCode">Machine-readable error code.</param>
/// <param name="ContextName">The context where the error occurred.</param>
/// <param name="Details">Optional additional details.</param>
public sealed record BoundedContextError(
    string Message,
    string ErrorCode,
    string? ContextName = null,
    IReadOnlyList<string>? Details = null)
{
    /// <summary>
    /// Creates an error for when a consumed event has no publisher.
    /// </summary>
    public static BoundedContextError OrphanedConsumer(string contextName, Type eventType) =>
        new(
            $"Context '{contextName}' consumes {eventType.Name} but no context publishes it",
            "CONTEXT_ORPHANED_CONSUMER",
            contextName);

    /// <summary>
    /// Creates an error for when a context has circular dependencies.
    /// </summary>
    public static BoundedContextError CircularDependency(IReadOnlyList<string> cycle) =>
        new(
            $"Circular dependency detected: {string.Join(" -> ", cycle)}",
            "CONTEXT_CIRCULAR_DEPENDENCY",
            Details: cycle);

    /// <summary>
    /// Creates an error for when validation fails.
    /// </summary>
    public static BoundedContextError ValidationFailed(string message, IReadOnlyList<string>? details = null) =>
        new(message, "CONTEXT_VALIDATION_FAILED", Details: details);
}

/// <summary>
/// Validates bounded context configurations.
/// </summary>
public sealed class BoundedContextValidator
{
    private readonly List<IBoundedContextModule> _contexts = [];

    /// <summary>
    /// Registers a context for validation.
    /// </summary>
    /// <param name="context">The context to register.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public BoundedContextValidator AddContext(IBoundedContextModule context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _contexts.Add(context);
        return this;
    }

    /// <summary>
    /// Validates that all consumed events have publishers.
    /// </summary>
    /// <returns>Either an error or Unit on success.</returns>
    public Either<BoundedContextError, Unit> ValidateEventContracts()
    {
        var allPublished = _contexts
            .SelectMany(c => c.PublishedIntegrationEvents)
            .ToHashSet();

        var errors = new List<string>();

        foreach (var context in _contexts)
        {
            foreach (var consumed in context.ConsumedIntegrationEvents)
            {
                if (!allPublished.Contains(consumed))
                {
                    errors.Add($"{context.ContextName} consumes {consumed.Name} but no context publishes it");
                }
            }
        }

        if (errors.Count > 0)
        {
            return BoundedContextError.ValidationFailed(
                "Event contract validation failed",
                errors.AsReadOnly());
        }

        return Unit.Default;
    }

    /// <summary>
    /// Generates a context map from registered contexts.
    /// </summary>
    /// <returns>A context map based on event relationships.</returns>
    public ContextMap GenerateContextMap()
    {
        var map = new ContextMap();

        foreach (var consumer in _contexts)
        {
            foreach (var eventType in consumer.ConsumedIntegrationEvents)
            {
                var publisher = _contexts.FirstOrDefault(c =>
                    c.PublishedIntegrationEvents.Contains(eventType));

                if (publisher is not null)
                {
                    map.AddPublishedLanguage(
                        publisher.ContextName,
                        consumer.ContextName,
                        eventType.Name);
                }
            }
        }

        return map;
    }
}

/// <summary>
/// Extension methods for bounded context registration.
/// </summary>
public static class BoundedContextExtensions
{
    /// <summary>
    /// Registers a bounded context module.
    /// </summary>
    /// <typeparam name="TContext">The bounded context type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBoundedContext<TContext>(this IServiceCollection services)
        where TContext : BoundedContextModule, new()
    {
        ArgumentNullException.ThrowIfNull(services);

        var context = new TContext();
        context.Configure(services);

        services.AddSingleton(context);

        return services;
    }

    /// <summary>
    /// Registers a bounded context module with explicit contracts.
    /// </summary>
    /// <typeparam name="TContext">The bounded context type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBoundedContextModule<TContext>(this IServiceCollection services)
        where TContext : class, IBoundedContextModule, new()
    {
        ArgumentNullException.ThrowIfNull(services);

        var context = new TContext();
        context.Configure(services);

        services.AddSingleton<IBoundedContextModule>(context);

        return services;
    }

    /// <summary>
    /// Registers a bounded context module using a factory.
    /// </summary>
    /// <typeparam name="TContext">The bounded context type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">Factory to create the context.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBoundedContext<TContext>(
        this IServiceCollection services,
        Func<IServiceProvider, TContext> factory)
        where TContext : BoundedContextModule
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.AddSingleton<BoundedContextModule>(sp =>
        {
            var context = factory(sp);
            context.Configure(services);
            return context;
        });

        return services;
    }

    /// <summary>
    /// Scans an assembly for bounded context modules and registers them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBoundedContextsFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var contextTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => typeof(BoundedContextModule).IsAssignableFrom(t) ||
                        typeof(IBoundedContextModule).IsAssignableFrom(t));

        foreach (var contextType in contextTypes)
        {
            var context = Activator.CreateInstance(contextType);

            if (context is BoundedContextModule bcm)
            {
                bcm.Configure(services);
                services.AddSingleton(bcm);
            }
            else if (context is IBoundedContextModule ibcm)
            {
                ibcm.Configure(services);
                services.AddSingleton(ibcm);
            }
        }

        return services;
    }

    /// <summary>
    /// Gets the bounded context name for a type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>The context name if attributed, otherwise null.</returns>
    public static string? GetBoundedContextName(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var attribute = type.GetCustomAttribute<BoundedContextAttribute>();
        return attribute?.ContextName;
    }

    /// <summary>
    /// Gets all types in an assembly belonging to a specific bounded context.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="contextName">The context name to filter by.</param>
    /// <returns>Types belonging to the specified context.</returns>
    public static IEnumerable<Type> GetTypesInBoundedContext(
        this Assembly assembly,
        string contextName)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(contextName);

        return assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<BoundedContextAttribute>()?.ContextName == contextName);
    }
}
