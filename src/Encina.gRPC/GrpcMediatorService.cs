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
    private const string GrpcTypeNotFound = "GRPC_TYPE_NOT_FOUND";
    private const string GrpcDeserializeFailed = "GRPC_DESERIALIZE_FAILED";

    private readonly IEncina _encina;
    private readonly ILogger<GrpcEncinaService> _logger;
    private readonly ITypeResolver _typeResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcEncinaService"/> class.
    /// </summary>
    /// <param name="encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="typeResolver">The type resolver for resolving request and notification types.</param>
    /// <param name="options">The configuration options (reserved for future use).</param>
    public GrpcEncinaService(
        IEncina encina,
        ILogger<GrpcEncinaService> logger,
        ITypeResolver typeResolver,
        IOptions<EncinaGrpcOptions> options)
    {
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(typeResolver);
        ArgumentNullException.ThrowIfNull(options);

        _encina = encina;
        _logger = logger;
        _typeResolver = typeResolver;
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

            var type = _typeResolver.ResolveRequestType(requestType);
            if (type is null)
            {
                return Left<EncinaError, byte[]>( // NOSONAR S6966: Left is a pure function
                    EncinaErrors.Create(
                        GrpcTypeNotFound,
                        $"Request type '{requestType}' not found."));
            }

            var request = JsonSerializer.Deserialize(requestData, type);
            if (request is null)
            {
                return Left<EncinaError, byte[]>( // NOSONAR S6966: Left is a pure function
                    EncinaErrors.Create(
                        GrpcDeserializeFailed,
                        "Failed to deserialize request."));
            }

            // Use reflection to call the generic Send method
            // IEncina.Send<TResponse> has 1 generic parameter (the response type)
            var sendMethod = typeof(IEncina)
                .GetMethods()
                .FirstOrDefault(m => m.Name == "Send" && m.GetGenericArguments().Length == 1);

            if (sendMethod is null)
            {
                return Left<EncinaError, byte[]>(
                    EncinaErrors.Create(
                        "GRPC_SEND_METHOD_NOT_FOUND",
                        $"IEncina does not expose a generic Send<TResponse> method. " +
                        $"Ensure the IEncina interface defines a Send method with exactly one generic type parameter."));
            }

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

            var genericMethod = sendMethod.MakeGenericMethod(responseType);
            var invokeResult = genericMethod.Invoke(_encina, [request, cancellationToken])
                ?? throw new InvalidOperationException(
                    $"IEncina.Send<{responseType.Name}> returned null. " +
                    $"The method must return a non-null ValueTask.");
            var task = (dynamic)invokeResult;
            dynamic result = await task;

            return ExtractEitherResult(result, responseType, requestType);
        }
        catch (GrpcSerializationException ex)
        {
            Log.FailedToProcessRequest(_logger, ex, requestType);

            return Left<EncinaError, byte[]>( // NOSONAR S6966: Left is a pure function
                EncinaErrors.FromException(
                    "GRPC_SERIALIZE_FAILED",
                    ex.InnerException ?? ex,
                    $"Failed to serialize response for request of type '{requestType}'."));
        }
        catch (JsonException ex)
        {
            Log.FailedToProcessRequest(_logger, ex, requestType);

            return Left<EncinaError, byte[]>( // NOSONAR S6966: Left is a pure function
                EncinaErrors.FromException(
                    GrpcDeserializeFailed,
                    ex,
                    $"Failed to deserialize request of type '{requestType}'."));
        }
        catch (Exception ex)
        {
            Log.FailedToProcessRequest(_logger, ex, requestType);

            return Left<EncinaError, byte[]>( // NOSONAR S6966: Left is a pure function
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

            var type = _typeResolver.ResolveNotificationType(notificationType);
            if (type is null)
            {
                return Left<EncinaError, Unit>( // NOSONAR S6966: Left is a pure function
                    EncinaErrors.Create(
                        GrpcTypeNotFound,
                        $"Notification type '{notificationType}' not found."));
            }

            var notification = JsonSerializer.Deserialize(notificationData, type);
            if (notification is null)
            {
                return Left<EncinaError, Unit>( // NOSONAR S6966: Left is a pure function
                    EncinaErrors.Create(
                        GrpcDeserializeFailed,
                        "Failed to deserialize notification."));
            }

            // Use reflection to call the generic Publish method
            // IEncina.Publish<TNotification> returns ValueTask<Either<EncinaError, Unit>>
            var publishMethod = typeof(IEncina)
                .GetMethods()
                .FirstOrDefault(m => m.Name == "Publish" && m.GetGenericArguments().Length == 1);

            if (publishMethod is null)
            {
                return Left<EncinaError, Unit>( // NOSONAR S6966: Left is a pure function
                    EncinaErrors.Create(
                        "GRPC_PUBLISH_METHOD_NOT_FOUND",
                        $"IEncina does not expose a generic Publish<TNotification> method. " +
                        $"Ensure the IEncina interface defines a Publish method with exactly one generic type parameter."));
            }

            var genericMethod = publishMethod.MakeGenericMethod(type);
            var invokeResult = genericMethod.Invoke(_encina, [notification, cancellationToken])
                ?? throw new InvalidOperationException(
                    $"IEncina.Publish<{type.Name}> returned null. " +
                    $"The method must return a non-null ValueTask.");
            var task = (dynamic)invokeResult;
            Either<EncinaError, Unit> result = await task;

            return result;
        }
        catch (JsonException ex)
        {
            Log.FailedToProcessNotification(_logger, ex, notificationType);

            return Left<EncinaError, Unit>( // NOSONAR S6966: Left is a pure function
                EncinaErrors.FromException(
                    GrpcDeserializeFailed,
                    ex,
                    $"Failed to deserialize notification of type '{notificationType}'."));
        }
        catch (Exception ex)
        {
            Log.FailedToProcessNotification(_logger, ex, notificationType);

            return Left<EncinaError, Unit>( // NOSONAR S6966: Left is a pure function
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

    /// <summary>
    /// Extracts the result from a dynamic Either&lt;EncinaError, T&gt; and serializes it to bytes.
    /// </summary>
    /// <param name="result">The dynamic Either result from reflection invocation.</param>
    /// <param name="responseType">The response type for serialization.</param>
    /// <param name="contextTypeName">The type name for error context (request or notification type).</param>
    /// <returns>Either an error or the serialized response bytes.</returns>
    /// <exception cref="GrpcSerializationException">
    /// Thrown when serialization of the response fails. This exception wraps the underlying
    /// <see cref="JsonException"/> to distinguish serialization errors from deserialization errors.
    /// </exception>
    private static Either<EncinaError, byte[]> ExtractEitherResult(
        dynamic result,
        Type responseType,
        string contextTypeName)
    {
        if ((bool)result.IsRight)
        {
            object? responseValue = null;

            // The cast to Action<object> is safe due to contravariance: Either<L,R>.IfRight expects
            // an Action<R>, and since any R can be assigned to object, Action<object> is a valid
            // contravariant substitution. This allows us to capture the strongly-typed response
            // value without knowing R at compile time when working with dynamic Either instances.
            result.IfRight((Action<object>)(r => responseValue = r));

            // Validate that the response is not null before serialization.
            // A null response from IEncina.Send indicates an unexpected state.
            if (responseValue is null) // NOSONAR S2583: IfRight lambda assignment not tracked by analyzer
            {
                return Left<EncinaError, byte[]>(
                    EncinaErrors.Create(
                        "GRPC_NULL_RESPONSE",
                        $"The response for request '{contextTypeName}' was null. " +
                        $"Expected a non-null value of type '{responseType.Name}'."));
            }

            try
            {
                var responseBytes = JsonSerializer.SerializeToUtf8Bytes(responseValue, responseType);
                return Right<EncinaError, byte[]>(responseBytes);
            }
            catch (JsonException ex)
            {
                // Wrap in a custom exception to distinguish from deserialization errors
                throw new GrpcSerializationException(
                    $"Failed to serialize response of type '{responseType.Name}' for request '{contextTypeName}'.",
                    ex);
            }
        }

        EncinaError? error = null;
        result.IfLeft((Action<EncinaError>)(e => error = e));

        if (error is null) // NOSONAR S2583: IfLeft lambda assignment not tracked by analyzer
        {
            return Left<EncinaError, byte[]>(
                EncinaErrors.Create(
                    "GRPC_ERROR_EXTRACTION_FAILED",
                    $"Failed to extract error from Either.Left for type '{contextTypeName}'."));
        }

        return Left<EncinaError, byte[]>(error);
    }
}

/// <summary>
/// Exception thrown when serialization of a gRPC response fails.
/// Used to distinguish serialization errors from deserialization errors.
/// </summary>
public sealed class GrpcSerializationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcSerializationException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The JSON exception that caused this exception.</param>
    public GrpcSerializationException(string message, JsonException innerException)
        : base(message, innerException)
    {
    }
}
