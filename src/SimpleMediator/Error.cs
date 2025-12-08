using System;
using LanguageExt;
using static LanguageExt.Prelude;

namespace SimpleMediator;

/// <summary>
/// Minimal immutable error representation with optional exception metadata.
/// </summary>
public readonly record struct Error
{
    private static readonly string DefaultMessage = "An error occurred";

    private Error(string message, Option<Exception> exposedException, Option<Exception> metadataException)
    {
        Message = string.IsNullOrWhiteSpace(message) ? DefaultMessage : message;
        Exception = exposedException;
        MetadataException = metadataException;
    }

    /// <summary>
    /// Human-readable description of the failure.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Optional exception information associated with the failure that callers may inspect.
    /// </summary>
    public Option<Exception> Exception { get; }

    /// <summary>
    /// Exception metadata retained for mediator internals (e.g., codes, details).
    /// </summary>
    internal Option<Exception> MetadataException { get; }

    /// <summary>
    /// Creates an error from a message.
    /// </summary>
    public static Error New(string message)
        => new(message, Option<Exception>.None, Option<Exception>.None);

    /// <summary>
    /// Creates an error from a message and optional exception.
    /// </summary>
    public static Error New(string message, Exception? exception)
    {
        if (exception is null)
        {
            return new Error(message, Option<Exception>.None, Option<Exception>.None);
        }

        var actualException = exception ?? throw new ArgumentNullException(nameof(exception));
        return new Error(message, Optional(actualException), Optional(actualException));
    }

    /// <summary>
    /// Creates an error from an exception, preserving the exception message by default.
    /// </summary>
    public static Error New(Exception exception)
    {
        if (exception is null)
        {
            return new Error(DefaultMessage, Option<Exception>.None, Option<Exception>.None);
        }

        return new Error(exception.Message ?? DefaultMessage, Optional(Normalize(exception)), Optional(exception));
    }

    /// <summary>
    /// Creates an error from an exception and explicit message override.
    /// </summary>
    public static Error New(Exception exception, string message)
    {
        if (exception is null)
        {
            return new Error(message, Option<Exception>.None, Option<Exception>.None);
        }

        return new Error(message, Optional(Normalize(exception)), Optional(exception));
    }

    internal static Error FromMediatorException(MediatorException mediatorException)
    {
        var exposed = mediatorException.InnerException is { } inner
            ? Optional(inner)
            : Optional<Exception>(mediatorException);
        return new Error(mediatorException.Message, exposed, Optional<Exception>(mediatorException));
    }

    private static Exception Normalize(Exception exception)
        => exception switch
        {
            MediatorException mediator when mediator.InnerException is { } inner => inner,
            _ => exception
        };

    /// <summary>
    /// Implicit conversion from <see cref="string"/> to <see cref="Error"/>.
    /// </summary>
    public static implicit operator Error(string message) => New(message);

    /// <summary>
    /// Implicit conversion from <see cref="Exception"/> to <see cref="Error"/>.
    /// </summary>
    public static implicit operator Error(Exception exception) => New(exception);
}
