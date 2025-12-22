namespace Encina.Refit;

/// <summary>
/// Marker interface for requests that should be handled by Refit HTTP clients.
/// </summary>
/// <typeparam name="TApiClient">The Refit API client interface type.</typeparam>
/// <typeparam name="TResponse">The type of response expected from the API call.</typeparam>
/// <remarks>
/// Implement this interface in combination with <see cref="IRequest{TResponse}"/> to create
/// requests that are automatically routed to Refit-generated HTTP clients.
///
/// The handler will automatically use the registered Refit client interface for the
/// specified <typeparamref name="TApiClient"/> type.
/// </remarks>
/// <example>
/// <code>
/// // Define your Refit API interface
/// public interface IGitHubApi
/// {
///     [Get("/users/{username}")]
///     Task&lt;GitHubUser&gt; GetUserAsync(string username);
/// }
///
/// // Create a request that uses the Refit client
/// public record GetGitHubUserRequest(string Username)
///     : IRequest&lt;GitHubUser&gt;, IRestApiRequest&lt;IGitHubApi, GitHubUser&gt;
/// {
///     public async Task&lt;GitHubUser&gt; ExecuteAsync(
///         IGitHubApi api,
///         CancellationToken cancellationToken)
///     {
///         return await api.GetUserAsync(Username);
///     }
/// }
///
/// // Register the Refit client
/// services.AddRefitClient&lt;IGitHubApi&gt;()
///     .ConfigureHttpClient(c =&gt; c.BaseAddress = new Uri("https://api.github.com"));
///
/// // Use through Encina
/// var user = await mediator.Send(new GetGitHubUserRequest("octocat"));
/// </code>
/// </example>
public interface IRestApiRequest<TApiClient, TResponse> : IRequest<TResponse>
    where TApiClient : class
{
    /// <summary>
    /// Executes the API call using the provided Refit client.
    /// </summary>
    /// <param name="apiClient">The Refit-generated API client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The API response.</returns>
    Task<TResponse> ExecuteAsync(TApiClient apiClient, CancellationToken cancellationToken);
}
