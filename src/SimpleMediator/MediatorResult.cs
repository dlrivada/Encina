using System;
using LanguageExt;

namespace SimpleMediator;

/// <summary>
/// Factoría de errores estándar producidos por SimpleMediator.
/// </summary>
internal static class MediatorErrors
{
    /// <summary>
    /// Error inesperado de infraestructura.
    /// </summary>
    public static Error Unknown { get; } = Create("mediator.unknown", "Se produjo un error inesperado en SimpleMediator.");

    /// <summary>
    /// Crea un error con código y mensaje explícitos.
    /// </summary>
    public static Error Create(string code, string message, Exception? exception = null, object? details = null)
        => Error.FromMediatorException(new MediatorException(code, message, exception, details));

    /// <summary>
    /// Envuelve una excepción en un error tipado.
    /// </summary>
    public static Error FromException(string code, Exception exception, string? message = null, object? details = null)
        => Create(code, message ?? exception.Message, exception, details);
}

/// <summary>
/// Excepción interna utilizada para conservar metadatos de fallos del mediador sin propagarlos.
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
/// Extensiones auxiliares para extraer metadatos de <see cref="Error"/>.
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
