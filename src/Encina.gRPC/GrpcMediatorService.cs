using System.Text.Json;
using Encina.Diagnostics;
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

            var (plan, error) = PrepareSend(requestType, requestData);
            if (plan is null)
            {
                return Left<EncinaError, byte[]>(error!); // NOSONAR S6966: Left is a pure function
            }

            dynamic result = await InvokeSendAsync(plan, cancellationToken).ConfigureAwait(false);

            return ExtractEitherResult(result, plan.ResponseType, requestType);
        }
        catch (GrpcSerializationException ex)
        {
            Log.FailedToProcessRequest(_logger, ex.ForLogging(), requestType);

            return Left<EncinaError, byte[]>( // NOSONAR S6966: Left is a pure function
                EncinaErrors.FromException(
                    "GRPC_SERIALIZE_FAILED",
                    ex.InnerException ?? ex,
                    $"Failed to serialize response for request of type '{requestType}'."));
        }
        catch (JsonException ex)
        {
            Log.FailedToProcessRequest(_logger, ex.ForLogging(), requestType);

            return Left<EncinaError, byte[]>( // NOSONAR S6966: Left is a pure function
                EncinaErrors.FromException(
                    GrpcDeserializeFailed,
                    ex,
                    $"Failed to deserialize request of type '{requestType}'."));
        }
        catch (Exception ex)
        {
            Log.FailedToProcessRequest(_logger, ex.ForLogging(), requestType);

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

            var (plan, error) = PreparePublish(notificationType, notificationData);
            if (plan is null)
            {
                return Left<EncinaError, Unit>(error!); // NOSONAR S6966: Left is a pure function
            }

            return await InvokePublishAsync(plan, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            Log.FailedToProcessNotification(_logger, ex.ForLogging(), notificationType);

            return Left<EncinaError, Unit>( // NOSONAR S6966: Left is a pure function
                EncinaErrors.FromException(
                    GrpcDeserializeFailed,
                    ex,
                    $"Failed to deserialize notification of type '{notificationType}'."));
        }
        catch (Exception ex)
        {
            Log.FailedToProcessNotification(_logger, ex.ForLogging(), notificationType);

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

        yield return Left<EncinaError, byte[]>( // NOSONAR S6966: LanguageExt Left is a pure function
            EncinaErrors.Create(
                "GRPC_STREAMING_NOT_IMPLEMENTED",
                "Streaming is not yet implemented."));
    }

    private (DispatchPlan? Plan, EncinaError? Error) PrepareSend(string requestType, byte[] requestData)
    {
        var type = _typeResolver.ResolveRequestType(requestType);
        var (request, error) = DeserializeMessage(
            type,
            requestData,
            $"Request type '{requestType}' not found.",
            "Failed to deserialize request.");

        if (request is null)
        {
            return (null, error);
        }

        // Use reflection to call the generic Send method
        // IEncina.Send<TResponse> has 1 generic parameter (the response type)
        var sendMethod = FindGenericMethod("Send");
        if (sendMethod is null)
        {
            return (null, EncinaErrors.Create(
                "GRPC_SEND_METHOD_NOT_FOUND",
                $"IEncina does not expose a generic Send<TResponse> method. " +
                $"Ensure the IEncina interface defines a Send method with exactly one generic type parameter."));
        }

        var responseType = ResolveResponseType(type!);
        if (responseType is null)
        {
            return (null, EncinaErrors.Create(
                "GRPC_RESPONSE_TYPE_NOT_FOUND",
                $"Could not determine response type for request '{requestType}'."));
        }

        return (new DispatchPlan(request, sendMethod, responseType), null);
    }

    private (DispatchPlan? Plan, EncinaError? Error) PreparePublish(string notificationType, byte[] notificationData)
    {
        var type = _typeResolver.ResolveNotificationType(notificationType);
        var (notification, error) = DeserializeMessage(
            type,
            notificationData,
            $"Notification type '{notificationType}' not found.",
            "Failed to deserialize notification.");

        if (notification is null)
        {
            return (null, error);
        }

        // Use reflection to call the generic Publish method
        // IEncina.Publish<TNotification> returns ValueTask<Either<EncinaError, Unit>>
        var publishMethod = FindGenericMethod("Publish");
        if (publishMethod is null)
        {
            return (null, EncinaErrors.Create(
                "GRPC_PUBLISH_METHOD_NOT_FOUND",
                $"IEncina does not expose a generic Publish<TNotification> method. " +
                $"Ensure the IEncina interface defines a Publish method with exactly one generic type parameter."));
        }

        return (new DispatchPlan(notification, publishMethod, type!), null);
    }

    private static (object? Message, EncinaError? Error) DeserializeMessage(
        Type? type,
        byte[] data,
        string notFoundMessage,
        string deserializeFailedMessage)
    {
        if (type is null)
        {
            return (null, EncinaErrors.Create(GrpcTypeNotFound, notFoundMessage));
        }

        var message = JsonSerializer.Deserialize(data, type);

        return message is null
            ? (null, EncinaErrors.Create(GrpcDeserializeFailed, deserializeFailedMessage))
            : (message, null);
    }

    private static System.Reflection.MethodInfo? FindGenericMethod(string name) =>
        typeof(IEncina)
            .GetMethods()
            .FirstOrDefault(m => m.Name == name && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 2);

    private static Type? ResolveResponseType(Type requestType) =>
        requestType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
            ?.GetGenericArguments()[0];

    private async Task<dynamic> InvokeSendAsync(DispatchPlan plan, CancellationToken cancellationToken)
    {
        var task = (dynamic)InvokeGeneric(plan, "Send", cancellationToken);
        return await task;
    }

    private async ValueTask<Either<EncinaError, Unit>> InvokePublishAsync(DispatchPlan plan, CancellationToken cancellationToken)
    {
        var task = (dynamic)InvokeGeneric(plan, "Publish", cancellationToken);
        Either<EncinaError, Unit> result = await task;

        return result;
    }

    private object InvokeGeneric(DispatchPlan plan, string methodName, CancellationToken cancellationToken)
    {
        var genericMethod = plan.OpenMethod.MakeGenericMethod(plan.GenericArgument);

        return genericMethod.Invoke(_encina, [plan.Message, cancellationToken])
            ?? throw new InvalidOperationException(
                $"IEncina.{methodName}<{plan.GenericArgument.Name}> returned null. " +
                $"The method must return a non-null ValueTask.");
    }

    private sealed record DispatchPlan(object Message, System.Reflection.MethodInfo OpenMethod, Type GenericArgument)
    {
        public Type ResponseType => GenericArgument;
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
                return Left<EncinaError, byte[]>( // NOSONAR S6966: LanguageExt Left is a pure function
                    EncinaErrors.Create(
                        "GRPC_NULL_RESPONSE",
                        $"The response for request '{contextTypeName}' was null. " +
                        $"Expected a non-null value of type '{responseType.Name}'."));
            }

            try
            {
                var responseBytes = JsonSerializer.SerializeToUtf8Bytes(responseValue, responseType);
                return Right<EncinaError, byte[]>(responseBytes); // NOSONAR S6966: LanguageExt Right is a pure function
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
            return Left<EncinaError, byte[]>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.Create(
                    "GRPC_ERROR_EXTRACTION_FAILED",
                    $"Failed to extract error from Either.Left for type '{contextTypeName}'."));
        }

        return Left<EncinaError, byte[]>(error); // NOSONAR S6966: LanguageExt Left is a pure function
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
