using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Encina.Testing.Architecture;

/// <summary>
/// Provides pre-built architectural rules for enforcing Encina best practices and clean architecture patterns.
/// </summary>
/// <remarks>
/// <para>
/// These rules help ensure that your codebase follows established architectural patterns:
/// </para>
/// <list type="bullet">
/// <item><description>Handlers don't depend on infrastructure directly</description></item>
/// <item><description>Domain layer doesn't depend on messaging infrastructure</description></item>
/// <item><description>Notifications are sealed for proper event handling</description></item>
/// <item><description>Proper layer separation is maintained</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class ArchitectureTests
/// {
///     private static readonly Architecture Architecture = new ArchLoader()
///         .LoadAssemblies(typeof(MyHandler).Assembly)
///         .Build();
///
///     [Fact]
///     public void Handlers_ShouldNotDependOnInfrastructure()
///     {
///         EncinaArchitectureRules
///             .HandlersShouldNotDependOnInfrastructure()
///             .Check(Architecture);
///     }
/// }
/// </code>
/// </example>
public static class EncinaArchitectureRules
{
    /// <summary>
    /// Creates a rule that handlers should not depend on infrastructure namespaces directly.
    /// </summary>
    /// <remarks>
    /// Handlers should depend on abstractions (repositories, services) rather than concrete
    /// infrastructure implementations like Entity Framework, Dapper, or specific database drivers.
    /// </remarks>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule HandlersShouldNotDependOnInfrastructure()
    {
        var infrastructureTypes = Classes()
            .That()
            .ResideInNamespaceMatching(".*EntityFrameworkCore.*")
            .Or()
            .ResideInNamespaceMatching(".*Dapper.*")
            .Or()
            .ResideInNamespaceMatching(".*SqlClient.*")
            .Or()
            .ResideInNamespaceMatching(".*Npgsql.*")
            .Or()
            .ResideInNamespaceMatching(".*MongoDB.*")
            .As("Infrastructure Types");

        return Classes()
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .DoNotHaveNameEndingWith("NotificationHandler")
            .Should()
            .NotDependOnAny(infrastructureTypes)
            .Because("Handlers should depend on abstractions, not infrastructure implementations");
    }

    /// <summary>
    /// Creates a rule that notification types should be sealed.
    /// </summary>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule NotificationsShouldBeSealed()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("Notification")
            .Or()
            .HaveNameEndingWith("Event")
            .Should()
            .BeSealed()
            .Because("Notifications and Events should be sealed to prevent inheritance issues");
    }

    /// <summary>
    /// Creates a rule that handler classes should be sealed.
    /// </summary>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule HandlersShouldBeSealed()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .BeSealed()
            .Because("Handlers should be sealed to prevent inheritance and improve performance");
    }

    /// <summary>
    /// Creates a rule that types in the specified domain namespace should not depend on messaging infrastructure.
    /// </summary>
    /// <param name="domainNamespace">The namespace pattern for domain types (e.g., "MyApp.Domain").</param>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule DomainShouldNotDependOnMessaging(string domainNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainNamespace);

        var messagingTypes = Classes()
            .That()
            .ResideInNamespaceMatching(".*Encina\\.Messaging.*")
            .As("Messaging Types");

        return Classes()
            .That()
            .ResideInNamespaceMatching($".*{EscapeForRegex(domainNamespace)}.*")
            .Should()
            .NotDependOnAny(messagingTypes)
            .Because("Domain layer should be independent of messaging infrastructure");
    }

    /// <summary>
    /// Creates a rule that types in the specified domain namespace should not depend on application layer.
    /// </summary>
    /// <param name="domainNamespace">The namespace pattern for domain types.</param>
    /// <param name="applicationNamespace">The namespace pattern for application types.</param>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule DomainShouldNotDependOnApplication(string domainNamespace, string applicationNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationNamespace);

        var applicationTypes = Classes()
            .That()
            .ResideInNamespaceMatching($".*{EscapeForRegex(applicationNamespace)}.*")
            .As("Application Types");

        return Classes()
            .That()
            .ResideInNamespaceMatching($".*{EscapeForRegex(domainNamespace)}.*")
            .Should()
            .NotDependOnAny(applicationTypes)
            .Because("Domain layer should not depend on Application layer");
    }

    /// <summary>
    /// Creates a rule that application layer types should not depend on infrastructure.
    /// </summary>
    /// <param name="applicationNamespace">The namespace pattern for application types.</param>
    /// <param name="infrastructureNamespace">The namespace pattern for infrastructure types.</param>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule ApplicationShouldNotDependOnInfrastructure(
        string applicationNamespace,
        string infrastructureNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(infrastructureNamespace);

        var infrastructureTypes = Classes()
            .That()
            .ResideInNamespaceMatching($".*{EscapeForRegex(infrastructureNamespace)}.*")
            .As("Infrastructure Types");

        return Classes()
            .That()
            .ResideInNamespaceMatching($".*{EscapeForRegex(applicationNamespace)}.*")
            .Should()
            .NotDependOnAny(infrastructureTypes)
            .Because("Application layer should not depend on Infrastructure layer");
    }

    /// <summary>
    /// Creates a rule that validators should follow naming convention.
    /// </summary>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule ValidatorsShouldFollowNamingConvention()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("Validator")
            .Should()
            .BeSealed()
            .Because("Validators should be sealed and follow the naming convention *Validator");
    }

    /// <summary>
    /// Creates a rule that pipeline behaviors should be sealed.
    /// </summary>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule BehaviorsShouldBeSealed()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("Behavior")
            .Should()
            .BeSealed()
            .Because("Pipeline behaviors should be sealed");
    }

    /// <summary>
    /// Creates a rule that repository interfaces should reside in the domain layer.
    /// </summary>
    /// <param name="domainNamespace">The namespace pattern for domain types.</param>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule RepositoryInterfacesShouldResideInDomain(string domainNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainNamespace);

        return Interfaces()
            .That()
            .HaveNameEndingWith("Repository")
            .Should()
            .ResideInNamespaceMatching($".*{EscapeForRegex(domainNamespace)}.*")
            .Because("Repository interfaces should be defined in the Domain layer");
    }

    /// <summary>
    /// Creates a rule that repository implementations should reside in the infrastructure layer.
    /// </summary>
    /// <param name="infrastructureNamespace">The namespace pattern for infrastructure types.</param>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule RepositoryImplementationsShouldResideInInfrastructure(string infrastructureNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(infrastructureNamespace);

        return Classes()
            .That()
            .HaveNameEndingWith("Repository")
            .Should()
            .ResideInNamespaceMatching($".*{EscapeForRegex(infrastructureNamespace)}.*")
            .Because("Repository implementations should be in the Infrastructure layer");
    }

    private static string EscapeForRegex(string input) =>
        Regex.Escape(input);

    /// <summary>
    /// Creates a rule that request types should follow the naming convention.
    /// </summary>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    /// <remarks>
    /// Requests should end with Command or Query to indicate their intent.
    /// </remarks>
    public static IArchRule RequestsShouldFollowNamingConvention()
    {
        return Classes()
            .That()
            .ImplementInterface(typeof(IRequest<>))
            .Or()
            .ImplementInterface(typeof(ICommand<>))
            .Or()
            .ImplementInterface(typeof(IQuery<>))
            .Should()
            .HaveNameMatching(".*(?:Command|Query)$")
            .Because("Request types should end with 'Command' or 'Query' to indicate their intent");
    }

    /// <summary>
    /// Creates a rule that aggregates should be sealed and follow naming conventions.
    /// </summary>
    /// <remarks>
    /// This rule verifies that classes ending with "Aggregate" in the specified namespace are sealed.
    /// While aggregates should ideally inherit from AggregateRoot&lt;TId&gt; or AggregateBase&lt;TId&gt;,
    /// ArchUnitNET supports two approaches for validating generic base class inheritance:
    /// (1) Use <c>AreAssignableTo(typeof(MyGenericBase&lt;&gt;))</c> to check assignment to open generic types, or
    /// (2) Create custom <c>DescribedPredicate</c> implementations that inspect <c>BaseType</c> and
    /// <c>GenericTypeDefinition</c> via reflection to detect generic base-class inheritance.
    /// </remarks>
    /// <param name="aggregateNamespace">The namespace pattern for aggregates.</param>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule AggregatesShouldFollowPattern(string aggregateNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateNamespace);

        return Classes()
            .That()
            .ResideInNamespaceMatching($".*{EscapeForRegex(aggregateNamespace)}.*")
            .And()
            .HaveNameEndingWith("Aggregate")
            .Should()
            .BeSealed()
            .Because("Aggregates should be sealed and follow naming conventions");
    }

    /// <summary>
    /// Creates a rule that value objects should be records or sealed classes.
    /// </summary>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule ValueObjectsShouldBeSealed()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("ValueObject")
            .Or()
            .HaveNameEndingWith("VO")
            .Should()
            .BeSealed()
            .Because("Value objects should be sealed records or classes for immutability");
    }

    /// <summary>
    /// Creates a rule that saga types should be sealed.
    /// </summary>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule SagasShouldBeSealed()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("Saga")
            .Should()
            .BeSealed()
            .Because("Sagas should be sealed for clarity and performance");
    }

    /// <summary>
    /// Creates a rule that store implementations should be sealed.
    /// </summary>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule StoreImplementationsShouldBeSealed()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("Store")
            .And()
            .DoNotHaveNameStartingWith("Fake")
            .Should()
            .BeSealed()
            .Because("Store implementations should be sealed to prevent misuse");
    }

    /// <summary>
    /// Creates a rule that event handler classes should be sealed.
    /// </summary>
    /// <remarks>
    /// Sealing event handlers prevents inheritance and improves performance.
    /// </remarks>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule EventHandlersShouldBeSealed()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("EventHandler")
            .Should()
            .BeSealed()
            .Because("Event handlers should be sealed");
    }

    /// <summary>
    /// Creates a combined rule for clean architecture layer separation.
    /// </summary>
    /// <param name="domainNamespace">The namespace for domain types.</param>
    /// <param name="applicationNamespace">The namespace for application types.</param>
    /// <param name="infrastructureNamespace">The namespace for infrastructure types.</param>
    /// <returns>A combined architecture rule for layer separation.</returns>
    public static IArchRule CleanArchitectureLayersShouldBeSeparated(
        string domainNamespace,
        string applicationNamespace,
        string infrastructureNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(infrastructureNamespace);

        var domainRule = DomainShouldNotDependOnApplication(domainNamespace, applicationNamespace);
        var appRule = ApplicationShouldNotDependOnInfrastructure(applicationNamespace, infrastructureNamespace);

        return domainRule.And(appRule);
    }

    #region CQRS Pattern Rules

    /// <summary>
    /// Creates a rule that handler classes should implement the correct Encina handler interface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This rule verifies that classes ending with "Handler" implement one of:
    /// <list type="bullet">
    /// <item><description><see cref="IRequestHandler{TRequest, TResponse}"/> - Base handler interface</description></item>
    /// <item><description><see cref="ICommandHandler{TCommand, TResponse}"/> - Command handler interface</description></item>
    /// <item><description><see cref="IQueryHandler{TQuery, TResponse}"/> - Query handler interface</description></item>
    /// <item><description><see cref="INotificationHandler{TNotification}"/> - Notification handler interface</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    /// <example>
    /// <code>
    /// // Valid handler - implements ICommandHandler
    /// public sealed class CreateOrderHandler : ICommandHandler&lt;CreateOrderCommand, OrderId&gt;
    /// {
    ///     public ValueTask&lt;Either&lt;EncinaError, OrderId&gt;&gt; Handle(CreateOrderCommand request, CancellationToken ct) => ...;
    /// }
    ///
    /// // Invalid handler - missing interface implementation
    /// public sealed class InvalidHandler // Will fail the rule
    /// {
    ///     public void Handle() { }
    /// }
    /// </code>
    /// </example>
    public static IArchRule HandlersShouldImplementCorrectInterface()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .AreNotAbstract()
            .Should()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .OrShould()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .OrShould()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .OrShould()
            .ImplementInterface(typeof(INotificationHandler<>))
            .Because("Handlers must implement IRequestHandler<,>, ICommandHandler<,>, IQueryHandler<,>, or INotificationHandler<>");
    }

    /// <summary>
    /// Creates a rule that command classes should implement the ICommand interface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This rule verifies that classes ending with "Command" implement <see cref="ICommand{TResponse}"/>
    /// or <see cref="ICommand"/> (for Unit-returning commands).
    /// </para>
    /// <para>
    /// Encina supports two command patterns:
    /// <list type="bullet">
    /// <item><description><c>ICommand&lt;TResponse&gt;</c> - Commands that return a specific response type</description></item>
    /// <item><description><c>ICommand</c> - Commands that return Unit (fire-and-forget)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    /// <example>
    /// <code>
    /// // Valid command with response
    /// public sealed record CreateOrderCommand(string CustomerId) : ICommand&lt;OrderId&gt;;
    ///
    /// // Valid command without response (returns Unit)
    /// public sealed record DeleteOrderCommand(Guid OrderId) : ICommand;
    ///
    /// // Invalid - ends with "Command" but doesn't implement ICommand
    /// public sealed record InvalidCommand(string Data); // Will fail the rule
    /// </code>
    /// </example>
    public static IArchRule CommandsShouldImplementICommand()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("Command")
            .And()
            .AreNotAbstract()
            .And()
            .AreNotNested()
            .Should()
            .ImplementInterface(typeof(ICommand<>))
            .OrShould()
            .ImplementInterface(typeof(ICommand))
            .Because("Commands should implement ICommand<TResponse> or ICommand for Encina CQRS pattern");
    }

    /// <summary>
    /// Creates a rule that query classes should implement the IQuery interface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This rule verifies that classes ending with "Query" implement <see cref="IQuery{TResponse}"/>
    /// or <see cref="IQuery"/> (for Unit-returning queries, though this is uncommon).
    /// </para>
    /// <para>
    /// Queries in CQRS should be read-only operations that return data without modifying state.
    /// </para>
    /// </remarks>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    /// <example>
    /// <code>
    /// // Valid query
    /// public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery&lt;OrderDto&gt;;
    ///
    /// // Valid query returning collection
    /// public sealed record GetOrdersForCustomerQuery(string CustomerId) : IQuery&lt;IReadOnlyList&lt;OrderDto&gt;&gt;;
    ///
    /// // Invalid - ends with "Query" but doesn't implement IQuery
    /// public sealed record InvalidQuery(string Filter); // Will fail the rule
    /// </code>
    /// </example>
    public static IArchRule QueriesShouldImplementIQuery()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("Query")
            .And()
            .AreNotAbstract()
            .And()
            .AreNotNested()
            .Should()
            .ImplementInterface(typeof(IQuery<>))
            .OrShould()
            .ImplementInterface(typeof(IQuery))
            .Because("Queries should implement IQuery<TResponse> or IQuery for Encina CQRS pattern");
    }

    /// <summary>
    /// Creates a rule that handlers should not depend on controller or API types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This rule enforces separation between the application layer (handlers) and the
    /// presentation layer (controllers, API endpoints). Handlers should be framework-agnostic
    /// and not have dependencies on ASP.NET Core controllers or similar presentation concerns.
    /// </para>
    /// </remarks>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    /// <example>
    /// <code>
    /// // Valid handler - no controller dependencies
    /// public sealed class CreateOrderHandler : ICommandHandler&lt;CreateOrderCommand, OrderId&gt;
    /// {
    ///     private readonly IOrderRepository _repository;
    ///     public CreateOrderHandler(IOrderRepository repository) => _repository = repository;
    /// }
    ///
    /// // Invalid handler - depends on controller
    /// public sealed class BadHandler : ICommandHandler&lt;SomeCommand, Unit&gt;
    /// {
    ///     private readonly OrderController _controller; // Will fail the rule
    /// }
    /// </code>
    /// </example>
    public static IArchRule HandlersShouldNotDependOnControllers()
    {
        var controllerTypes = Classes()
            .That()
            .ResideInNamespaceMatching(".*Controllers.*")
            .Or()
            .ResideInNamespaceMatching(".*Api.*")
            .Or()
            .ResideInNamespaceMatching(".*Endpoints.*")
            .Or()
            .HaveNameEndingWith("Controller")
            .Or()
            .HaveNameEndingWith("Endpoint")
            .As("Controller/API Types");

        return Classes()
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .AreNotAbstract()
            .Should()
            .NotDependOnAny(controllerTypes)
            .Because("Handlers should not depend on presentation layer (Controllers, APIs, Endpoints)");
    }

    #endregion

    #region Pipeline Behavior Rules

    /// <summary>
    /// Creates a rule that pipeline behavior classes should implement the correct interface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This rule verifies that classes with "Behavior" or "PipelineBehavior" suffix implement
    /// one of Encina's pipeline behavior interfaces:
    /// <list type="bullet">
    /// <item><description><see cref="IPipelineBehavior{TRequest, TResponse}"/> - Generic pipeline behavior</description></item>
    /// <item><description><see cref="ICommandPipelineBehavior{TCommand, TResponse}"/> - Command-specific behavior</description></item>
    /// <item><description><see cref="IQueryPipelineBehavior{TQuery, TResponse}"/> - Query-specific behavior</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Pipeline behaviors wrap handler execution and can implement cross-cutting concerns like
    /// logging, validation, caching, and transaction management.
    /// </para>
    /// </remarks>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    /// <example>
    /// <code>
    /// // Valid behavior
    /// public sealed class LoggingBehavior&lt;TRequest, TResponse&gt; : IPipelineBehavior&lt;TRequest, TResponse&gt;
    ///     where TRequest : IRequest&lt;TResponse&gt;
    /// {
    ///     public ValueTask&lt;Either&lt;EncinaError, TResponse&gt;&gt; Handle(
    ///         TRequest request, RequestHandlerDelegate&lt;TResponse&gt; next, CancellationToken ct) => next();
    /// }
    ///
    /// // Invalid - ends with "Behavior" but doesn't implement interface
    /// public sealed class InvalidBehavior { } // Will fail the rule
    /// </code>
    /// </example>
    public static IArchRule PipelineBehaviorsShouldImplementCorrectInterface()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("Behavior")
            .Or()
            .HaveNameEndingWith("PipelineBehavior")
            .And()
            .AreNotAbstract()
            .Should()
            .ImplementInterface(typeof(IPipelineBehavior<,>))
            .OrShould()
            .ImplementInterface(typeof(ICommandPipelineBehavior<,>))
            .OrShould()
            .ImplementInterface(typeof(IQueryPipelineBehavior<,>))
            .Because("Pipeline behaviors must implement IPipelineBehavior<TRequest, TResponse> or a specialized variant");
    }

    #endregion

    #region Saga Pattern Rules

    /// <summary>
    /// Creates a rule that saga data classes should be sealed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Saga data classes hold the state of a saga instance and are serialized/deserialized
    /// during saga execution. They should be sealed to ensure proper serialization behavior
    /// and to prevent inheritance complications.
    /// </para>
    /// <para>
    /// Saga data must satisfy the <c>class, new()</c> constraint for proper instantiation.
    /// </para>
    /// </remarks>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    /// <example>
    /// <code>
    /// // Valid saga data
    /// public sealed class OrderSagaData
    /// {
    ///     public Guid OrderId { get; set; }
    ///     public string CustomerId { get; set; } = string.Empty;
    ///     public decimal TotalAmount { get; set; }
    /// }
    ///
    /// // Invalid - not sealed
    /// public class InvalidSagaData // Will fail the rule
    /// {
    ///     public Guid Id { get; set; }
    /// }
    /// </code>
    /// </example>
    public static IArchRule SagaDataShouldBeSealed()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("SagaData")
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .Because("Saga data classes should be sealed for proper serialization and instantiation");
    }

    #endregion
}
