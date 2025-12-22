using LanguageExt;

namespace Encina.gRPC;

/// <summary>
/// Interface for exposing Encina operations over gRPC.
/// </summary>
public interface IGrpcMediatorService
{
    /// <summary>
    /// Sends a request and returns the response.
    /// </summary>
    /// <param name="requestType">The fully qualified type name of the request.</param>
    /// <param name="requestData">The serialized request data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or the serialized response.</returns>
    ValueTask<Either<MediatorError, byte[]>> SendAsync(
        string requestType,
        byte[] requestData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a notification.
    /// </summary>
    /// <param name="notificationType">The fully qualified type name of the notification.</param>
    /// <param name="notificationData">The serialized notification data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or Unit on success.</returns>
    ValueTask<Either<MediatorError, Unit>> PublishAsync(
        string notificationType,
        byte[] notificationData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams responses from a request.
    /// </summary>
    /// <param name="requestType">The fully qualified type name of the request.</param>
    /// <param name="requestData">The serialized request data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of serialized responses.</returns>
    IAsyncEnumerable<Either<MediatorError, byte[]>> StreamAsync(
        string requestType,
        byte[] requestData,
        CancellationToken cancellationToken = default);
}
