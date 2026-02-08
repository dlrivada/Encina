using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.DomainModeling.Pagination;

/// <summary>
/// Extension methods for registering cursor pagination services in an <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// <para>
/// These extensions register the cursor encoder used for encoding and decoding
/// cursor values in cursor-based pagination.
/// </para>
/// <para>
/// By default, <see cref="Base64JsonCursorEncoder"/> is registered, which uses
/// JSON serialization with Base64 URL-safe encoding.
/// </para>
/// </remarks>
public static class CursorPaginationServiceCollectionExtensions
{
    /// <summary>
    /// Adds cursor pagination services to the service collection using the default
    /// <see cref="Base64JsonCursorEncoder"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers <see cref="ICursorEncoder"/> as a singleton using
    /// <see cref="Base64JsonCursorEncoder"/> with default JSON options.
    /// </para>
    /// <para>
    /// The encoder is registered using <c>TryAddSingleton</c>, so if you want
    /// to use a custom encoder, register it before calling this method.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register cursor pagination services
    /// services.AddCursorPagination();
    ///
    /// // Now you can inject ICursorEncoder
    /// public class MyService(ICursorEncoder encoder)
    /// {
    ///     public string EncodeCursor(object value) => encoder.Encode(value)!;
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddCursorPagination(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the default encoder as singleton (stateless, thread-safe)
        services.TryAddSingleton<ICursorEncoder, Base64JsonCursorEncoder>();

        return services;
    }

    /// <summary>
    /// Adds cursor pagination services to the service collection with custom
    /// JSON serialization options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="jsonOptions">The JSON serialization options to use for cursor encoding.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="jsonOptions"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this overload when you need to customize the JSON serialization behavior,
    /// for example to handle custom types or use different naming conventions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var jsonOptions = new JsonSerializerOptions
    /// {
    ///     PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    ///     // ... other options
    /// };
    ///
    /// services.AddCursorPagination(jsonOptions);
    /// </code>
    /// </example>
    public static IServiceCollection AddCursorPagination(
        this IServiceCollection services,
        JsonSerializerOptions jsonOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(jsonOptions);

        // Register with custom options (replaces any existing registration)
        services.AddSingleton<ICursorEncoder>(new Base64JsonCursorEncoder(jsonOptions));

        return services;
    }

    /// <summary>
    /// Adds cursor pagination services to the service collection with a custom encoder.
    /// </summary>
    /// <typeparam name="TEncoder">The type of the custom cursor encoder.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this overload when you want to use a completely custom encoder implementation,
    /// for example one that uses a different serialization format or encryption.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Custom encoder implementation
    /// public class EncryptedCursorEncoder : ICursorEncoder
    /// {
    ///     public string? Encode&lt;T&gt;(T? value) { /* ... */ }
    ///     public T? Decode&lt;T&gt;(string? cursor) { /* ... */ }
    /// }
    ///
    /// // Register the custom encoder
    /// services.AddCursorPagination&lt;EncryptedCursorEncoder&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddCursorPagination<TEncoder>(this IServiceCollection services)
        where TEncoder : class, ICursorEncoder
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the custom encoder as singleton
        services.AddSingleton<ICursorEncoder, TEncoder>();

        return services;
    }
}
