using Dapr.Client;

namespace Encina.Dapr;

/// <summary>
/// Marker interface for requests that interact with Dapr State Management.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <remarks>
/// Dapr State Management provides:
/// - Key/value state storage (Redis, Cosmos DB, SQL Server, etc.)
/// - Strong and eventual consistency options
/// - Bulk operations (get/set/delete multiple keys)
/// - Transaction support (multi-item ACID operations)
/// - TTL (time-to-live) for automatic expiration
/// - ETag-based concurrency control
///
/// Returns <typeparamref name="TResponse"/> from the state operation in Railway Oriented Programming.
/// </remarks>
/// <example>
/// <code>
/// // Save state
/// public record SaveUserPreferencesCommand(string UserId, UserPreferences Preferences)
///     : IRequest&lt;Unit&gt;, IDaprStateRequest&lt;Unit&gt;
/// {
///     public string StoreName => "statestore";
///     public string StateKey => $"user-preferences-{UserId}";
///
///     public async Task&lt;Unit&gt; ExecuteAsync(
///         DaprClient daprClient,
///         CancellationToken cancellationToken)
///     {
///         await daprClient.SaveStateAsync(
///             StoreName,
///             StateKey,
///             Preferences,
///             cancellationToken: cancellationToken);
///         return Unit.Default;
///     }
/// }
///
/// // Get state
/// public record GetUserPreferencesQuery(string UserId)
///     : IQuery&lt;UserPreferences&gt;, IDaprStateRequest&lt;UserPreferences&gt;
/// {
///     public string StoreName => "statestore";
///     public string StateKey => $"user-preferences-{UserId}";
///
///     public async Task&lt;UserPreferences&gt; ExecuteAsync(
///         DaprClient daprClient,
///         CancellationToken cancellationToken)
///     {
///         return await daprClient.GetStateAsync&lt;UserPreferences&gt;(
///             StoreName,
///             StateKey,
///             cancellationToken: cancellationToken);
///     }
/// }
///
/// // Use through Encina
/// var result = await mediator.Send(new SaveUserPreferencesCommand(userId, prefs));
/// result.Match(
///     Right: _ => Console.WriteLine("Preferences saved"),
///     Left: error => Console.WriteLine($"Error: {error.Message}")
/// );
/// </code>
/// </example>
public interface IDaprStateRequest<TResponse> : IRequest<TResponse>
{
    /// <summary>
    /// Gets the name of the Dapr state store component.
    /// </summary>
    string StoreName { get; }

    /// <summary>
    /// Gets the state key to operate on.
    /// </summary>
    string StateKey { get; }

    /// <summary>
    /// Executes the state operation using the provided Dapr client.
    /// </summary>
    /// <param name="daprClient">The Dapr client instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the response.</returns>
    Task<TResponse> ExecuteAsync(DaprClient daprClient, CancellationToken cancellationToken);
}
