using System.Collections.Generic;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina;

/// <summary>
/// Guard clauses for request and notification validation in the Encina.
/// </summary>
internal static class EncinaRequestGuards
{
    /// <summary>
    /// Validates that a request is not null and returns a functional error if it is.
    /// </summary>
    /// <typeparam name="TResponse">The response type expected from the request.</typeparam>
    /// <param name="request">The request instance to validate.</param>
    /// <param name="error">The error to return if validation fails.</param>
    /// <returns>True if the request is valid; otherwise, false.</returns>
    public static bool TryValidateRequest<TResponse>(object? request, out Either<EncinaError, TResponse> error)
    {
        if (request is not null)
        {
            error = default;
            return true;
        }

        const string message = "The request cannot be null.";
        error = Left<EncinaError, TResponse>(EncinaErrors.Create(EncinaErrorCodes.RequestNull, message));
        return false;
    }

    /// <summary>
    /// Validates that a notification is not null and returns a functional error if it is.
    /// </summary>
    /// <param name="notification">The notification instance to validate.</param>
    /// <param name="error">The error to return if validation fails.</param>
    /// <returns>True if the notification is valid; otherwise, false.</returns>
    public static bool TryValidateNotification(object? notification, out Either<EncinaError, Unit> error)
    {
        if (notification is not null)
        {
            error = default;
            return true;
        }

        const string message = "The notification cannot be null.";
        error = Left<EncinaError, Unit>(EncinaErrors.Create(EncinaErrorCodes.NotificationNull, message));
        return false;
    }

    /// <summary>
    /// Validates that a handler was resolved from DI and returns a functional error if it wasn't.
    /// </summary>
    /// <typeparam name="TResponse">The response type expected from the handler.</typeparam>
    /// <param name="handler">The handler instance to validate.</param>
    /// <param name="requestType">The type of the request being handled.</param>
    /// <param name="responseType">The type of the expected response.</param>
    /// <param name="error">The error to return if validation fails.</param>
    /// <returns>True if a handler was found; otherwise, false.</returns>
    public static bool TryValidateHandler<TResponse>(
        object? handler,
        Type requestType,
        Type responseType,
        out Either<EncinaError, TResponse> error)
    {
        if (handler is not null)
        {
            error = default;
            return true;
        }

        string message = $"No registered IRequestHandler was found for {requestType.Name} -> {responseType.Name}.";
        var metadata = new Dictionary<string, object?>
        {
            ["requestType"] = requestType.FullName,
            ["responseType"] = responseType.FullName,
            ["stage"] = "handler_resolution"
        };
        error = Left<EncinaError, TResponse>(EncinaErrors.Create(EncinaErrorCodes.RequestHandlerMissing, message, details: metadata));
        return false;
    }

    /// <summary>
    /// Validates that the handler is of the expected type.
    /// </summary>
    /// <typeparam name="TResponse">The response type expected from the handler.</typeparam>
    /// <param name="handler">The handler instance to validate.</param>
    /// <param name="expectedType">The expected handler type.</param>
    /// <param name="requestType">The type of the request being handled.</param>
    /// <param name="error">The error to return if validation fails.</param>
    /// <returns>True if the handler is of the expected type; otherwise, false.</returns>
    public static bool TryValidateHandlerType<TResponse>(
        object handler,
        Type expectedType,
        Type requestType,
        out Either<EncinaError, TResponse> error)
    {
        if (expectedType.IsInstanceOfType(handler))
        {
            error = default;
            return true;
        }

        string message = $"Handler {handler.GetType().Name} does not implement {expectedType.Name} for {requestType.Name}.";
        var metadata = new Dictionary<string, object?>
        {
            ["handlerType"] = handler.GetType().FullName,
            ["expectedType"] = expectedType.FullName,
            ["requestType"] = requestType.FullName,
            ["stage"] = "handler_validation"
        };
        error = Left<EncinaError, TResponse>(EncinaErrors.Create(EncinaErrorCodes.RequestHandlerTypeMismatch, message, details: metadata));
        return false;
    }

    /// <summary>
    /// Validates that a stream request is not null and returns a functional error if it is.
    /// </summary>
    /// <typeparam name="TItem">The item type yielded by the stream.</typeparam>
    /// <param name="request">The stream request instance to validate.</param>
    /// <param name="error">The error to return if validation fails.</param>
    /// <returns>True if the stream request is valid; otherwise, false.</returns>
    public static bool TryValidateStreamRequest<TItem>(object? request, out Either<EncinaError, TItem> error)
    {
        if (request is not null)
        {
            error = default;
            return true;
        }

        const string message = "The stream request cannot be null.";
        error = Left<EncinaError, TItem>(EncinaErrors.Create(EncinaErrorCodes.RequestNull, message));
        return false;
    }
}
