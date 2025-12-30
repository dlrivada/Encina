using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LanguageExt;

namespace Encina.Testing.Fakes;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IEncina"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// FakeEncina allows you to configure responses for specific request types and verify
/// that requests were sent and notifications were published. It supports:
/// </para>
/// <list type="bullet">
/// <item><description>Configuring responses via SetupResponse methods</description></item>
/// <item><description>Configuring errors via <see cref="SetupError{TRequest, TResponse}"/></description></item>
/// <item><description>Configuring stream responses via SetupStream methods</description></item>
/// <item><description>Verifying sent requests via <see cref="SentRequests"/> and WasSent methods</description></item>
/// <item><description>Verifying published notifications via <see cref="PublishedNotifications"/> and WasPublished methods</description></item>
/// </list>
/// <para>
/// All operations are thread-safe for parallel test execution.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Arrange
/// var fakeEncina = new FakeEncina();
/// fakeEncina.SetupResponse&lt;GetUserQuery, UserDto&gt;(new UserDto { Id = 1, Name = "Test" });
///
/// // Act
/// var result = await fakeEncina.Send(new GetUserQuery(1));
///
/// // Assert
/// result.ShouldBeSuccess();
/// fakeEncina.WasSent&lt;GetUserQuery&gt;().ShouldBeTrue();
/// </code>
/// </example>
public sealed class FakeEncina : IEncina
{
    private readonly ConcurrentDictionary<Type, object> _responses = new();
    private readonly ConcurrentDictionary<Type, object> _errors = new();
    private readonly ConcurrentDictionary<Type, object> _streamResponses = new();
    private readonly List<object> _sentRequests = [];
    private readonly List<object> _publishedNotifications = [];
    private readonly List<object> _streamRequests = [];
    private readonly object _lock = new();

    /// <summary>
    /// Gets all requests that have been sent through <see cref="Send{TResponse}"/>.
    /// </summary>
    public IReadOnlyList<object> SentRequests
    {
        get { lock (_lock) { return [.. _sentRequests]; } }
    }

    /// <summary>
    /// Gets all notifications that have been published through <see cref="Publish{TNotification}"/>.
    /// </summary>
    public IReadOnlyList<object> PublishedNotifications
    {
        get { lock (_lock) { return [.. _publishedNotifications]; } }
    }

    /// <summary>
    /// Gets all stream requests that have been sent through <see cref="Stream{TItem}"/>.
    /// </summary>
    public IReadOnlyList<object> StreamRequests
    {
        get { lock (_lock) { return [.. _streamRequests]; } }
    }

    /// <summary>
    /// Configures a successful response for a specific request type.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="response">The response to return.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FakeEncina SetupResponse<TRequest, TResponse>(TResponse response)
        where TRequest : IRequest<TResponse>
    {
        _responses[typeof(TRequest)] = response!;
        _errors.TryRemove(typeof(TRequest), out _);
        return this;
    }

    /// <summary>
    /// Configures a successful response for a specific request type using a factory function.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="responseFactory">Factory function that receives the request and returns the response.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FakeEncina SetupResponse<TRequest, TResponse>(Func<TRequest, TResponse> responseFactory)
        where TRequest : IRequest<TResponse>
    {
        _responses[typeof(TRequest)] = responseFactory;
        _errors.TryRemove(typeof(TRequest), out _);
        return this;
    }

    /// <summary>
    /// Configures an error response for a specific request type.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="error">The error to return.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FakeEncina SetupError<TRequest, TResponse>(EncinaError error)
        where TRequest : IRequest<TResponse>
    {
        _errors[typeof(TRequest)] = error;
        _responses.TryRemove(typeof(TRequest), out _);
        return this;
    }

    /// <summary>
    /// Configures a stream response for a specific request type.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="items">The items to yield.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FakeEncina SetupStream<TRequest, TItem>(IEnumerable<TItem> items)
        where TRequest : IStreamRequest<TItem>
    {
        _streamResponses[typeof(TRequest)] = items.ToList();
        return this;
    }

    /// <summary>
    /// Configures a stream response for a specific request type with potential errors.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="results">The results to yield (can include errors).</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FakeEncina SetupStream<TRequest, TItem>(IEnumerable<Either<EncinaError, TItem>> results)
        where TRequest : IStreamRequest<TItem>
    {
        _streamResponses[typeof(TRequest)] = results.ToList();
        return this;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, TResponse>> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_lock) { _sentRequests.Add(request); }
        var requestType = request.GetType();

        // Check for error setup first
        if (_errors.TryGetValue(requestType, out var error))
        {
            return new ValueTask<Either<EncinaError, TResponse>>((EncinaError)error);
        }

        // Check for response setup
        if (_responses.TryGetValue(requestType, out var response))
        {
            // Check if it's a factory function
            if (response is Delegate factory)
            {
                var result = factory.DynamicInvoke(request);
                return new ValueTask<Either<EncinaError, TResponse>>((TResponse)result!);
            }

            return new ValueTask<Either<EncinaError, TResponse>>((TResponse)response);
        }

        // No setup found - return a default error
        return new ValueTask<Either<EncinaError, TResponse>>(
            EncinaErrors.Create(EncinaErrorCodes.HandlerMissing, $"No handler configured for request type '{requestType.Name}'"));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        lock (_lock) { _publishedNotifications.Add(notification); }

        // Check for error setup
        if (_errors.TryGetValue(typeof(TNotification), out var error))
        {
            return new ValueTask<Either<EncinaError, Unit>>((EncinaError)error);
        }

        return new ValueTask<Either<EncinaError, Unit>>(Unit.Default);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Either<EncinaError, TItem>> Stream<TItem>(
        IStreamRequest<TItem> request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_lock) { _streamRequests.Add(request); }
        var requestType = request.GetType();

        // Check for stream setup
        if (_streamResponses.TryGetValue(requestType, out var response))
        {
            // Check if it's a list of Either results
            if (response is List<Either<EncinaError, TItem>> eitherResults)
            {
                foreach (var result in eitherResults)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return result;
                }
                yield break;
            }

            // Check if it's a list of items
            if (response is List<TItem> items)
            {
                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return item;
                }
                yield break;
            }
        }

        // No setup found - yield nothing (empty stream)
    }

    /// <summary>
    /// Verifies that a request of the specified type was sent.
    /// </summary>
    /// <typeparam name="TRequest">The request type to check for.</typeparam>
    /// <returns>True if a request of the specified type was sent.</returns>
    public bool WasSent<TRequest>()
    {
        lock (_lock) { return _sentRequests.Any(r => r is TRequest); }
    }

    /// <summary>
    /// Verifies that a request of the specified type was sent and matches the predicate.
    /// </summary>
    /// <typeparam name="TRequest">The request type to check for.</typeparam>
    /// <param name="predicate">Predicate to match against sent requests.</param>
    /// <returns>True if a matching request was sent.</returns>
    public bool WasSent<TRequest>(Func<TRequest, bool> predicate)
    {
        lock (_lock) { return _sentRequests.OfType<TRequest>().Any(predicate); }
    }

    /// <summary>
    /// Gets all sent requests of the specified type.
    /// </summary>
    /// <typeparam name="TRequest">The request type to filter by.</typeparam>
    /// <returns>Collection of sent requests of the specified type.</returns>
    public IReadOnlyList<TRequest> GetSentRequests<TRequest>()
    {
        lock (_lock) { return _sentRequests.OfType<TRequest>().ToList().AsReadOnly(); }
    }

    /// <summary>
    /// Gets the count of requests of the specified type that were sent.
    /// </summary>
    /// <typeparam name="TRequest">The request type to count.</typeparam>
    /// <returns>The number of requests sent.</returns>
    public int GetSentCount<TRequest>()
    {
        lock (_lock) { return _sentRequests.Count(r => r is TRequest); }
    }

    /// <summary>
    /// Verifies that a notification of the specified type was published.
    /// </summary>
    /// <typeparam name="TNotification">The notification type to check for.</typeparam>
    /// <returns>True if a notification of the specified type was published.</returns>
    public bool WasPublished<TNotification>() where TNotification : INotification
    {
        lock (_lock) { return _publishedNotifications.Any(n => n is TNotification); }
    }

    /// <summary>
    /// Verifies that a notification of the specified type was published and matches the predicate.
    /// </summary>
    /// <typeparam name="TNotification">The notification type to check for.</typeparam>
    /// <param name="predicate">Predicate to match against published notifications.</param>
    /// <returns>True if a matching notification was published.</returns>
    public bool WasPublished<TNotification>(Func<TNotification, bool> predicate)
        where TNotification : INotification
    {
        lock (_lock) { return _publishedNotifications.OfType<TNotification>().Any(predicate); }
    }

    /// <summary>
    /// Gets all published notifications of the specified type.
    /// </summary>
    /// <typeparam name="TNotification">The notification type to filter by.</typeparam>
    /// <returns>Collection of published notifications of the specified type.</returns>
    public IReadOnlyList<TNotification> GetPublishedNotifications<TNotification>()
        where TNotification : INotification
    {
        lock (_lock) { return _publishedNotifications.OfType<TNotification>().ToList().AsReadOnly(); }
    }

    /// <summary>
    /// Gets the count of notifications of the specified type that were published.
    /// </summary>
    /// <typeparam name="TNotification">The notification type to count.</typeparam>
    /// <returns>The number of notifications published.</returns>
    public int GetPublishedCount<TNotification>() where TNotification : INotification
    {
        lock (_lock) { return _publishedNotifications.Count(n => n is TNotification); }
    }

    /// <summary>
    /// Verifies that a stream request of the specified type was sent.
    /// </summary>
    /// <typeparam name="TRequest">The request type to check for.</typeparam>
    /// <returns>True if a stream request of the specified type was sent.</returns>
    public bool WasStreamed<TRequest>()
    {
        lock (_lock) { return _streamRequests.Any(r => r is TRequest); }
    }

    /// <summary>
    /// Clears all recorded requests, notifications, and configured responses.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _responses.Clear();
            _errors.Clear();
            _streamResponses.Clear();
            _sentRequests.Clear();
            _publishedNotifications.Clear();
            _streamRequests.Clear();
        }
    }

    /// <summary>
    /// Clears only the recorded requests and notifications (keeps configured responses).
    /// </summary>
    public void ClearRecordedCalls()
    {
        lock (_lock)
        {
            _sentRequests.Clear();
            _publishedNotifications.Clear();
            _streamRequests.Clear();
        }
    }

    /// <summary>
    /// Configures an error for a notification type.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="error">The error to return when publishing.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FakeEncina SetupPublishError<TNotification>(EncinaError error)
        where TNotification : INotification
    {
        _errors[typeof(TNotification)] = error;
        return this;
    }
}
