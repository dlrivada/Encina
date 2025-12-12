using System.Collections.Generic;
using LanguageExt;
using static LanguageExt.Prelude;

namespace SimpleMediator;

/// <summary>
/// Guard clauses for request and notification validation in the mediator.
/// </summary>
internal static class MediatorRequestGuards
{
    /// <summary>
    /// Validates that a request is not null and returns a functional error if it is.
    /// </summary>
    /// <typeparam name="TResponse">The response type expected from the request.</typeparam>
    /// <param name="request">The request instance to validate.</param>
    /// <param name="error">The error to return if validation fails.</param>
    /// <returns>True if the request is valid; otherwise, false.</returns>
    public static bool TryValidateRequest<TResponse>(object? request, out Either<MediatorError, TResponse> error)
    {
        if (request is not null)
        {
            error = default;
            return true;
        }

        const string message = "The request cannot be null.";
        error = Left<MediatorError, TResponse>(MediatorErrors.Create(MediatorErrorCodes.RequestNull, message));
        return false;
    }

    /// <summary>
    /// Validates that a notification is not null and returns a functional error if it is.
    /// </summary>
    /// <param name="notification">The notification instance to validate.</param>
    /// <param name="error">The error to return if validation fails.</param>
    /// <returns>True if the notification is valid; otherwise, false.</returns>
    public static bool TryValidateNotification(object? notification, out Either<MediatorError, Unit> error)
    {
        if (notification is not null)
        {
            error = default;
            return true;
        }

        const string message = "The notification cannot be null.";
        error = Left<MediatorError, Unit>(MediatorErrors.Create(MediatorErrorCodes.NotificationNull, message));
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
        out Either<MediatorError, TResponse> error)
    {
        if (handler is not null)
        {
            error = default;
            return true;
        }

        var message = $"No registered IRequestHandler was found for {requestType.Name} -> {responseType.Name}.";
        var metadata = new Dictionary<string, object?>
        {
            ["requestType"] = requestType.FullName,
            ["responseType"] = responseType.FullName,
            ["stage"] = "handler_resolution"
        };
        error = Left<MediatorError, TResponse>(MediatorErrors.Create(MediatorErrorCodes.RequestHandlerMissing, message, details: metadata));
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
        out Either<MediatorError, TResponse> error)
    {
        if (expectedType.IsInstanceOfType(handler))
        {
            error = default;
            return true;
        }

        var message = $"Handler {handler.GetType().Name} does not implement {expectedType.Name} for {requestType.Name}.";
        var metadata = new Dictionary<string, object?>
        {
            ["handlerType"] = handler.GetType().FullName,
            ["expectedType"] = expectedType.FullName,
            ["requestType"] = requestType.FullName,
            ["stage"] = "handler_validation"
        };
        error = Left<MediatorError, TResponse>(MediatorErrors.Create(MediatorErrorCodes.RequestHandlerTypeMismatch, message, details: metadata));
        return false;
    }
}
