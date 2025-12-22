using Dapr.Client;

namespace Encina.Dapr;

/// <summary>
/// Marker interface for requests that interact with Dapr Input/Output Bindings.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <remarks>
/// Dapr Bindings provide:
/// - Integration with external systems (queues, databases, APIs, etc.)
/// - Input bindings: Trigger handlers from external events (e.g., cron schedules, message queues)
/// - Output bindings: Invoke external systems (e.g., send SMS, email, store in S3)
/// - 100+ pre-built bindings (AWS SQS, Azure Event Hubs, Kafka, Redis, etc.)
/// - Metadata support for binding-specific configuration
///
/// Returns <typeparamref name="TResponse"/> from the binding operation in Railway Oriented Programming.
/// </remarks>
/// <example>
/// <code>
/// // Send email via SendGrid binding
/// public record SendEmailCommand(string To, string Subject, string Body)
///     : ICommand&lt;Unit&gt;, IDaprBindingRequest&lt;Unit&gt;
/// {
///     public string BindingName => "sendgrid";
///     public string Operation => "create"; // Binding-specific operation
///
///     public async Task&lt;Unit&gt; ExecuteAsync(
///         DaprClient daprClient,
///         CancellationToken cancellationToken)
///     {
///         var metadata = new Dictionary&lt;string, string&gt;
///         {
///             ["emailTo"] = To,
///             ["subject"] = Subject
///         };
///
///         await daprClient.InvokeBindingAsync(
///             BindingName,
///             Operation,
///             Body,
///             metadata,
///             cancellationToken);
///
///         return Unit.Default;
///     }
/// }
///
/// // Upload file to Azure Blob Storage binding
/// public record UploadFileCommand(string FileName, byte[] FileContent)
///     : ICommand&lt;Unit&gt;, IDaprBindingRequest&lt;Unit&gt;
/// {
///     public string BindingName => "azure-blob";
///     public string Operation => "create";
///
///     public async Task&lt;Unit&gt; ExecuteAsync(
///         DaprClient daprClient,
///         CancellationToken cancellationToken)
///     {
///         var metadata = new Dictionary&lt;string, string&gt;
///         {
///             ["blobName"] = FileName
///         };
///
///         await daprClient.InvokeBindingAsync(
///             BindingName,
///             Operation,
///             FileContent,
///             metadata,
///             cancellationToken);
///
///         return Unit.Default;
///     }
/// }
///
/// // Use through Encina
/// var result = await mediator.Send(new SendEmailCommand(
///     "user@example.com",
///     "Welcome!",
///     "Thanks for signing up"));
/// result.Match(
///     Right: _ => Console.WriteLine("Email sent"),
///     Left: error => Console.WriteLine($"Error: {error.Message}")
/// );
/// </code>
/// </example>
public interface IDaprBindingRequest<TResponse> : IRequest<TResponse>
{
    /// <summary>
    /// Gets the name of the Dapr binding component.
    /// </summary>
    string BindingName { get; }

    /// <summary>
    /// Gets the operation to perform on the binding.
    /// </summary>
    /// <remarks>
    /// Common operations include:
    /// - "create" - Create/send data to the binding
    /// - "get" - Retrieve data from the binding
    /// - "delete" - Delete data from the binding
    /// - "list" - List items from the binding
    /// Specific operations depend on the binding type.
    /// </remarks>
    string Operation { get; }

    /// <summary>
    /// Executes the binding operation using the provided Dapr client.
    /// </summary>
    /// <param name="daprClient">The Dapr client instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the response.</returns>
    Task<TResponse> ExecuteAsync(DaprClient daprClient, CancellationToken cancellationToken);
}
