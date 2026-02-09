using System.Collections.Frozen;
using System.Text.Json;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Errors;
using Encina.Cdc.Messaging;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Cdc.Processing;

/// <summary>
/// Dispatches CDC change events to registered typed handlers.
/// Routes events based on table-to-entity type mappings configured via <see cref="CdcConfiguration"/>.
/// </summary>
/// <remarks>
/// <para>
/// The dispatcher maintains a registry of table name → entity type → handler type mappings.
/// When a change event is received, it:
/// <list type="number">
///   <item><description>Looks up the entity type for the table name</description></item>
///   <item><description>Deserializes Before/After payloads to the entity type</description></item>
///   <item><description>Resolves the handler from DI and invokes the appropriate method</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class CdcDispatcher : ICdcDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CdcDispatcher> _logger;
    private readonly FrozenDictionary<string, Type> _tableToEntityMap;
    private readonly FrozenDictionary<Type, Type> _entityToHandlerMap;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="CdcDispatcher"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving handlers.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="configuration">The CDC configuration containing table mappings and handler registrations.</param>
    public CdcDispatcher(
        IServiceProvider serviceProvider,
        ILogger<CdcDispatcher> logger,
        CdcConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(configuration);

        _serviceProvider = serviceProvider;
        _logger = logger;

        _tableToEntityMap = configuration.TableMappings
            .ToFrozenDictionary(
                m => m.TableName,
                m => m.EntityType,
                StringComparer.OrdinalIgnoreCase);

        _entityToHandlerMap = configuration.HandlerRegistrations
            .ToFrozenDictionary(
                r => r.EntityType,
                r => r.HandlerType);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DispatchAsync(
        ChangeEvent changeEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changeEvent);

        if (!_tableToEntityMap.TryGetValue(changeEvent.TableName, out var entityType))
        {
            CdcLog.NoHandlerForTable(_logger, changeEvent.TableName, changeEvent.Operation);
            return Right(unit);
        }

        if (!_entityToHandlerMap.TryGetValue(entityType, out var handlerType))
        {
            CdcLog.NoHandlerForTable(_logger, changeEvent.TableName, changeEvent.Operation);
            return Right(unit);
        }

        var handlerInterfaceType = typeof(IChangeEventHandler<>).MakeGenericType(entityType);
        var handler = _serviceProvider.GetService(handlerInterfaceType);

        if (handler is null)
        {
            CdcLog.NoHandlerForTable(_logger, changeEvent.TableName, changeEvent.Operation);
            return Right(unit);
        }

        CdcLog.DispatchingChangeEvent(_logger, changeEvent.Operation, changeEvent.TableName, handlerType.Name);

        var context = new ChangeContext(changeEvent.TableName, changeEvent.Metadata, cancellationToken);

        try
        {
            var result = changeEvent.Operation switch
            {
                ChangeOperation.Insert or ChangeOperation.Snapshot => await DispatchInsertAsync(
                    handler, entityType, changeEvent, context).ConfigureAwait(false),
                ChangeOperation.Update => await DispatchUpdateAsync(
                    handler, entityType, changeEvent, context).ConfigureAwait(false),
                ChangeOperation.Delete => await DispatchDeleteAsync(
                    handler, entityType, changeEvent, context).ConfigureAwait(false),
                _ => Left(CdcErrors.HandlerFailed(
                    changeEvent.TableName,
                    new InvalidOperationException($"Unknown change operation: {changeEvent.Operation}")))
            };

            if (result.IsRight)
            {
                CdcLog.DispatchedChangeEvent(_logger, changeEvent.Operation, changeEvent.TableName);

                // Invoke interceptors after successful handler dispatch
                await InvokeInterceptorsAsync(changeEvent, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
        catch (Exception ex)
        {
            CdcLog.HandlerFailed(_logger, ex, handlerType.Name, changeEvent.Operation, changeEvent.TableName);
            return Left(CdcErrors.HandlerFailed(changeEvent.TableName, ex));
        }
    }

    private async ValueTask<Either<EncinaError, Unit>> DispatchInsertAsync(
        object handler,
        Type entityType,
        ChangeEvent changeEvent,
        ChangeContext context)
    {
        var after = DeserializeEntity(changeEvent.After, entityType, changeEvent.TableName, changeEvent.Operation);
        if (after is null)
        {
            return Left(CdcErrors.DeserializationFailed(
                changeEvent.TableName,
                entityType,
                new InvalidOperationException("After value is required for Insert/Snapshot operations")));
        }

        var method = typeof(IChangeEventHandler<>)
            .MakeGenericType(entityType)
            .GetMethod(nameof(IChangeEventHandler<object>.HandleInsertAsync))!;

        var task = (ValueTask<Either<EncinaError, Unit>>)method.Invoke(handler, [after, context])!;
        return await task.ConfigureAwait(false);
    }

    private async ValueTask<Either<EncinaError, Unit>> DispatchUpdateAsync(
        object handler,
        Type entityType,
        ChangeEvent changeEvent,
        ChangeContext context)
    {
        var before = DeserializeEntity(changeEvent.Before, entityType, changeEvent.TableName, changeEvent.Operation);
        var after = DeserializeEntity(changeEvent.After, entityType, changeEvent.TableName, changeEvent.Operation);

        if (before is null || after is null)
        {
            return Left(CdcErrors.DeserializationFailed(
                changeEvent.TableName,
                entityType,
                new InvalidOperationException("Both Before and After values are required for Update operations")));
        }

        var method = typeof(IChangeEventHandler<>)
            .MakeGenericType(entityType)
            .GetMethod(nameof(IChangeEventHandler<object>.HandleUpdateAsync))!;

        var task = (ValueTask<Either<EncinaError, Unit>>)method.Invoke(handler, [before, after, context])!;
        return await task.ConfigureAwait(false);
    }

    private async ValueTask<Either<EncinaError, Unit>> DispatchDeleteAsync(
        object handler,
        Type entityType,
        ChangeEvent changeEvent,
        ChangeContext context)
    {
        var before = DeserializeEntity(changeEvent.Before, entityType, changeEvent.TableName, changeEvent.Operation);
        if (before is null)
        {
            return Left(CdcErrors.DeserializationFailed(
                changeEvent.TableName,
                entityType,
                new InvalidOperationException("Before value is required for Delete operations")));
        }

        var method = typeof(IChangeEventHandler<>)
            .MakeGenericType(entityType)
            .GetMethod(nameof(IChangeEventHandler<object>.HandleDeleteAsync))!;

        var task = (ValueTask<Either<EncinaError, Unit>>)method.Invoke(handler, [before, context])!;
        return await task.ConfigureAwait(false);
    }

    private object? DeserializeEntity(
        object? value,
        Type entityType,
        string tableName,
        ChangeOperation operation)
    {
        if (value is null)
        {
            return null;
        }

        // If the value is already the correct type, return it directly
        if (entityType.IsInstanceOfType(value))
        {
            return value;
        }

        try
        {
            // If it's a JsonElement, deserialize to the target type
            if (value is JsonElement jsonElement)
            {
                return jsonElement.Deserialize(entityType, JsonOptions);
            }

            // Try to serialize and then deserialize (handles anonymous types, dictionaries, etc.)
            var json = JsonSerializer.Serialize(value, JsonOptions);
            return JsonSerializer.Deserialize(json, entityType, JsonOptions);
        }
        catch (Exception ex)
        {
            CdcLog.DeserializationFailed(_logger, ex, operation, tableName, entityType.Name);
            return null;
        }
    }

    private async ValueTask InvokeInterceptorsAsync(
        ChangeEvent changeEvent,
        CancellationToken cancellationToken)
    {
        var interceptors = _serviceProvider.GetServices<ICdcEventInterceptor>();

        foreach (var interceptor in interceptors)
        {
            try
            {
                await interceptor.OnEventDispatchedAsync(changeEvent, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log but don't fail — interceptor errors should not prevent position saving
                CdcLog.HandlerFailed(
                    _logger, ex, interceptor.GetType().Name, changeEvent.Operation, changeEvent.TableName);
            }
        }
    }
}
