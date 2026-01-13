using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.Testing.Pact;

/// <summary>
/// Verifies Pact contracts against an Encina-based provider implementation.
/// </summary>
/// <remarks>
/// <para>
/// This verifier runs Pact contracts against your actual Encina handlers to ensure
/// your provider implementation satisfies consumer expectations.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var verifier = new EncinaPactProviderVerifier(encina)
///     .WithProviderState("an order exists", async () =>
///     {
///         await SetupOrderInDatabase();
///     })
///     .WithProviderState("no orders exist", async () =>
///     {
///         await ClearOrdersFromDatabase();
///     });
///
/// var result = await verifier.VerifyAsync("./pacts/consumer-provider.json");
/// result.Success.Should().BeTrue();
/// </code>
/// </para>
/// </remarks>
public sealed class EncinaPactProviderVerifier : IDisposable
{
    private static readonly JsonSerializerOptions s_defaultJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Type?> s_requestTypeCache = new(StringComparer.Ordinal);

    private static readonly System.Reflection.MethodInfo s_extractFromEitherMethod =
        typeof(EncinaPactProviderVerifier).GetMethod(
            nameof(ExtractFromEither),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
        ?? throw new InvalidOperationException(
            $"Failed to find method '{nameof(ExtractFromEither)}' via reflection. This is a bug in EncinaPactProviderVerifier.");

    // Cache the generic method definitions for IEncina.Send and IEncina.Publish.
    // We filter by parameter count to be robust against future overloads.
    // Send<TResponse>(IRequest<TResponse>, CancellationToken) - 2 parameters
    // Publish<TNotification>(TNotification, CancellationToken) - 2 parameters
    private static readonly System.Reflection.MethodInfo s_sendMethod =
        typeof(IEncina).GetMethods()
            .FirstOrDefault(m => m.Name == nameof(IEncina.Send)
                && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2)
        ?? throw new InvalidOperationException(
            $"Failed to find method '{nameof(IEncina.Send)}' on {nameof(IEncina)} with expected signature. This is a bug in EncinaPactProviderVerifier.");

    private static readonly System.Reflection.MethodInfo s_publishMethod =
        typeof(IEncina).GetMethods()
            .FirstOrDefault(m => m.Name == nameof(IEncina.Publish)
                && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2)
        ?? throw new InvalidOperationException(
            $"Failed to find method '{nameof(IEncina.Publish)}' on {nameof(IEncina)} with expected signature. This is a bug in EncinaPactProviderVerifier.");

    /// <summary>
    /// Event raised when an assembly type loading warning occurs during type discovery.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subscribe to this event to receive diagnostic information about assembly loading failures
    /// that occur while searching for request types. This is useful for debugging type resolution issues.
    /// </para>
    /// <para>
    /// Since this is an instance event, subscribers are automatically eligible for garbage
    /// collection when the verifier instance is no longer referenced. However, you should
    /// still unsubscribe explicitly if the subscriber has a longer lifetime than the verifier.
    /// </para>
    /// <para>
    /// Example of subscription:
    /// <code>
    /// var verifier = new EncinaPactProviderVerifier(encina);
    /// verifier.OnAssemblyLoadWarning += msg => _testOutput.WriteLine(msg);
    /// 
    /// // ... use the verifier ...
    /// 
    /// // Verifier disposal will make the event unreachable
    /// verifier.Dispose();
    /// </code>
    /// </para>
    /// </remarks>
    public event Action<string>? OnAssemblyLoadWarning;

    private readonly IEncina _encina;
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _ownsServiceProvider;
    private readonly Dictionary<string, Func<Task>> _providerStateHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Func<IDictionary<string, object>, Task>> _providerStateHandlersWithParams = new(StringComparer.OrdinalIgnoreCase);
    private Action<string>? _missingStateHandler;
    private bool _strictProviderStates;
    private bool _disposed;

    /// <summary>
    /// Gets the provider name being verified.
    /// </summary>
    public string? ProviderName { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaPactProviderVerifier"/> class.
    /// </summary>
    /// <param name="encina">The Encina instance with registered handlers.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <exception cref="ArgumentNullException">Thrown when encina or serviceProvider is null.</exception>
    public EncinaPactProviderVerifier(
        IEncina encina,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _encina = encina;
        _serviceProvider = serviceProvider;
        _ownsServiceProvider = false;
    }

    /// <summary>
    /// Initializes a new instance with just the Encina instance for testing purposes.
    /// </summary>
    /// <param name="encina">The Encina instance.</param>
    /// <remarks>
    /// <para>
    /// <strong>Warning:</strong> This constructor creates an empty service provider that cannot
    /// resolve handler dependencies. It is intended only for unit testing scenarios where
    /// handler resolution is not required.
    /// </para>
    /// <para>
    /// For production use, use the <see cref="EncinaPactProviderVerifier(IEncina, IServiceProvider)"/>
    /// constructor to supply a fully configured service provider that can resolve all dependencies.
    /// </para>
    /// </remarks>
    internal EncinaPactProviderVerifier(IEncina encina)
    {
        ArgumentNullException.ThrowIfNull(encina);

        _encina = encina;
        _serviceProvider = new ServiceCollection().BuildServiceProvider();
        _ownsServiceProvider = true;
    }

    /// <summary>
    /// Sets the provider name for verification.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>The verifier for fluent chaining.</returns>
    public EncinaPactProviderVerifier WithProviderName(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ProviderName = providerName;
        return this;
    }

    /// <summary>
    /// Enables strict provider state mode, which throws an exception when a provider state
    /// is requested but no handler is registered.
    /// </summary>
    /// <param name="enabled">Whether strict mode is enabled. Defaults to true.</param>
    /// <returns>The verifier for fluent chaining.</returns>
    /// <remarks>
    /// When strict mode is enabled, verification will fail fast if a Pact interaction
    /// references a provider state that has no registered handler. This helps catch
    /// test configuration errors early.
    /// </remarks>
    public EncinaPactProviderVerifier WithStrictProviderStates(bool enabled = true)
    {
        _strictProviderStates = enabled;
        return this;
    }

    /// <summary>
    /// Registers a callback to be invoked when a provider state handler is not found.
    /// </summary>
    /// <param name="handler">The callback that receives a warning message about the missing handler.</param>
    /// <returns>The verifier for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this to integrate with your logging framework or test output.
    /// The callback receives a descriptive message including the state name and any parameters.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// verifier.OnMissingProviderState(msg => _testOutput.WriteLine(msg));
    /// </code>
    /// </para>
    /// </remarks>
    public EncinaPactProviderVerifier OnMissingProviderState(Action<string> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _missingStateHandler = handler;
        return this;
    }

    /// <summary>
    /// Registers a provider state handler for setting up test conditions.
    /// </summary>
    /// <param name="stateName">The name of the provider state.</param>
    /// <param name="setupAction">The async action to set up the state.</param>
    /// <returns>The verifier for fluent chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when stateName is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when setupAction is null.</exception>
    public EncinaPactProviderVerifier WithProviderState(string stateName, Func<Task> setupAction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateName);
        ArgumentNullException.ThrowIfNull(setupAction);

        _providerStateHandlers[stateName] = setupAction;
        return this;
    }

    /// <summary>
    /// Registers a provider state handler with parameters for setting up test conditions.
    /// </summary>
    /// <param name="stateName">The name of the provider state.</param>
    /// <param name="setupAction">The async action to set up the state, receiving parameters.</param>
    /// <returns>The verifier for fluent chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when stateName is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when setupAction is null.</exception>
    public EncinaPactProviderVerifier WithProviderState(
        string stateName,
        Func<IDictionary<string, object>, Task> setupAction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateName);
        ArgumentNullException.ThrowIfNull(setupAction);

        _providerStateHandlersWithParams[stateName] = setupAction;
        return this;
    }

    /// <summary>
    /// Registers a synchronous provider state handler.
    /// </summary>
    /// <param name="stateName">The name of the provider state.</param>
    /// <param name="setupAction">The action to set up the state.</param>
    /// <returns>The verifier for fluent chaining.</returns>
    public EncinaPactProviderVerifier WithProviderState(string stateName, Action setupAction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateName);
        ArgumentNullException.ThrowIfNull(setupAction);

        _providerStateHandlers[stateName] = () =>
        {
            setupAction();
            return Task.CompletedTask;
        };
        return this;
    }

    /// <summary>
    /// Verifies a Pact file against the provider implementation.
    /// </summary>
    /// <param name="pactFilePath">Path to the Pact JSON file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The verification result.</returns>
    /// <exception cref="ArgumentException">Thrown when pactFilePath is null or empty.</exception>
    public async Task<PactVerificationResult> VerifyAsync(
        string pactFilePath,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(pactFilePath);

        if (!File.Exists(pactFilePath))
        {
            return new PactVerificationResult(
                Success: false,
                Errors: [$"Pact file not found: {pactFilePath}"],
                InteractionResults: []);
        }

        try
        {
            var pactContent = await File.ReadAllTextAsync(pactFilePath, cancellationToken);
            var pact = JsonSerializer.Deserialize<PactFile>(pactContent, s_defaultJsonOptions);

            if (pact is null)
            {
                return new PactVerificationResult(
                    Success: false,
                    Errors: ["Failed to parse Pact file"],
                    InteractionResults: []);
            }

            var results = new List<InteractionVerificationResult>();
            var errors = new List<string>();

            foreach (var interaction in pact.Interactions ?? [])
            {
                var result = await VerifyInteractionAsync(interaction, cancellationToken);
                results.Add(result);

                if (!result.Success)
                {
                    errors.Add($"Interaction '{interaction.Description}' failed: {result.ErrorMessage}");
                }
            }

            return new PactVerificationResult(
                Success: errors.Count == 0,
                Errors: errors,
                InteractionResults: results);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new PactVerificationResult(
                Success: false,
                Errors: [$"Verification error: {ex.Message}"],
                InteractionResults: []);
        }
    }

    private async Task<InteractionVerificationResult> VerifyInteractionAsync(
        PactInteraction interaction,
        CancellationToken cancellationToken)
    {
        var description = interaction.Description ?? "Unknown";

        try
        {
            // Execute provider state setup if defined (supports both v2 and v3+ formats)
            await ExecuteProviderStatesAsync(interaction);

            // Extract request type and category from path
            var path = interaction.Request?.Path;
            if (string.IsNullOrEmpty(path))
            {
                return new InteractionVerificationResult(
                    Description: description,
                    Success: false,
                    ErrorMessage: "Request path is missing");
            }

            var requestCategory = ExtractRequestCategory(path);
            var requestTypeName = ExtractRequestTypeName(path);

            if (string.IsNullOrEmpty(requestTypeName))
            {
                return new InteractionVerificationResult(
                    Description: description,
                    Success: false,
                    ErrorMessage: "Could not determine request type from path");
            }

            // Find the request type in loaded assemblies
            var requestType = FindRequestType(requestTypeName, OnAssemblyLoadWarning);
            if (requestType is null)
            {
                return new InteractionVerificationResult(
                    Description: description,
                    Success: false,
                    ErrorMessage: $"Request type '{requestTypeName}' not found in loaded assemblies");
            }

            // Deserialize the request body
            object? request;
            try
            {
                var bodyJson = interaction.Request?.Body is JsonElement jsonElement
                    ? jsonElement.GetRawText()
                    : JsonSerializer.Serialize(interaction.Request?.Body, s_defaultJsonOptions);

                request = JsonSerializer.Deserialize(bodyJson, requestType, s_defaultJsonOptions);
            }
            catch (JsonException ex)
            {
                return new InteractionVerificationResult(
                    Description: description,
                    Success: false,
                    ErrorMessage: $"Failed to deserialize request body: {ex.Message}");
            }

            if (request is null)
            {
                return new InteractionVerificationResult(
                    Description: description,
                    Success: false,
                    ErrorMessage: "Request body deserialized to null");
            }

            // Invoke the appropriate handler based on request category
            var (actualStatusCode, actualBody) = await InvokeHandlerAsync(request, requestCategory, cancellationToken);

            // Compare actual response with expected
            var expectedStatusCode = interaction.Response?.Status ?? 200;
            if (actualStatusCode != expectedStatusCode)
            {
                return new InteractionVerificationResult(
                    Description: description,
                    Success: false,
                    ErrorMessage: $"Status code mismatch: expected {expectedStatusCode}, got {actualStatusCode}");
            }

            // Compare response bodies if expected body is defined
            if (interaction.Response?.Body is not null)
            {
                var expectedBodyJson = NormalizeJson(interaction.Response.Body);
                var actualBodyJson = NormalizeJson(actualBody);

                if (!JsonBodiesMatch(expectedBodyJson, actualBodyJson))
                {
                    return new InteractionVerificationResult(
                        Description: description,
                        Success: false,
                        ErrorMessage: $"Response body mismatch. Expected: {expectedBodyJson}, Actual: {actualBodyJson}");
                }
            }

            return new InteractionVerificationResult(
                Description: description,
                Success: true,
                ErrorMessage: null);
        }
        catch (Exception ex)
        {
            return new InteractionVerificationResult(
                Description: description,
                Success: false,
                ErrorMessage: ex.Message);
        }
    }

    private async Task<(int StatusCode, object? Body)> InvokeHandlerAsync(
        object request,
        string requestCategory,
        CancellationToken cancellationToken)
    {
        return requestCategory switch
        {
            "commands" or "queries" => await InvokeRequestHandlerAsync(request, cancellationToken),
            "notifications" => await InvokeNotificationHandlerAsync(request, cancellationToken),
            _ => (400, new PactErrorResponse(false, "encina.unknown", $"Unknown request category: {requestCategory}"))
        };
    }

    private async Task<(int StatusCode, object? Body)> InvokeRequestHandlerAsync(
        object request,
        CancellationToken cancellationToken)
    {
        // Find the response type from IRequest<TResponse> interface
        var requestType = request.GetType();
        var requestInterface = requestType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

        if (requestInterface is null)
        {
            return (400, new PactErrorResponse(false, "encina.invalid", $"Type {requestType.Name} does not implement IRequest<TResponse>"));
        }

        var responseType = requestInterface.GetGenericArguments()[0];

        // Call IEncina.Send<TResponse> via reflection using cached MethodInfo
        var sendMethod = s_sendMethod.MakeGenericMethod(responseType);
        var resultTask = (ValueTask<object?>?)sendMethod.Invoke(_encina, new object[] { request, cancellationToken });

        if (resultTask is null)
        {
            return (500, new PactErrorResponse(false, "encina.internal", "Handler invocation returned null"));
        }

        var result = await resultTask.Value;

        // Result is Either<EncinaError, TResponse> - we need to extract it
        return ProcessEitherResult(result, responseType);
    }

    private async Task<(int StatusCode, object? Body)> InvokeNotificationHandlerAsync(
        object notification,
        CancellationToken cancellationToken)
    {
        // Find INotification interface
        var notificationType = notification.GetType();
        if (!typeof(INotification).IsAssignableFrom(notificationType))
        {
            return (400, new PactErrorResponse(false, "encina.invalid", $"Type {notificationType.Name} does not implement INotification"));
        }

        // Call IEncina.Publish<TNotification> via reflection using cached MethodInfo
        var publishMethod = s_publishMethod.MakeGenericMethod(notificationType);
        var resultTask = (ValueTask<Either<EncinaError, Unit>>?)publishMethod.Invoke(_encina, new object[] { notification, cancellationToken });

        if (resultTask is null)
        {
            return (500, new PactErrorResponse(false, "encina.internal", "Publish invocation returned null"));
        }

        var result = await resultTask.Value;

        return result.Match<(int, object?)>(
            Right: _ => (200, new PactNotificationResponse(Received: true)),
            Left: error => (MapErrorToStatusCode(error), SerializeError(error)));
    }

    private static (int StatusCode, object? Body) ProcessEitherResult(object? result, Type responseType)
    {
        if (result is null)
        {
            return (500, new PactErrorResponse(false, "encina.internal", "Handler returned null"));
        }

        // Delegate to a generic helper method that can use LanguageExt's Either<L,R>.Match directly.
        // This requires reflection to construct the generic method at runtime.
        return ProcessEitherResultGeneric(result, responseType);
    }

    private static (int StatusCode, object? Body) ProcessEitherResultGeneric(object result, Type responseType)
    {
        // Use the cached MethodInfo and construct the generic method for the specific response type.
        // This avoids the expensive GetMethod call on every invocation.
        var genericMethod = s_extractFromEitherMethod.MakeGenericMethod(responseType);
        var invokeResult = genericMethod.Invoke(null, new object[] { result });

        if (invokeResult is null)
        {
            throw new InvalidOperationException(
                $"Invocation of '{nameof(ExtractFromEither)}<{responseType.Name}>' returned null. " +
                "This is unexpected as the method should always return a tuple.");
        }

        return ((int StatusCode, object? Body))invokeResult;
    }

    private static (int StatusCode, object? Body) ExtractFromEither<TResponse>(object result)
    {
        if (result is not Either<EncinaError, TResponse> either)
        {
            return (500, new PactErrorResponse(false, "encina.internal", $"Expected Either<EncinaError, {typeof(TResponse).Name}> but got {result.GetType().Name}"));
        }

        return either.Match(
            Right: response => (200, (object?)response),
            Left: error => (MapErrorToStatusCode(error), SerializeError(error)));
    }

    private static string ExtractRequestCategory(string path)
    {
        // Path format: /api/{category}/{TypeName}
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? segments[1].ToLowerInvariant() : string.Empty;
    }

    private static Type? FindRequestType(string typeName, Action<string>? warningCallback = null)
    {
        // Check cache first to avoid expensive assembly scanning
        if (s_requestTypeCache.TryGetValue(typeName, out var cachedType))
        {
            return cachedType;
        }

        // Search in all loaded assemblies for the type
        var foundType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => GetTypesFromAssemblySafely(a, warningCallback))
            .FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.Ordinal));

        // Cache the result (even if null) to avoid repeated scans
        s_requestTypeCache.TryAdd(typeName, foundType);

        return foundType;
    }

    private static Type[] GetTypesFromAssemblySafely(System.Reflection.Assembly assembly, Action<string>? warningCallback = null)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException ex)
        {
            // Log the loader exceptions for diagnostics - these often indicate missing dependencies
            var loaderMessages = ex.LoaderExceptions
                ?.Where(le => le is not null)
                .Select(le => le!.Message)
                .Distinct()
                .ToArray() ?? [];

            var warning = $"ReflectionTypeLoadException while loading types from assembly '{assembly.FullName}'. " +
                $"Loader exceptions: [{string.Join("; ", loaderMessages)}]. " +
                $"Returning {ex.Types?.Count(t => t is not null) ?? 0} successfully loaded types.";

            warningCallback?.Invoke(warning);

            // Return the types that were successfully loaded
            return ex.Types?.Where(t => t is not null).Cast<Type>().ToArray() ?? [];
        }
        catch (Exception ex)
        {
            // Log unexpected exceptions for diagnostics
            var warning = $"Unexpected exception while loading types from assembly '{assembly.FullName}': " +
                $"{ex.GetType().Name}: {ex.Message}";

            warningCallback?.Invoke(warning);

            return [];
        }
    }

    /// <summary>
    /// Clears the internal request type cache.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this method between test runs in long-running test processes to prevent
    /// unbounded cache growth. The cache uses a <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}"/>
    /// which does not have a maximum size limit.
    /// </para>
    /// <para>
    /// For scenarios with very high type diversity, consider replacing the cache with a
    /// bounded/LRU cache implementation to limit memory usage.
    /// </para>
    /// </remarks>
    internal static void ClearTypeCache() => s_requestTypeCache.Clear();

    private static string NormalizeJson(object? obj)
    {
        if (obj is null) return "null";
        if (obj is JsonElement element) return element.GetRawText();
        return JsonSerializer.Serialize(obj, s_defaultJsonOptions);
    }

    private static bool JsonBodiesMatch(string expected, string actual)
    {
        // Normalize both JSON strings for comparison
        try
        {
            using var expectedDoc = JsonDocument.Parse(expected);
            using var actualDoc = JsonDocument.Parse(actual);
            return JsonElementsMatch(expectedDoc.RootElement, actualDoc.RootElement);
        }
        catch (JsonException)
        {
            // If JSON parsing fails, fall back to string comparison
            return string.Equals(expected, actual, StringComparison.Ordinal);
        }
    }

    private static bool JsonElementsMatch(JsonElement expected, JsonElement actual)
    {
        if (expected.ValueKind != actual.ValueKind)
            return false;

        return expected.ValueKind switch
        {
            JsonValueKind.Object => JsonObjectsMatch(expected, actual),
            JsonValueKind.Array => JsonArraysMatch(expected, actual),
            JsonValueKind.String => expected.GetString() == actual.GetString(),
            JsonValueKind.Number => expected.GetRawText() == actual.GetRawText(),
            JsonValueKind.True or JsonValueKind.False => expected.GetBoolean() == actual.GetBoolean(),
            JsonValueKind.Null => true,
            _ => expected.GetRawText() == actual.GetRawText()
        };
    }

    /// <summary>
    /// Compares two JSON objects using Pact consumer-driven subset matching semantics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements subset matching: all properties in <paramref name="expected"/> must exist
    /// in <paramref name="actual"/> with matching values, but <paramref name="actual"/> may contain
    /// additional properties that are not in <paramref name="expected"/>.
    /// </para>
    /// <para>
    /// This is intentional Pact behavior - consumers define their expectations, and providers may
    /// return additional data. Extra properties in the actual response do not cause verification failure,
    /// allowing providers to evolve their API without breaking existing consumer contracts.
    /// </para>
    /// </remarks>
    /// <param name="expected">The expected JSON object (consumer expectation).</param>
    /// <param name="actual">The actual JSON object (provider response).</param>
    /// <returns>True if all expected properties match; false otherwise.</returns>
    private static bool JsonObjectsMatch(JsonElement expected, JsonElement actual)
    {
        var expectedProps = expected.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        var actualProps = actual.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

        // Subset matching: verify all expected properties exist in actual with matching values.
        // Extra properties in actual are intentionally allowed (Pact consumer-driven semantics).
        foreach (var prop in expectedProps)
        {
            if (!actualProps.TryGetValue(prop.Key, out var actualValue))
                return false;
            if (!JsonElementsMatch(prop.Value, actualValue))
                return false;
        }

        return true;
    }

    private static bool JsonArraysMatch(JsonElement expected, JsonElement actual)
    {
        var expectedItems = expected.EnumerateArray().ToList();
        var actualItems = actual.EnumerateArray().ToList();

        if (expectedItems.Count != actualItems.Count)
            return false;

        for (var i = 0; i < expectedItems.Count; i++)
        {
            if (!JsonElementsMatch(expectedItems[i], actualItems[i]))
                return false;
        }

        return true;
    }

    private static int MapErrorToStatusCode(EncinaError error)
    {
        var code = error.GetCode().IfNone("encina.unknown");

        return code switch
        {
            var c when c.StartsWith("encina.validation", StringComparison.OrdinalIgnoreCase) => (int)HttpStatusCode.BadRequest,
            var c when c.StartsWith("encina.notfound", StringComparison.OrdinalIgnoreCase) => (int)HttpStatusCode.NotFound,
            var c when c.StartsWith("encina.authorization", StringComparison.OrdinalIgnoreCase) => (int)HttpStatusCode.Forbidden,
            var c when c.StartsWith("encina.authentication", StringComparison.OrdinalIgnoreCase) => (int)HttpStatusCode.Unauthorized,
            var c when c.StartsWith("encina.conflict", StringComparison.OrdinalIgnoreCase) => (int)HttpStatusCode.Conflict,
            var c when c.StartsWith("encina.timeout", StringComparison.OrdinalIgnoreCase) => (int)HttpStatusCode.RequestTimeout,
            var c when c.StartsWith("encina.ratelimit", StringComparison.OrdinalIgnoreCase) => (int)HttpStatusCode.TooManyRequests,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    private static PactErrorResponse SerializeError(EncinaError error)
    {
        var code = error.GetCode().IfNone("encina.unknown");
        return new PactErrorResponse(
            IsSuccess: false,
            ErrorCode: code,
            ErrorMessage: error.Message);
    }

    private async Task ExecuteProviderStatesAsync(PactInteraction interaction)
    {
        // Pact v3+ format: providerStates array
        if (interaction.ProviderStates is { Count: > 0 })
        {
            foreach (var state in interaction.ProviderStates.Where(s => !string.IsNullOrEmpty(s.Name)))
            {
                await ExecuteProviderStateAsync(state.Name!, state.Params);
            }
        }
        // Pact v2 format: single providerState string
        else if (!string.IsNullOrEmpty(interaction.ProviderState))
        {
            await ExecuteProviderStateAsync(interaction.ProviderState, null);
        }
    }

    private async Task ExecuteProviderStateAsync(string stateName, IDictionary<string, object>? parameters)
    {
        if (_providerStateHandlersWithParams.TryGetValue(stateName, out var handlerWithParams) && parameters is not null)
        {
            await handlerWithParams(parameters);
        }
        else if (_providerStateHandlers.TryGetValue(stateName, out var handler))
        {
            await handler();
        }
        else
        {
            // No handler registered for this provider state
            var paramsInfo = parameters is { Count: > 0 }
                ? $" with parameters: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}"
                : string.Empty;
            var message = $"No provider state handler registered for '{stateName}'{paramsInfo}. " +
                          $"Register a handler using WithProviderState(\"{stateName}\", ...) or disable strict mode.";

            if (_strictProviderStates)
            {
                throw new InvalidOperationException(message);
            }

            _missingStateHandler?.Invoke(message);
        }
    }

    private static string? ExtractRequestTypeName(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        // Path format: /api/commands/{TypeName} or /api/queries/{TypeName}
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 3 ? segments[2] : null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_ownsServiceProvider && _serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _providerStateHandlers.Clear();
        _providerStateHandlersWithParams.Clear();
        _disposed = true;
    }

    // Internal types for Pact JSON deserialization
    private sealed record PactFile(
        [property: JsonPropertyName("consumer")] PactConsumer? Consumer,
        [property: JsonPropertyName("provider")] PactProvider? Provider,
        [property: JsonPropertyName("interactions")] List<PactInteraction>? Interactions);

    private sealed record PactConsumer(
        [property: JsonPropertyName("name")] string? Name);

    private sealed record PactProvider(
        [property: JsonPropertyName("name")] string? Name);

    /// <summary>
    /// Represents a Pact interaction with support for both v2 and v3+ provider state formats.
    /// </summary>
    private sealed record PactInteraction(
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("providerState")] string? ProviderState,
        [property: JsonPropertyName("providerStates")] List<PactProviderState>? ProviderStates,
        [property: JsonPropertyName("request")] PactRequest? Request,
        [property: JsonPropertyName("response")] PactResponse? Response);

    /// <summary>
    /// Represents a provider state entry for Pact v3+ format.
    /// </summary>
    private sealed record PactProviderState(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("params")] IDictionary<string, object>? Params);

    private sealed record PactRequest(
        [property: JsonPropertyName("method")] string? Method,
        [property: JsonPropertyName("path")] string? Path,
        [property: JsonPropertyName("body")] object? Body);

    private sealed record PactResponse(
        [property: JsonPropertyName("status")] int? Status,
        [property: JsonPropertyName("body")] object? Body);
}

/// <summary>
/// Result of verifying a Pact contract.
/// </summary>
/// <param name="Success">Whether all interactions were verified successfully.</param>
/// <param name="Errors">List of error messages if verification failed.</param>
/// <param name="InteractionResults">Results for each individual interaction.</param>
public sealed record PactVerificationResult(
    bool Success,
    IReadOnlyList<string> Errors,
    IReadOnlyList<InteractionVerificationResult> InteractionResults);

/// <summary>
/// Result of verifying a single Pact interaction.
/// </summary>
/// <param name="Description">The interaction description.</param>
/// <param name="Success">Whether the interaction was verified successfully.</param>
/// <param name="ErrorMessage">Error message if verification failed.</param>
public sealed record InteractionVerificationResult(
    string Description,
    bool Success,
    string? ErrorMessage);
