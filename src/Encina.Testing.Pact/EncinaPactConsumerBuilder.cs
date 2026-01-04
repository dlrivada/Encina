using System.Net;
using System.Text.Json;
using LanguageExt;
using PactNet;
using PactNet.Infrastructure.Outputters;

namespace Encina.Testing.Pact;

/// <summary>
/// Fluent builder for defining consumer-side Pact expectations for Encina requests.
/// </summary>
/// <remarks>
/// <para>
/// This builder simplifies defining Pact interactions for Encina commands, queries, and notifications.
/// It integrates with PactNet v4+ and generates Pact JSON files that can be verified against providers.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var pact = new EncinaPactConsumerBuilder("OrdersWebApp", "OrdersAPI")
///     .WithCommandExpectation(
///         new CreateOrderCommand(orderId),
///         Either&lt;EncinaError, OrderDto&gt;.Right(new OrderDto { Id = orderId }),
///         description: "Create a new order")
///     .WithQueryExpectation(
///         new GetOrderByIdQuery(orderId),
///         Either&lt;EncinaError, OrderDto&gt;.Right(new OrderDto { Id = orderId }),
///         description: "Get existing order");
/// 
/// await pact.VerifyAsync(async uri => {
///     // Test your client code using the mock server at 'uri'
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class EncinaPactConsumerBuilder : IDisposable
{
    // Error code prefixes for HTTP status mapping
    private const string ErrorPrefixValidation = "encina.validation";
    private const string ErrorPrefixAuthorization = "encina.authorization";
    private const string ErrorPrefixAuthentication = "encina.authentication";
    private const string ErrorPrefixNotFound = "encina.notfound";
    private const string ErrorPrefixConflict = "encina.conflict";
    private const string ErrorPrefixTimeout = "encina.timeout";
    private const string ErrorPrefixRateLimit = "encina.ratelimit";

    private readonly IPactBuilderV4 _pactBuilder;
    private readonly List<PactInteraction> _interactions = [];
    private readonly object _configurationLock = new();
    private Uri? _mockServerUri;
    private int _interactionsConfigured;
    private bool _disposed;

    /// <summary>
    /// Gets the consumer name for this Pact.
    /// </summary>
    public string ConsumerName { get; }

    /// <summary>
    /// Gets the provider name for this Pact.
    /// </summary>
    public string ProviderName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaPactConsumerBuilder"/> class.
    /// </summary>
    /// <param name="consumerName">Name of the consumer (your service).</param>
    /// <param name="providerName">Name of the provider (the service you depend on).</param>
    /// <param name="pactDirectory">Directory to write Pact files. Defaults to ./pacts.</param>
    /// <param name="outputWriter">Optional xUnit output helper for logging.</param>
    /// <exception cref="ArgumentException">Thrown when consumer or provider name is null/empty.</exception>
    public EncinaPactConsumerBuilder(
        string consumerName,
        string providerName,
        string pactDirectory = "./pacts",
        Xunit.Abstractions.ITestOutputHelper? outputWriter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(pactDirectory);

        ConsumerName = consumerName;
        ProviderName = providerName;

        var pactConfig = new PactConfig
        {
            PactDir = pactDirectory,
            DefaultJsonSettings = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }
        };

        if (outputWriter is not null)
        {
            pactConfig.Outputters = [new XunitOutputAdapter(outputWriter)];
        }

        var pact = PactNet.Pact.V4(consumerName, providerName, pactConfig);
        _pactBuilder = pact.WithHttpInteractions();
    }

    /// <summary>
    /// Defines an expectation for a command request.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="command">The command instance.</param>
    /// <param name="expectedResponse">The expected Either result.</param>
    /// <param name="description">Optional description for the interaction.</param>
    /// <param name="providerState">Optional provider state required for this interaction.</param>
    /// <returns>The builder for fluent chaining.</returns>
    public EncinaPactConsumerBuilder WithCommandExpectation<TCommand, TResponse>(
        TCommand command,
        Either<EncinaError, TResponse> expectedResponse,
        string? description = null,
        string? providerState = null)
        where TCommand : ICommand<TResponse>
        where TResponse : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(command);

        var requestTypeName = typeof(TCommand).Name;
        var path = $"/api/commands/{requestTypeName}";
        var desc = description ?? $"Command: {requestTypeName}";

        AddInteraction(
            desc,
            HttpMethod.Post,
            path,
            command,
            expectedResponse,
            providerState,
            InteractionType.Command);

        return this;
    }

    /// <summary>
    /// Defines an expectation for a query request.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="query">The query instance.</param>
    /// <param name="expectedResponse">The expected Either result.</param>
    /// <param name="description">Optional description for the interaction.</param>
    /// <param name="providerState">Optional provider state required for this interaction.</param>
    /// <returns>The builder for fluent chaining.</returns>
    public EncinaPactConsumerBuilder WithQueryExpectation<TQuery, TResponse>(
        TQuery query,
        Either<EncinaError, TResponse> expectedResponse,
        string? description = null,
        string? providerState = null)
        where TQuery : IQuery<TResponse>
        where TResponse : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(query);

        var requestTypeName = typeof(TQuery).Name;
        var path = $"/api/queries/{requestTypeName}";
        var desc = description ?? $"Query: {requestTypeName}";

        AddInteraction(
            desc,
            HttpMethod.Post,
            path,
            query,
            expectedResponse,
            providerState,
            InteractionType.Query);

        return this;
    }

    /// <summary>
    /// Defines an expectation for a notification (async message).
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="notification">The notification instance.</param>
    /// <param name="description">Optional description for the interaction.</param>
    /// <param name="providerState">Optional provider state required for this interaction.</param>
    /// <returns>The builder for fluent chaining.</returns>
    public EncinaPactConsumerBuilder WithNotificationExpectation<TNotification>(
        TNotification notification,
        string? description = null,
        string? providerState = null)
        where TNotification : INotification
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = typeof(TNotification).Name;
        var path = $"/api/notifications/{notificationType}";
        var desc = description ?? $"Notification: {notificationType}";

        AddInteraction(
            desc,
            HttpMethod.Post,
            path,
            notification,
            Either<EncinaError, object>.Right(new PactNotificationResponse(Received: true)),
            providerState,
            InteractionType.Notification);

        return this;
    }

    /// <summary>
    /// Defines an expectation for a command that should fail with a specific error.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The expected response type (unused since it fails).</typeparam>
    /// <param name="command">The command instance.</param>
    /// <param name="expectedError">The expected error.</param>
    /// <param name="description">Optional description for the interaction.</param>
    /// <param name="providerState">Optional provider state required for this interaction.</param>
    /// <returns>The builder for fluent chaining.</returns>
    public EncinaPactConsumerBuilder WithCommandFailureExpectation<TCommand, TResponse>(
        TCommand command,
        EncinaError expectedError,
        string? description = null,
        string? providerState = null)
        where TCommand : ICommand<TResponse>
        where TResponse : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(expectedError);

        var requestTypeName = typeof(TCommand).Name;
        var path = $"/api/commands/{requestTypeName}";
        var desc = description ?? $"Command Failure: {requestTypeName}";

        AddInteraction(
            desc,
            HttpMethod.Post,
            path,
            command,
            Either<EncinaError, TResponse>.Left(expectedError),
            providerState,
            InteractionType.Command);

        return this;
    }

    /// <summary>
    /// Defines an expectation for a query that should fail with a specific error.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The expected response type (unused since it fails).</typeparam>
    /// <param name="query">The query instance.</param>
    /// <param name="expectedError">The expected error.</param>
    /// <param name="description">Optional description for the interaction.</param>
    /// <param name="providerState">Optional provider state required for this interaction.</param>
    /// <returns>The builder for fluent chaining.</returns>
    public EncinaPactConsumerBuilder WithQueryFailureExpectation<TQuery, TResponse>(
        TQuery query,
        EncinaError expectedError,
        string? description = null,
        string? providerState = null)
        where TQuery : IQuery<TResponse>
        where TResponse : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(expectedError);

        var requestTypeName = typeof(TQuery).Name;
        var path = $"/api/queries/{requestTypeName}";
        var desc = description ?? $"Query Failure: {requestTypeName}";

        AddInteraction(
            desc,
            HttpMethod.Post,
            path,
            query,
            Either<EncinaError, TResponse>.Left(expectedError),
            providerState,
            InteractionType.Query);

        return this;
    }

    /// <summary>
    /// Gets the mock server base URL for testing.
    /// </summary>
    /// <returns>The base URL of the mock server.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the mock server has not been started. Call <see cref="VerifyAsync"/> first.
    /// </exception>
    /// <remarks>
    /// The mock server URI is only available after calling <see cref="VerifyAsync"/>.
    /// The URI is dynamically assigned by PactNet and cannot be predetermined.
    /// </remarks>
    public Uri GetMockServerUri()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_mockServerUri is null)
        {
            throw new InvalidOperationException(
                "Mock server has not been started. Call VerifyAsync() to start the mock server and execute tests.");
        }

        return _mockServerUri;
    }

    /// <summary>
    /// Starts the mock server, executes the test action, and verifies the interactions.
    /// </summary>
    /// <param name="testAction">The async test action to execute against the mock server.</param>
    /// <returns>A task representing the async verification.</returns>
    /// <remarks>
    /// This method configures all defined interactions, starts the PactNet mock server,
    /// and executes your test code with the dynamically assigned mock server URI.
    /// </remarks>
    public async Task VerifyAsync(Func<Uri, Task> testAction)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(testAction);

        ConfigureInteractionsOnce();

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            _mockServerUri = ctx.MockServerUri;
            await testAction(ctx.MockServerUri);
        });
    }

    /// <summary>
    /// Starts the mock server, executes the test action, and verifies the interactions.
    /// </summary>
    /// <param name="testAction">The synchronous test action to execute against the mock server.</param>
    /// <remarks>
    /// This method configures all defined interactions, starts the PactNet mock server,
    /// and executes your test code with the dynamically assigned mock server URI.
    /// </remarks>
    public void Verify(Action<Uri> testAction)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(testAction);

        ConfigureInteractionsOnce();

        _pactBuilder.Verify(ctx =>
        {
            _mockServerUri = ctx.MockServerUri;
            testAction(ctx.MockServerUri);
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Note: IPactBuilderV4 does not implement IDisposable.
        // Pact files are written when Verify/VerifyAsync completes successfully.
        _interactions.Clear();
        _disposed = true;
    }

    private void ConfigureInteractionsOnce()
    {
        if (Volatile.Read(ref _interactionsConfigured) == 1)
        {
            return;
        }

        lock (_configurationLock)
        {
            if (_interactionsConfigured == 1)
            {
                return;
            }

            foreach (var interaction in _interactions)
            {
                ConfigureInteraction(interaction);
            }

            Volatile.Write(ref _interactionsConfigured, 1);
        }
    }

    private void AddInteraction<TRequest, TResponse>(
        string description,
        HttpMethod method,
        string path,
        TRequest request,
        Either<EncinaError, TResponse> expectedResponse,
        string? providerState,
        InteractionType interactionType)
        where TRequest : notnull
        where TResponse : notnull
    {
        var interaction = new PactInteraction(
            description,
            method,
            path,
            request,
            expectedResponse.IsRight,
            expectedResponse.Match<object>(
                Right: r => r,
                Left: e => SerializeError(e)),
            expectedResponse.Match(
                Right: _ => HttpStatusCode.OK,
                Left: e => MapErrorToStatusCode(e)),
            providerState,
            interactionType,
            typeof(TRequest).Name);

        _interactions.Add(interaction);
    }

    private void ConfigureInteraction(PactInteraction interaction)
    {
        var builder = _pactBuilder.UponReceiving(interaction.Description);

        if (!string.IsNullOrEmpty(interaction.ProviderState))
        {
            builder.Given(interaction.ProviderState);
        }

        builder
            .WithRequest(interaction.Method, interaction.Path)
            .WithHeader("Content-Type", "application/json")
            .WithJsonBody(interaction.RequestBody);

        builder.WillRespond()
            .WithStatus(interaction.StatusCode)
            .WithHeader("Content-Type", "application/json")
            .WithJsonBody(interaction.ResponseBody);
    }

    private static PactErrorResponse SerializeError(EncinaError error)
    {
        var code = error.GetCode().IfNone("encina.unknown");

        // IsSuccess is always false for error responses.
        // It's included for symmetry with PactSuccessResponse in the JSON contract.
        return new PactErrorResponse(
            IsSuccess: false,
            ErrorCode: code,
            ErrorMessage: error.Message);
    }

    private static HttpStatusCode MapErrorToStatusCode(EncinaError error)
    {
        var code = error.GetCode().IfNone("encina.unknown");

        return code switch
        {
            var c when c.StartsWith(ErrorPrefixValidation, StringComparison.OrdinalIgnoreCase) => HttpStatusCode.BadRequest,
            var c when c.StartsWith(ErrorPrefixAuthorization, StringComparison.OrdinalIgnoreCase) => HttpStatusCode.Forbidden,
            var c when c.StartsWith(ErrorPrefixAuthentication, StringComparison.OrdinalIgnoreCase) => HttpStatusCode.Unauthorized,
            var c when c.StartsWith(ErrorPrefixNotFound, StringComparison.OrdinalIgnoreCase) => HttpStatusCode.NotFound,
            var c when c.StartsWith(ErrorPrefixConflict, StringComparison.OrdinalIgnoreCase) => HttpStatusCode.Conflict,
            var c when c.StartsWith(ErrorPrefixTimeout, StringComparison.OrdinalIgnoreCase) => HttpStatusCode.RequestTimeout,
            var c when c.StartsWith(ErrorPrefixRateLimit, StringComparison.OrdinalIgnoreCase) => HttpStatusCode.TooManyRequests,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private sealed record PactInteraction(
        string Description,
        HttpMethod Method,
        string Path,
        object RequestBody,
        bool IsSuccess,
        object ResponseBody,
        HttpStatusCode StatusCode,
        string? ProviderState,
        InteractionType InteractionType,
        string RequestTypeName);

    private enum InteractionType
    {
        Command,
        Query,
        Notification
    }
}

/// <summary>
/// Standard error response format for Pact contracts.
/// </summary>
/// <remarks>
/// This record represents the JSON structure serialized in Pact error responses.
/// The <see cref="IsSuccess"/> property is always <c>false</c> for error responses,
/// providing symmetry with <see cref="PactSuccessResponse{T}"/> in the JSON contract.
/// Providers can use this property to distinguish between success and error payloads.
/// </remarks>
/// <param name="IsSuccess">Always <c>false</c> for error responses. Included for contract symmetry with success responses.</param>
/// <param name="ErrorCode">The error code identifying the type of error.</param>
/// <param name="ErrorMessage">A human-readable description of the error.</param>
public sealed record PactErrorResponse(
    bool IsSuccess,
    string ErrorCode,
    string ErrorMessage);

/// <summary>
/// Standard notification acknowledgment response for Pact contracts.
/// </summary>
/// <remarks>
/// This record represents the JSON structure serialized in Pact notification responses.
/// It provides a stable, named type for serialization instead of an anonymous type.
/// </remarks>
/// <param name="Received">Indicates whether the notification was successfully received.</param>
public sealed record PactNotificationResponse(bool Received);
