using System.Text.Json;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.gRPC;

/// <summary>
/// gRPC-based implementation of the Encina service.
/// </summary>
public sealed class GrpcEncinaService : IGrpcEncinaService
{
    private readonly IEncina _encina;
    private readonly ILogger<GrpcEncinaService> _logger;
    private readonly Dictionary<string, Type> _requestTypeCache = new();
    private readonly Dictionary<string, Type> _notificationTypeCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcEncinaService"/> class.
    /// </summary>
    /// <param name="encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options (reserved for future use).</param>
    public GrpcEncinaService(
        IEncina encina,
        ILogger<GrpcEncinaService> logger,
        IOptions<EncinaGrpcOptions> options)
    {
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _encina = encina;
        _logger = logger;
        _ = options.Value; // Reserved for future use
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, byte[]>> SendAsync(
        string requestType,
        byte[] requestData,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(requestData);

        try
        {
            Log.ProcessingRequest(_logger, requestType);

            var type = ResolveType(requestType, _requestTypeCache);
            if (type is null)
            {
                return Left<EncinaError, byte[]>(
                    EncinaErrors.Create(
                        "GRPC_TYPE_NOT_FOUND",
                        $"Request type '{requestType}' not found."));
            }

            var request = JsonSerializer.Deserialize(requestData, type);
            if (request is null)
            {
                return Left<EncinaError, byte[]>(
                    EncinaErrors.Create(
                        "GRPC_DESERIALIZE_FAILED",
                        "Failed to deserialize request."));
            }

            // Use reflection to call the generic Send method
            var sendMethod = typeof(IEncina)
                .GetMethods()
                .First(m => m.Name == "Send" && m.GetGenericArguments().Length == 2);

            var responseType = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
                ?.GetGenericArguments()[0];

            if (responseType is null)
            {
                return Left<EncinaError, byte[]>(
                    EncinaErrors.Create(
                        "GRPC_RESPONSE_TYPE_NOT_FOUND",
                        $"Could not determine response type for request '{requestType}'."));
            }

            var genericMethod = sendMethod.MakeGenericMethod(type, responseType);
            var task = (dynamic)genericMethod.Invoke(_encina, [request, cancellationToken])!;
            Either<EncinaError, object> result = await task;

            return result.Match(
                Right: response =>
                {
                    var responseBytes = JsonSerializer.SerializeToUtf8Bytes(response, responseType);
                    return Right<EncinaError, byte[]>(responseBytes);
                },
                Left: error => Left<EncinaError, byte[]>(error));
        }
        catch (Exception ex)
        {
            Log.FailedToProcessRequest(_logger, ex, requestType);

            return Left<EncinaError, byte[]>(
                EncinaErrors.FromException(
                    "GRPC_SEND_FAILED",
                    ex,
                    $"Failed to process request of type '{requestType}'."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> PublishAsync(
        string notificationType,
        byte[] notificationData,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notificationType);
        ArgumentNullException.ThrowIfNull(notificationData);

        try
        {
            Log.ProcessingNotification(_logger, notificationType);

            var type = ResolveType(notificationType, _notificationTypeCache);
            if (type is null)
            {
                return Left<EncinaError, Unit>(
                    EncinaErrors.Create(
                        "GRPC_TYPE_NOT_FOUND",
                        $"Notification type '{notificationType}' not found."));
            }

            var notification = JsonSerializer.Deserialize(notificationData, type);
            if (notification is null)
            {
                return Left<EncinaError, Unit>(
                    EncinaErrors.Create(
                        "GRPC_DESERIALIZE_FAILED",
                        "Failed to deserialize notification."));
            }

            // Use reflection to call the generic Publish method
            var publishMethod = typeof(IEncina)
                .GetMethods()
                .First(m => m.Name == "Publish" && m.GetGenericArguments().Length == 1);

            var genericMethod = publishMethod.MakeGenericMethod(type);
            var task = (ValueTask)genericMethod.Invoke(_encina, [notification, cancellationToken])!;
            await task;

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            Log.FailedToProcessNotification(_logger, ex, notificationType);

            return Left<EncinaError, Unit>(
                EncinaErrors.FromException(
                    "GRPC_PUBLISH_FAILED",
                    ex,
                    $"Failed to process notification of type '{notificationType}'."));
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Either<EncinaError, byte[]>> StreamAsync(
        string requestType,
        byte[] requestData,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Streaming is not yet implemented
        await Task.CompletedTask;

        yield return Left<EncinaError, byte[]>(
            EncinaErrors.Create(
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
