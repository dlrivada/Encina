using Dapr.Client;

namespace Encina.Dapr;

/// <summary>
/// Marker interface for requests that invoke methods on other services via Dapr service invocation.
/// </summary>
/// <typeparam name="TResponse">The type of response expected from the service invocation.</typeparam>
/// <remarks>
/// Dapr service invocation enables reliable service-to-service calls with built-in:
/// - Service discovery
/// - Automatic retries
/// - mTLS encryption
/// - Distributed tracing
/// - Circuit breaking
///
/// This integrates with Encina's Railway Oriented Programming pattern.
/// </remarks>
/// <example>
/// <code>
/// // Define a request to call another service
/// public record GetOrderRequest(string OrderId)
///     : IRequest&lt;Order&gt;, IDaprServiceInvocationRequest&lt;Order&gt;
/// {
///     public string AppId => "order-service";
///     public string MethodName => "orders/get";
///     public HttpMethod HttpMethod => HttpMethod.Get;
///
///     public async Task&lt;Order&gt; InvokeAsync(
///         DaprClient daprClient,
///         CancellationToken cancellationToken)
///     {
///         return await daprClient.InvokeMethodAsync&lt;Order&gt;(
///             AppId,
///             MethodName,
///             cancellationToken);
///     }
/// }
///
/// // Use through Encina
/// var result = await mediator.Send(new GetOrderRequest("12345"));
/// result.Match(
///     Right: order => Console.WriteLine($"Order: {order.Id}"),
///     Left: error => Console.WriteLine($"Error: {error.Message}")
/// );
/// </code>
/// </example>
public interface IDaprServiceInvocationRequest<TResponse> : IRequest<TResponse>
{
    /// <summary>
    /// Gets the Dapr App ID of the target service.
    /// </summary>
    string AppId { get; }

    /// <summary>
    /// Gets the method name to invoke on the target service.
    /// </summary>
    string MethodName { get; }

    /// <summary>
    /// Gets the HTTP method to use for the invocation.
    /// </summary>
    HttpMethod HttpMethod { get; }

    /// <summary>
    /// Executes the service invocation using the provided Dapr client.
    /// </summary>
    /// <param name="daprClient">The Dapr client instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The service response.</returns>
    Task<TResponse> InvokeAsync(DaprClient daprClient, CancellationToken cancellationToken);
}
