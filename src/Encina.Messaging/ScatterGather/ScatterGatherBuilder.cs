using LanguageExt;

namespace Encina.Messaging.ScatterGather;

/// <summary>
/// Static factory for creating scatter-gather builders.
/// </summary>
public static class ScatterGatherBuilder
{
    /// <summary>
    /// Creates a new scatter-gather builder.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to scatter.</typeparam>
    /// <typeparam name="TResponse">The type of response from handlers.</typeparam>
    /// <param name="name">The name of the scatter-gather operation.</param>
    /// <returns>A new scatter-gather builder.</returns>
    public static ScatterGatherBuilder<TRequest, TResponse> Create<TRequest, TResponse>(string name)
        where TRequest : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ScatterGatherBuilder<TRequest, TResponse>(name);
    }

    /// <summary>
    /// Creates a new scatter-gather builder with an auto-generated name.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to scatter.</typeparam>
    /// <typeparam name="TResponse">The type of response from handlers.</typeparam>
    /// <returns>A new scatter-gather builder.</returns>
    public static ScatterGatherBuilder<TRequest, TResponse> Create<TRequest, TResponse>()
        where TRequest : class
        => new($"ScatterGather_{typeof(TRequest).Name}");
}

/// <summary>
/// Fluent builder for configuring scatter-gather operations.
/// </summary>
/// <typeparam name="TRequest">The type of request to scatter.</typeparam>
/// <typeparam name="TResponse">The type of response from handlers.</typeparam>
public sealed class ScatterGatherBuilder<TRequest, TResponse>
    where TRequest : class
{
    private readonly string _name;
    private readonly List<ScatterDefinition<TRequest, TResponse>> _scatterHandlers = [];
    private Func<IReadOnlyList<ScatterExecutionResult<TResponse>>, CancellationToken, ValueTask<Either<EncinaError, TResponse>>>? _gatherHandler;
    private GatherStrategy _strategy = GatherStrategy.WaitForAll;
    private TimeSpan? _timeout;
    private int? _quorumCount;
    private bool _executeInParallel = true;
    private int? _maxDegreeOfParallelism;
    private Dictionary<string, object>? _metadata;
    private int _scatterCounter;

    internal ScatterGatherBuilder(string name)
    {
        _name = name;
    }

    /// <summary>
    /// Adds a scatter handler to the operation.
    /// </summary>
    /// <param name="name">The name of the scatter handler.</param>
    /// <returns>A scatter builder for configuring the handler.</returns>
    public ScatterBuilder<TRequest, TResponse> ScatterTo(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ScatterBuilder<TRequest, TResponse>(this, name);
    }

    /// <summary>
    /// Adds a scatter handler with an auto-generated name.
    /// </summary>
    /// <returns>A scatter builder for configuring the handler.</returns>
    public ScatterBuilder<TRequest, TResponse> ScatterTo()
        => new(this, GenerateScatterName());

    /// <summary>
    /// Adds a scatter handler inline with a handler function.
    /// </summary>
    /// <param name="handler">The handler function.</param>
    /// <returns>This builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> ScatterTo(
        Func<TRequest, CancellationToken, ValueTask<Either<EncinaError, TResponse>>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _scatterHandlers.Add(new ScatterDefinition<TRequest, TResponse>(
            GenerateScatterName(),
            handler));
        return this;
    }

    /// <summary>
    /// Adds a scatter handler inline with a synchronous handler function.
    /// </summary>
    /// <param name="handler">The synchronous handler function.</param>
    /// <returns>This builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> ScatterTo(
        Func<TRequest, Either<EncinaError, TResponse>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return ScatterTo((req, _) => ValueTask.FromResult(handler(req)));
    }

    /// <summary>
    /// Adds a named scatter handler inline with a handler function.
    /// </summary>
    /// <param name="name">The name of the scatter handler.</param>
    /// <param name="handler">The handler function.</param>
    /// <returns>This builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> ScatterTo(
        string name,
        Func<TRequest, CancellationToken, ValueTask<Either<EncinaError, TResponse>>> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(handler);
        _scatterHandlers.Add(new ScatterDefinition<TRequest, TResponse>(name, handler));
        return this;
    }

    /// <summary>
    /// Adds a named scatter handler inline with a synchronous handler function.
    /// </summary>
    /// <param name="name">The name of the scatter handler.</param>
    /// <param name="handler">The synchronous handler function.</param>
    /// <returns>This builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> ScatterTo(
        string name,
        Func<TRequest, Either<EncinaError, TResponse>> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(handler);
        return ScatterTo(name, (req, _) => ValueTask.FromResult(handler(req)));
    }

    /// <summary>
    /// Configures the gather handler with a specific strategy.
    /// </summary>
    /// <param name="strategy">The gather strategy to use.</param>
    /// <returns>A gather builder for configuring the handler.</returns>
    public GatherBuilder<TRequest, TResponse> GatherWith(GatherStrategy strategy)
    {
        _strategy = strategy;
        return new GatherBuilder<TRequest, TResponse>(this);
    }

    /// <summary>
    /// Configures the gather handler with WaitForAll strategy.
    /// </summary>
    /// <returns>A gather builder for configuring the handler.</returns>
    public GatherBuilder<TRequest, TResponse> GatherAll()
        => GatherWith(GatherStrategy.WaitForAll);

    /// <summary>
    /// Configures the gather handler with WaitForFirst strategy.
    /// </summary>
    /// <returns>A gather builder for configuring the handler.</returns>
    public GatherBuilder<TRequest, TResponse> GatherFirst()
        => GatherWith(GatherStrategy.WaitForFirst);

    /// <summary>
    /// Configures the gather handler with WaitForQuorum strategy.
    /// </summary>
    /// <param name="quorumCount">The number of successful responses required.</param>
    /// <returns>A gather builder for configuring the handler.</returns>
    public GatherBuilder<TRequest, TResponse> GatherQuorum(int quorumCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quorumCount, 1);
        _strategy = GatherStrategy.WaitForQuorum;
        _quorumCount = quorumCount;
        return new GatherBuilder<TRequest, TResponse>(this);
    }

    /// <summary>
    /// Configures the gather handler with WaitForAllAllowPartial strategy.
    /// </summary>
    /// <returns>A gather builder for configuring the handler.</returns>
    public GatherBuilder<TRequest, TResponse> GatherAllAllowingPartialFailures()
        => GatherWith(GatherStrategy.WaitForAllAllowPartial);

    /// <summary>
    /// Sets the timeout for the entire scatter-gather operation.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>This builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> WithTimeout(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// Configures scatter handlers to execute sequentially instead of in parallel.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> ExecuteSequentially()
    {
        _executeInParallel = false;
        return this;
    }

    /// <summary>
    /// Configures scatter handlers to execute in parallel.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">Optional maximum degree of parallelism.</param>
    /// <returns>This builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> ExecuteInParallel(int? maxDegreeOfParallelism = null)
    {
        if (maxDegreeOfParallelism.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxDegreeOfParallelism.Value, 1);
        }

        _executeInParallel = true;
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
        return this;
    }

    /// <summary>
    /// Adds metadata to the scatter-gather operation.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>This builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> WithMetadata(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _metadata ??= [];
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Builds the scatter-gather definition.
    /// </summary>
    /// <returns>The built scatter-gather definition.</returns>
    /// <exception cref="InvalidOperationException">Thrown if configuration is invalid.</exception>
    public BuiltScatterGatherDefinition<TRequest, TResponse> Build()
    {
        if (_scatterHandlers.Count == 0)
        {
            throw new InvalidOperationException("At least one scatter handler must be configured. Use ScatterTo() to add handlers.");
        }

        if (_gatherHandler is null)
        {
            throw new InvalidOperationException("A gather handler must be configured. Use GatherWith() or GatherAll() to configure.");
        }

        if (_strategy == GatherStrategy.WaitForQuorum && _quorumCount.HasValue && _quorumCount.Value > _scatterHandlers.Count)
        {
            throw new InvalidOperationException(
                $"Quorum count ({_quorumCount.Value}) cannot exceed the number of scatter handlers ({_scatterHandlers.Count}).");
        }

        var options = new ScatterGatherExecutionOptions(
            Strategy: _strategy,
            Timeout: _timeout,
            QuorumCount: _quorumCount,
            ExecuteInParallel: _executeInParallel,
            MaxDegreeOfParallelism: _maxDegreeOfParallelism,
            Metadata: _metadata?.AsReadOnly());

        return new BuiltScatterGatherDefinition<TRequest, TResponse>(
            _name,
            [.. _scatterHandlers.OrderBy(h => h.Priority)],
            _gatherHandler,
            options);
    }

    internal void AddScatterHandler(ScatterDefinition<TRequest, TResponse> handler)
        => _scatterHandlers.Add(handler);

    internal void SetGatherHandler(
        Func<IReadOnlyList<ScatterExecutionResult<TResponse>>, CancellationToken, ValueTask<Either<EncinaError, TResponse>>> handler)
        => _gatherHandler = handler;

    internal string GenerateScatterName() => $"Scatter_{++_scatterCounter}";
}

/// <summary>
/// Builder for configuring a scatter handler.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public sealed class ScatterBuilder<TRequest, TResponse>
    where TRequest : class
{
    private readonly ScatterGatherBuilder<TRequest, TResponse> _parent;
    private readonly string _name;
    private int _priority;
    private Dictionary<string, object>? _metadata;

    internal ScatterBuilder(ScatterGatherBuilder<TRequest, TResponse> parent, string name)
    {
        _parent = parent;
        _name = name;
    }

    /// <summary>
    /// Sets the priority of this scatter handler.
    /// </summary>
    /// <remarks>
    /// Lower values execute first in sequential mode.
    /// </remarks>
    /// <param name="priority">The priority value.</param>
    /// <returns>This builder for chaining.</returns>
    public ScatterBuilder<TRequest, TResponse> WithPriority(int priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>
    /// Adds metadata to this scatter handler.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>This builder for chaining.</returns>
    public ScatterBuilder<TRequest, TResponse> WithMetadata(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _metadata ??= [];
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Configures the handler function.
    /// </summary>
    /// <param name="handler">The async handler function.</param>
    /// <returns>The parent builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> Execute(
        Func<TRequest, CancellationToken, ValueTask<Either<EncinaError, TResponse>>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _parent.AddScatterHandler(new ScatterDefinition<TRequest, TResponse>(
            _name,
            handler,
            _priority,
            _metadata?.AsReadOnly()));
        return _parent;
    }

    /// <summary>
    /// Configures a synchronous handler function.
    /// </summary>
    /// <param name="handler">The synchronous handler function.</param>
    /// <returns>The parent builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> Execute(
        Func<TRequest, Either<EncinaError, TResponse>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Execute((req, _) => ValueTask.FromResult(handler(req)));
    }

    /// <summary>
    /// Configures an async handler function without Either.
    /// </summary>
    /// <param name="handler">The async handler function.</param>
    /// <returns>The parent builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> Execute(
        Func<TRequest, CancellationToken, ValueTask<TResponse>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Execute(async (req, ct) =>
        {
            var result = await handler(req, ct).ConfigureAwait(false);
            return Prelude.Right<EncinaError, TResponse>(result);
        });
    }

    /// <summary>
    /// Configures a synchronous handler function without Either.
    /// </summary>
    /// <param name="handler">The synchronous handler function.</param>
    /// <returns>The parent builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> Execute(Func<TRequest, TResponse> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Execute(req => Prelude.Right<EncinaError, TResponse>(handler(req)));
    }
}

/// <summary>
/// Builder for configuring the gather handler.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public sealed class GatherBuilder<TRequest, TResponse>
    where TRequest : class
{
    private readonly ScatterGatherBuilder<TRequest, TResponse> _parent;

    internal GatherBuilder(ScatterGatherBuilder<TRequest, TResponse> parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Configures the gather handler with access to all scatter results.
    /// </summary>
    /// <param name="handler">The gather handler function.</param>
    /// <returns>The parent builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> Aggregate(
        Func<IReadOnlyList<ScatterExecutionResult<TResponse>>, CancellationToken, ValueTask<Either<EncinaError, TResponse>>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _parent.SetGatherHandler(handler);
        return _parent;
    }

    /// <summary>
    /// Configures a synchronous gather handler.
    /// </summary>
    /// <param name="handler">The synchronous gather handler function.</param>
    /// <returns>The parent builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> Aggregate(
        Func<IReadOnlyList<ScatterExecutionResult<TResponse>>, Either<EncinaError, TResponse>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Aggregate((results, _) => ValueTask.FromResult(handler(results)));
    }

    /// <summary>
    /// Configures a gather handler that receives only successful responses.
    /// </summary>
    /// <param name="handler">The gather handler function.</param>
    /// <returns>The parent builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> AggregateSuccessful(
        Func<IEnumerable<TResponse>, CancellationToken, ValueTask<Either<EncinaError, TResponse>>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Aggregate((results, ct) =>
        {
            var successful = results
                .Where(r => r.IsSuccess)
                .Select(r => r.Result.Match(v => v, _ => default!));
            return handler(successful, ct);
        });
    }

    /// <summary>
    /// Configures a synchronous gather handler that receives only successful responses.
    /// </summary>
    /// <param name="handler">The synchronous gather handler function.</param>
    /// <returns>The parent builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> AggregateSuccessful(
        Func<IEnumerable<TResponse>, Either<EncinaError, TResponse>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return AggregateSuccessful((results, _) => ValueTask.FromResult(handler(results)));
    }

    /// <summary>
    /// Configures the gather to return the first successful response.
    /// </summary>
    /// <returns>The parent builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> TakeFirst()
    {
        return Aggregate(results =>
        {
            var first = results.FirstOrDefault(r => r.IsSuccess);
            if (first is null)
            {
                return Prelude.Left<EncinaError, TResponse>(
                    EncinaErrors.Create(ScatterGatherErrorCodes.AllScattersFailed, "No scatter handlers succeeded."));
            }

            return first.Result;
        });
    }

    /// <summary>
    /// Configures the gather to return the response with the best result according to a selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to compare.</typeparam>
    /// <param name="selector">The selector function to extract the comparison key.</param>
    /// <returns>The parent builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> TakeBest<TKey>(Func<TResponse, TKey> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return AggregateSuccessful(results =>
        {
            var best = results.OrderBy(selector).FirstOrDefault();
            if (best is null)
            {
                return Prelude.Left<EncinaError, TResponse>(
                    EncinaErrors.Create(ScatterGatherErrorCodes.AllScattersFailed, "No scatter handlers succeeded."));
            }

            return Prelude.Right<EncinaError, TResponse>(best);
        });
    }

    /// <summary>
    /// Configures the gather to return the response with the minimum value according to a selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to compare.</typeparam>
    /// <param name="selector">The selector function to extract the comparison key.</param>
    /// <returns>The parent builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> TakeMin<TKey>(Func<TResponse, TKey> selector)
        => TakeBest(selector);

    /// <summary>
    /// Configures the gather to return the response with the maximum value according to a selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to compare.</typeparam>
    /// <param name="selector">The selector function to extract the comparison key.</param>
    /// <returns>The parent builder for chaining.</returns>
    public ScatterGatherBuilder<TRequest, TResponse> TakeMax<TKey>(Func<TResponse, TKey> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return AggregateSuccessful(results =>
        {
            var best = results.OrderByDescending(selector).FirstOrDefault();
            if (best is null)
            {
                return Prelude.Left<EncinaError, TResponse>(
                    EncinaErrors.Create(ScatterGatherErrorCodes.AllScattersFailed, "No scatter handlers succeeded."));
            }

            return Prelude.Right<EncinaError, TResponse>(best);
        });
    }
}
