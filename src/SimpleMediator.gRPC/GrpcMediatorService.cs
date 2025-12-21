using System.Text.Json;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace SimpleMediator.gRPC;

/// <summary>
/// gRPC-based implementation of the mediator service.
/// </summary>
#pragma warning disable CA1848 // Use LoggerMessage delegates
public sealed class GrpcMediatorService : IGrpcMediatorService
{
    private readonly IMediator _mediator;
    private readonly ILogger<GrpcMediatorService> _logger;
    private readonly SimpleMediatorGrpcOptions _options;
    private readonly Dictionary<string, Type> _requestTypeCache = new();
    private readonly Dictionary<string, Type> _notificationTypeCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcMediatorService"/> class.
    /// </summary>
    /// <param name="mediator">The SimpleMediator instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public GrpcMediatorService(
        IMediator mediator,
        ILogger<GrpcMediatorService> logger,
        IOptions<SimpleMediatorGrpcOptions> options)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _mediator = mediator;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, byte[]>> SendAsync(
        string requestType,
        byte[] requestData,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(requestData);

        try
        {
            _logger.LogDebug(
                "Processing gRPC request of type {RequestType}",
                requestType);

            var type = ResolveType(requestType, _requestTypeCache);
            if (type is null)
            {
                return Left<MediatorError, byte[]>(
                    MediatorErrors.Create(
                        "GRPC_TYPE_NOT_FOUND",
                        $"Request type '{requestType}' not found."));
            }

            var request = JsonSerializer.Deserialize(requestData, type);
            if (request is null)
            {
                return Left<MediatorError, byte[]>(
                    MediatorErrors.Create(
                        "GRPC_DESERIALIZE_FAILED",
                        "Failed to deserialize request."));
            }

            // Use reflection to call the generic Send method
            var sendMethod = typeof(IMediator)
                .GetMethods()
                .First(m => m.Name == "Send" && m.GetGenericArguments().Length == 2);

            var responseType = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
                ?.GetGenericArguments()[0];

            if (responseType is null)
            {
                return Left<MediatorError, byte[]>(
                    MediatorErrors.Create(
                        "GRPC_RESPONSE_TYPE_NOT_FOUND",
                        $"Could not determine response type for request '{requestType}'."));
            }

            var genericMethod = sendMethod.MakeGenericMethod(type, responseType);
            var task = (dynamic)genericMethod.Invoke(_mediator, [request, cancellationToken])!;
            Either<MediatorError, object> result = await task;

            return result.Match(
                Right: response =>
                {
                    var responseBytes = JsonSerializer.SerializeToUtf8Bytes(response, responseType);
                    return Right<MediatorError, byte[]>(responseBytes);
                },
                Left: error => Left<MediatorError, byte[]>(error));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process gRPC request of type {RequestType}",
                requestType);

            return Left<MediatorError, byte[]>(
                MediatorErrors.FromException(
                    "GRPC_SEND_FAILED",
                    ex,
                    $"Failed to process request of type '{requestType}'."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, Unit>> PublishAsync(
        string notificationType,
        byte[] notificationData,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notificationType);
        ArgumentNullException.ThrowIfNull(notificationData);

        try
        {
            _logger.LogDebug(
                "Processing gRPC notification of type {NotificationType}",
                notificationType);

            var type = ResolveType(notificationType, _notificationTypeCache);
            if (type is null)
            {
                return Left<MediatorError, Unit>(
                    MediatorErrors.Create(
                        "GRPC_TYPE_NOT_FOUND",
                        $"Notification type '{notificationType}' not found."));
            }

            var notification = JsonSerializer.Deserialize(notificationData, type);
            if (notification is null)
            {
                return Left<MediatorError, Unit>(
                    MediatorErrors.Create(
                        "GRPC_DESERIALIZE_FAILED",
                        "Failed to deserialize notification."));
            }

            // Use reflection to call the generic Publish method
            var publishMethod = typeof(IMediator)
                .GetMethods()
                .First(m => m.Name == "Publish" && m.GetGenericArguments().Length == 1);

            var genericMethod = publishMethod.MakeGenericMethod(type);
            var task = (ValueTask)genericMethod.Invoke(_mediator, [notification, cancellationToken])!;
            await task;

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process gRPC notification of type {NotificationType}",
                notificationType);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    "GRPC_PUBLISH_FAILED",
                    ex,
                    $"Failed to process notification of type '{notificationType}'."));
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Either<MediatorError, byte[]>> StreamAsync(
        string requestType,
        byte[] requestData,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Streaming is not yet implemented
        await Task.CompletedTask;

        yield return Left<MediatorError, byte[]>(
            MediatorErrors.Create(
                "GRPC_STREAMING_NOT_IMPLEMENTED",
                "Streaming is not yet implemented."));
    }

    private static Type? ResolveType(string typeName, Dictionary<string, Type> cache)
    {
        if (cache.TryGetValue(typeName, out var cachedType))
        {
            return cachedType;
        }

        var type = Type.GetType(typeName);
        if (type is not null)
        {
            cache[typeName] = type;
        }

        return type;
    }
}
