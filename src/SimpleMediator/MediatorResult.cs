using System;
using LanguageExt;

namespace SimpleMediator;

/// <summary>
/// Factory for the standard errors produced by SimpleMediator.
/// </summary>
internal static class MediatorErrors
{
    /// <summary>
    /// Unexpected infrastructure error.
    /// </summary>
    public static Error Unknown { get; } = Create("mediator.unknown", "An unexpected error occurred in SimpleMediator.");

    /// <summary>
    /// Creates an error with explicit code and message.
    /// </summary>
    public static Error Create(string code, string message, Exception? exception = null, object? details = null)
        => Error.FromMediatorException(new MediatorException(code, message, exception, details));

    /// <summary>
    /// Wraps an exception inside a typed error.
    /// </summary>
    public static Error FromException(string code, Exception exception, string? message = null, object? details = null)
        => Create(code, message ?? exception.Message, exception, details);
}

/// <summary>
/// Internal exception used to capture mediator failure metadata without leaking it.
/// </summary>
internal sealed class MediatorException : Exception
{
    public MediatorException(string code, string message, Exception? innerException, object? details)
        : base(message, innerException)
    {
        Code = code;
        Details = details;
    }

    public string Code { get; }

    public object? Details { get; }
}

/// <summary>
/// Helper extensions to extract metadata from <see cref="Error"/>.
/// </summary>
internal static class MediatorErrorExtensions
{
    public static string GetMediatorCode(this Error error)
    {
        return error.MetadataException.Match(
            Some: ex => ex switch
            {
                MediatorException mediatorException => mediatorException.Code,
                _ => ex.GetType().Name
            },
            None: () => string.IsNullOrWhiteSpace(error.Message) ? "mediator.unknown" : error.Message);
    }

    public static object? GetMediatorDetails(this Error error)
    {
        return error.MetadataException.MatchUnsafe(
            ex => ex switch
            {
                MediatorException mediatorException => mediatorException.Details,
                _ => null
            },
            () => null);
    }
}
