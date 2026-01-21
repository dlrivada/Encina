using Encina.Messaging.ReadWriteSeparation;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore.ReadWriteSeparation;

/// <summary>
/// Pipeline behavior that sets the database routing context based on the request type.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior automatically determines the appropriate <see cref="DatabaseIntent"/> based on
/// whether the request is a query (<see cref="IQuery{TResponse}"/>) or command (<see cref="ICommand{TResponse}"/>).
/// It also checks for the <see cref="ForceWriteDatabaseAttribute"/> to override the default routing.
/// </para>
/// <para>
/// <b>Routing Logic:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="ICommand{TResponse}"/>: Routes to primary database (<see cref="DatabaseIntent.Write"/>)
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="IQuery{TResponse}"/>: Routes to read replica (<see cref="DatabaseIntent.Read"/>)
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="IQuery{TResponse}"/> with <see cref="ForceWriteDatabaseAttribute"/>:
///       Routes to primary database (<see cref="DatabaseIntent.ForceWrite"/>)
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Scope Management:</b>
/// The behavior creates a <see cref="DatabaseRoutingScope"/> that sets the intent for the duration
/// of the request. The scope is automatically disposed after the handler completes, restoring
/// any previous routing context.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query automatically routes to read replica
/// public sealed record GetProductsQuery : IQuery&lt;IReadOnlyList&lt;Product&gt;&gt;;
///
/// // Command automatically routes to primary database
/// public sealed record CreateProductCommand(string Name) : ICommand&lt;Product&gt;;
///
/// // Query with ForceWriteDatabase routes to primary for consistency
/// [ForceWriteDatabase(Reason = "Must read latest inventory after update")]
/// public sealed record GetInventoryAfterUpdateQuery(Guid ProductId) : IQuery&lt;Inventory&gt;;
/// </code>
/// </example>
public sealed class ReadWriteRoutingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ReadWriteRoutingPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadWriteRoutingPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public ReadWriteRoutingPipelineBehavior(
        ILogger<ReadWriteRoutingPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        // Determine the appropriate database intent
        var intent = DetermineIntent(request);

        Log.SettingDatabaseIntent(_logger, typeof(TRequest).Name, intent, context.CorrelationId);

        // Create a routing scope that will be disposed after the handler completes
        using var scope = new DatabaseRoutingScope(intent);

        return await nextStep();
    }

    private static DatabaseIntent DetermineIntent(TRequest request)
    {
        var requestType = typeof(TRequest);

        // Check if it's a command - always use write
        if (IsCommand(requestType))
        {
            return DatabaseIntent.Write;
        }

        // Check if it's a query
        if (IsQuery(requestType))
        {
            // Check for ForceWriteDatabase attribute
            if (HasForceWriteDatabaseAttribute(requestType))
            {
                return DatabaseIntent.ForceWrite;
            }

            return DatabaseIntent.Read;
        }

        // Default to write for unknown request types (safe default)
        return DatabaseIntent.Write;
    }

    private static bool IsCommand(Type requestType)
    {
        // Check if type implements ICommand<TResponse> or ICommand
        return requestType.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>))
            || typeof(ICommand).IsAssignableFrom(requestType);
    }

    private static bool IsQuery(Type requestType)
    {
        // Check if type implements IQuery<TResponse> or IQuery
        return requestType.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>))
            || typeof(IQuery).IsAssignableFrom(requestType);
    }

    private static bool HasForceWriteDatabaseAttribute(Type requestType)
    {
        return requestType.GetCustomAttributes(typeof(ForceWriteDatabaseAttribute), inherit: true).Length > 0;
    }
}

internal static partial class Log
{
    [LoggerMessage(
        EventId = 7001,
        Level = LogLevel.Debug,
        Message = "[{RequestType}] Setting database intent to {Intent} (CorrelationId: {CorrelationId})")]
    public static partial void SettingDatabaseIntent(
        ILogger logger,
        string requestType,
        DatabaseIntent intent,
        string correlationId);
}
