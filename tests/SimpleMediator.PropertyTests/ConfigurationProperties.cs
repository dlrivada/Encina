using System.Reflection;
using FsCheck;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace SimpleMediator.PropertyTests;

public sealed class ConfigurationProperties
{
    private const int DeterministicValue = 137;
    private const int MaxSelectorLength = 6;

    private enum HandlerExecution
    {
        Success,
        Exception,
        Cancellation
    }

    private static readonly (Type Type, string Label)[] TrackingPipelineBehaviorPool =
    {
        (typeof(TrackingOuterPipelineBehavior<,>), "pipeline-outer"),
        (typeof(TrackingInnerPipelineBehavior<,>), "pipeline-inner"),
        (typeof(TrackingMetricsPipelineBehavior<,>), "pipeline-metrics"),
        (typeof(TrackingAuditPipelineBehavior<,>), "pipeline-audit")
    };

    private static readonly (Type Type, string Label)[] TrackingPreProcessorPool =
    {
        (typeof(RecordingPreProcessorOne), "pre-one"),
        (typeof(RecordingPreProcessorTwo), "pre-two"),
        (typeof(GenericRecordingPreProcessor<>), "pre-generic")
    };

    private static readonly (Type Type, string Label)[] TrackingPostProcessorPool =
    {
        (typeof(RecordingPostProcessorOne), "post-one"),
        (typeof(RecordingPostProcessorTwo), "post-two"),
        (typeof(GenericRecordingPostProcessor<,>), "post-generic")
    };

    private static readonly Assembly[] CandidateAssemblies =
    {
        typeof(global::SimpleMediator.SimpleMediator).Assembly,
        typeof(ConfigurationProperties).Assembly
    };

    private static readonly Type[] PipelineBehaviorCandidates = TrackingPipelineBehaviorPool.Select(static c => c.Type).ToArray();
    private static readonly Type[] PreProcessorCandidates = TrackingPreProcessorPool.Select(static c => c.Type).ToArray();
    private static readonly Type[] PostProcessorCandidates = TrackingPostProcessorPool.Select(static c => c.Type).ToArray();

    [Property(MaxTest = 150)]
    public bool RegisterServicesFromAssemblies_RemovesNullsAndDuplicates(List<int> indices)
    {
        indices ??= new List<int>();

        var configuration = new SimpleMediatorConfiguration();
        var resolved = indices.Select(MapIndex).ToList();

        foreach (var assembly in resolved)
        {
            if (assembly is null)
            {
                configuration.RegisterServicesFromAssemblies(null!);
            }
            else
            {
                configuration.RegisterServicesFromAssembly(assembly);
            }
        }

        var registered = GetAssemblies(configuration);

        var expectedSet = resolved
            .Where(a => a is not null)
            .Select(a => a!)
            .ToHashSet();

        return registered.Count == expectedSet.Count && registered.ToHashSet().SetEquals(expectedSet);
    }

    [Property(MaxTest = 100)]
    public bool AddPipelineBehavior_IgnoresDuplicates(List<bool> selectors)
    {
        selectors ??= new List<bool>();
        selectors = selectors.Take(MaxSelectorLength).ToList();

        var behaviors = selectors.Select(selector =>
            selector ? PipelineBehaviorCandidates[0]
                     : PipelineBehaviorCandidates[1]).ToList();

        if (behaviors.Count == 0)
        {
            behaviors.Add(PipelineBehaviorCandidates[0]);
        }

        var configuration = new SimpleMediatorConfiguration();
        foreach (var behavior in behaviors)
        {
            configuration.AddPipelineBehavior(behavior);
        }

        var registered = GetPipelineBehaviorTypes(configuration);
        var expected = behaviors.Distinct().ToList();

        return registered.Count == expected.Count && registered.ToHashSet().SetEquals(expected);
    }

    [Property(MaxTest = 150)]
    public bool AddPipelineBehavior_PreservesFirstOccurrenceOrder(List<int> selectors)
    {
        var indices = NormalizeIndices(selectors, PipelineBehaviorCandidates.Length, 0);
        var configuration = new SimpleMediatorConfiguration();

        foreach (var index in indices)
        {
            configuration.AddPipelineBehavior(PipelineBehaviorCandidates[index]);
        }

        var registered = GetPipelineBehaviorTypes(configuration);
        var expected = OrderedDistinct(indices.Select(i => PipelineBehaviorCandidates[i]));

        return registered.SequenceEqual(expected);
    }

    [Property(MaxTest = 120)]
    public bool AddRequestPreProcessor_PreservesFirstOccurrenceOrder(List<int> selectors)
    {
        var indices = NormalizeIndices(selectors, PreProcessorCandidates.Length, 0, allowEmpty: false);
        var configuration = new SimpleMediatorConfiguration();

        foreach (var index in indices)
        {
            configuration.AddRequestPreProcessor(PreProcessorCandidates[index]);
        }

        var registered = GetRequestPreProcessorTypes(configuration);
        var expected = OrderedDistinct(indices.Select(i => PreProcessorCandidates[i]));

        return registered.SequenceEqual(expected);
    }

    [Property(MaxTest = 120)]
    public bool AddRequestPostProcessor_PreservesFirstOccurrenceOrder(List<int> selectors)
    {
        var indices = NormalizeIndices(selectors, PostProcessorCandidates.Length, 0, allowEmpty: false);
        var configuration = new SimpleMediatorConfiguration();

        foreach (var index in indices)
        {
            configuration.AddRequestPostProcessor(PostProcessorCandidates[index]);
        }

        var registered = GetRequestPostProcessorTypes(configuration);
        var expected = OrderedDistinct(indices.Select(i => PostProcessorCandidates[i]));

        return registered.SequenceEqual(expected);
    }

    [Property(MaxTest = 100)]
    public bool WithHandlerLifetime_UsesLastConfiguredValue(List<int> lifetimes)
    {
        var configuration = new SimpleMediatorConfiguration();
        var indices = NormalizeIndices(lifetimes, Enum.GetValues<ServiceLifetime>().Length, 0);

        foreach (var index in indices)
        {
            var lifetime = MapServiceLifetime(index);
            configuration.WithHandlerLifetime(lifetime);
        }

        return indices.Count == 0
            ? configuration.HandlerLifetime == ServiceLifetime.Scoped
            : configuration.HandlerLifetime == MapServiceLifetime(indices[^1]);
    }

    [Property(MaxTest = 50)]
    public bool AssemblyScanner_ReturnsCachedInstance(NonNegativeInt invocations)
    {
        var assembly = typeof(global::SimpleMediator.SimpleMediator).Assembly;
        var first = global::SimpleMediator.MediatorAssemblyScanner.GetRegistrations(assembly);

        for (var i = 0; i <= invocations.Get; i++)
        {
            var current = global::SimpleMediator.MediatorAssemblyScanner.GetRegistrations(assembly);
            if (!ReferenceEquals(first, current))
            {
                return false;
            }
        }

        return true;
    }

    private static Assembly? MapIndex(int value) => value switch
    {
        0 => CandidateAssemblies[0],
        1 => CandidateAssemblies[1],
        _ => null
    };

    private static List<int> NormalizeIndices(List<int>? input, int modulo, int defaultValue, bool allowEmpty = false)
    {
        var source = (input ?? new List<int>()).Take(MaxSelectorLength).ToList();

        if (source.Count == 0 && !allowEmpty)
        {
            source.Add(defaultValue);
        }

        return source.Select(value => ((value % modulo) + modulo) % modulo).ToList();
    }

    private static ServiceLifetime MapServiceLifetime(int index) => index switch
    {
        0 => ServiceLifetime.Scoped,
        1 => ServiceLifetime.Singleton,
        2 => ServiceLifetime.Transient,
        _ => ServiceLifetime.Scoped
    };

    private static HandlerExecution MapExecution(int value) => value switch
    {
        0 => HandlerExecution.Success,
        1 => HandlerExecution.Exception,
        2 => HandlerExecution.Cancellation,
        _ => HandlerExecution.Success
    };

    private sealed record ExecutionResult(Either<MediatorError, int> Outcome, IReadOnlyList<string> Events);

    private static List<Type> OrderedDistinct(IEnumerable<Type> sequence)
    {
        var seen = new System.Collections.Generic.HashSet<Type>();
        var ordered = new List<Type>();

        foreach (var type in sequence)
        {
            if (seen.Add(type))
            {
                ordered.Add(type);
            }
        }

        return ordered;
    }

    private static IReadOnlyCollection<Assembly> GetAssemblies(SimpleMediatorConfiguration configuration)
    {
        var property = typeof(SimpleMediatorConfiguration)
            .GetProperty("Assemblies", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        return property?.GetValue(configuration) as IReadOnlyCollection<Assembly> ?? new List<Assembly>();
    }

    private static IReadOnlyList<Type> GetPipelineBehaviorTypes(SimpleMediatorConfiguration configuration)
    {
        var property = typeof(SimpleMediatorConfiguration)
            .GetProperty("PipelineBehaviorTypes", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        return property?.GetValue(configuration) as IReadOnlyList<Type> ?? System.Array.Empty<Type>();
    }

    private static IReadOnlyList<Type> GetRequestPreProcessorTypes(SimpleMediatorConfiguration configuration)
    {
        var property = typeof(SimpleMediatorConfiguration)
            .GetProperty("RequestPreProcessorTypes", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        return property?.GetValue(configuration) as IReadOnlyList<Type> ?? System.Array.Empty<Type>();
    }

    private static IReadOnlyList<Type> GetRequestPostProcessorTypes(SimpleMediatorConfiguration configuration)
    {
        var property = typeof(SimpleMediatorConfiguration)
            .GetProperty("RequestPostProcessorTypes", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        return property?.GetValue(configuration) as IReadOnlyList<Type> ?? System.Array.Empty<Type>();
    }

    [Property(MaxTest = 150)]
    public bool Send_ComposesPipelineDeterministically(List<int> pipelineSelectors, List<int> preSelectors, List<int> postSelectors)
    {
        var pipelineIndices = NormalizeIndices(pipelineSelectors, TrackingPipelineBehaviorPool.Length, 0);
        var preIndices = NormalizeIndices(preSelectors, TrackingPreProcessorPool.Length, 0, allowEmpty: true);
        var postIndices = NormalizeIndices(postSelectors, TrackingPostProcessorPool.Length, 0, allowEmpty: true);

        var pipeline = SelectCandidates(pipelineIndices, TrackingPipelineBehaviorPool);
        var preProcessors = SelectCandidates(preIndices, TrackingPreProcessorPool);
        var postProcessors = SelectCandidates(postIndices, TrackingPostProcessorPool);

        var result = ExecuteMediator(pipeline, preProcessors, postProcessors, HandlerExecution.Success);
        var expected = BuildExpectedTimeline(pipeline, preProcessors, postProcessors, HandlerExecution.Success);

        return result.Outcome.Match(
            Left: static _ => false,
            Right: value => value == DeterministicValue && result.Events.SequenceEqual(expected));
    }

    [Property(MaxTest = 150, Skip = "Pure ROP: exceptions now propagate (fail-fast)")]
    public bool Send_ComposesPipelineAcrossOutcomes(List<int> pipelineSelectors, List<int> preSelectors, List<int> postSelectors, int outcomeSelector)
    {
        var outcome = MapExecution(outcomeSelector);
        var pipelineIndices = NormalizeIndices(pipelineSelectors, TrackingPipelineBehaviorPool.Length, 0);
        var preIndices = NormalizeIndices(preSelectors, TrackingPreProcessorPool.Length, 0, allowEmpty: true);
        var postIndices = NormalizeIndices(postSelectors, TrackingPostProcessorPool.Length, 0, allowEmpty: true);

        var pipeline = SelectCandidates(pipelineIndices, TrackingPipelineBehaviorPool);
        var preProcessors = SelectCandidates(preIndices, TrackingPreProcessorPool);
        var postProcessors = SelectCandidates(postIndices, TrackingPostProcessorPool);

        var result = ExecuteMediator(pipeline, preProcessors, postProcessors, outcome);
        var expected = BuildExpectedTimeline(pipeline, preProcessors, postProcessors, outcome);

        return outcome switch
        {
            HandlerExecution.Success => result.Outcome.Match(
                Left: static _ => false,
                Right: value => value == DeterministicValue && result.Events.SequenceEqual(expected)),
            HandlerExecution.Exception => result.Outcome.Match(
                Left: err => err.Exception.Match(Some: ex => ex is InvalidOperationException, None: () => false)
                                 && result.Events.SequenceEqual(expected),
                Right: static _ => false),
            HandlerExecution.Cancellation => result.Outcome.Match(
                Left: err => err.Exception.Match(Some: ex => ex is OperationCanceledException, None: () => false)
                                 && result.Events.SequenceEqual(expected),
                Right: static _ => false),
            _ => false
        };
    }

    private static List<(Type Type, string Label)> SelectCandidates(IEnumerable<int> indices, (Type Type, string Label)[] pool)
    {
        var seen = new System.Collections.Generic.HashSet<Type>();
        var ordered = new List<(Type, string)>();

        foreach (var index in indices)
        {
            if (index >= pool.Length)
            {
                continue;
            }

            var candidate = pool[index];
            if (seen.Add(candidate.Type))
            {
                ordered.Add(candidate);
            }
        }

        return ordered;
    }

    private static ExecutionResult ExecuteMediator(
        IReadOnlyList<(Type Type, string Label)> pipeline,
        IReadOnlyList<(Type Type, string Label)> preProcessors,
        IReadOnlyList<(Type Type, string Label)> postProcessors,
        HandlerExecution execution)
    {
        var services = new ServiceCollection();
        services.AddSingleton<CallRecorder>();
        services.AddScoped<IRequestHandler<TrackedRequest, int>, RecordingRequestHandler>();

        foreach (var candidate in pipeline)
        {
            var closedType = CloseGeneric(candidate.Type, typeof(TrackedRequest), typeof(int));
            services.AddScoped(typeof(IPipelineBehavior<TrackedRequest, int>), closedType);
        }

        foreach (var candidate in preProcessors)
        {
            var closedType = CloseGeneric(candidate.Type, typeof(TrackedRequest));
            services.AddScoped(typeof(IRequestPreProcessor<TrackedRequest>), closedType);
        }

        foreach (var candidate in postProcessors)
        {
            var closedType = CloseGeneric(candidate.Type, typeof(TrackedRequest), typeof(int));
            services.AddScoped(typeof(IRequestPostProcessor<TrackedRequest, int>), closedType);
        }

        using var provider = services.BuildServiceProvider();
        var recorder = provider.GetRequiredService<CallRecorder>();
        recorder.Clear();

        var mediator = new global::SimpleMediator.SimpleMediator(provider.GetRequiredService<IServiceScopeFactory>());
        var request = new TrackedRequest(DeterministicValue, execution);
        var tokenSource = execution == HandlerExecution.Cancellation ? new CancellationTokenSource() : null;
        tokenSource?.Cancel();

        var outcome = mediator.Send(request, tokenSource?.Token ?? CancellationToken.None)
            .AsTask()
            .GetAwaiter()
            .GetResult();
        var events = recorder.Snapshot();
        return new ExecutionResult(outcome, events);
    }

    private static Type CloseGeneric(Type type, params Type[] arguments)
        => type.IsGenericTypeDefinition ? type.MakeGenericType(arguments) : type;

    private static List<string> BuildExpectedTimeline(
        IReadOnlyList<(Type Type, string Label)> pipeline,
        IReadOnlyList<(Type Type, string Label)> preProcessors,
        IReadOnlyList<(Type Type, string Label)> postProcessors,
        HandlerExecution execution)
    {
        var events = new List<string>();
        events.AddRange(preProcessors.Select(c => $"pre:{c.Label}"));
        events.AddRange(pipeline.Select(c => $"behavior:{c.Label}:enter"));
        events.Add("handler");
        events.AddRange(pipeline.Reverse().Select(c => $"behavior:{c.Label}:exit"));
        if (execution == HandlerExecution.Success)
        {
            events.AddRange(postProcessors.Select(c => $"post:{c.Label}"));
        }
        return events;
    }

    private sealed record TrackedRequest(int Value, HandlerExecution Execution) : IRequest<int>;

    private sealed class RecordingRequestHandler(ConfigurationProperties.CallRecorder recorder) : IRequestHandler<TrackedRequest, int>
    {
        private readonly CallRecorder _recorder = recorder;

        public Task<Either<MediatorError, int>> Handle(TrackedRequest request, CancellationToken cancellationToken)
        {
            _recorder.Add("handler");

            return request.Execution switch
            {
                HandlerExecution.Success => Task.FromResult(Right<MediatorError, int>(request.Value)),
                HandlerExecution.Exception => ThrowAsFault<int>(),
                HandlerExecution.Cancellation => ThrowAsCancellation<int>(cancellationToken),
                _ => Task.FromResult(Right<MediatorError, int>(request.Value))
            };

            static Task<Either<MediatorError, T>> ThrowAsFault<T>() => Task.FromException<Either<MediatorError, T>>(new InvalidOperationException("boom"));

            static Task<Either<MediatorError, T>> ThrowAsCancellation<T>(CancellationToken token)
            {
                var source = Task.FromCanceled<Either<MediatorError, T>>(token);
                return source;
            }
        }
    }

    private sealed class CallRecorder
    {
        private readonly List<string> _events = new();
        private readonly object _lock = new();

        public void Add(string entry)
        {
            lock (_lock)
            {
                _events.Add(entry);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _events.Clear();
            }
        }

        public string[] Snapshot()
        {
            lock (_lock)
            {
                return _events.ToArray();
            }
        }
    }

    private abstract class RecordingPipelineBehaviorBase<TRequest, TResponse>(ConfigurationProperties.CallRecorder recorder) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly CallRecorder _recorder = recorder;

        protected abstract string Label { get; }

        public async ValueTask<Either<MediatorError, TResponse>> Handle(TRequest request, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
        {
            _recorder.Add($"behavior:{Label}:enter");
            try
            {
                return await nextStep().ConfigureAwait(false);
            }
            finally
            {
                _recorder.Add($"behavior:{Label}:exit");
            }
        }
    }

    private sealed class TrackingOuterPipelineBehavior<TRequest, TResponse>(ConfigurationProperties.CallRecorder recorder) : RecordingPipelineBehaviorBase<TRequest, TResponse>(recorder)
        where TRequest : IRequest<TResponse>
    {
        protected override string Label => "pipeline-outer";
    }

    private sealed class TrackingInnerPipelineBehavior<TRequest, TResponse>(ConfigurationProperties.CallRecorder recorder) : RecordingPipelineBehaviorBase<TRequest, TResponse>(recorder)
        where TRequest : IRequest<TResponse>
    {
        protected override string Label => "pipeline-inner";
    }

    private sealed class TrackingMetricsPipelineBehavior<TRequest, TResponse>(ConfigurationProperties.CallRecorder recorder) : RecordingPipelineBehaviorBase<TRequest, TResponse>(recorder)
        where TRequest : IRequest<TResponse>
    {
        protected override string Label => "pipeline-metrics";
    }

    private sealed class TrackingAuditPipelineBehavior<TRequest, TResponse>(ConfigurationProperties.CallRecorder recorder) : RecordingPipelineBehaviorBase<TRequest, TResponse>(recorder)
        where TRequest : IRequest<TResponse>
    {
        protected override string Label => "pipeline-audit";
    }

    private abstract class RecordingPreProcessorBase<TRequest>(ConfigurationProperties.CallRecorder recorder) : IRequestPreProcessor<TRequest>
    {
        private readonly CallRecorder _recorder = recorder;

        protected abstract string Label { get; }

        public Task Process(TRequest request, CancellationToken cancellationToken)
        {
            _recorder.Add($"pre:{Label}");
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingPreProcessorOne(ConfigurationProperties.CallRecorder recorder) : RecordingPreProcessorBase<TrackedRequest>(recorder)
    {
        protected override string Label => "pre-one";
    }

    private sealed class RecordingPreProcessorTwo(ConfigurationProperties.CallRecorder recorder) : RecordingPreProcessorBase<TrackedRequest>(recorder)
    {
        protected override string Label => "pre-two";
    }

    private sealed class GenericRecordingPreProcessor<TRequest>(ConfigurationProperties.CallRecorder recorder) : RecordingPreProcessorBase<TRequest>(recorder)
    {
        protected override string Label => "pre-generic";
    }

    private abstract class RecordingPostProcessorBase<TRequest, TResponse>(ConfigurationProperties.CallRecorder recorder) : IRequestPostProcessor<TRequest, TResponse>
    {
        private readonly CallRecorder _recorder = recorder;

        protected abstract string Label { get; }

        public Task Process(TRequest request, Either<MediatorError, TResponse> response, CancellationToken cancellationToken)
        {
            if (response.IsRight)
            {
                _recorder.Add($"post:{Label}");
            }
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingPostProcessorOne(ConfigurationProperties.CallRecorder recorder) : RecordingPostProcessorBase<TrackedRequest, int>(recorder)
    {
        protected override string Label => "post-one";
    }

    private sealed class RecordingPostProcessorTwo(ConfigurationProperties.CallRecorder recorder) : RecordingPostProcessorBase<TrackedRequest, int>(recorder)
    {
        protected override string Label => "post-two";
    }

    private sealed class GenericRecordingPostProcessor<TRequest, TResponse>(ConfigurationProperties.CallRecorder recorder) : RecordingPostProcessorBase<TRequest, TResponse>(recorder)
    {
        protected override string Label => "post-generic";
    }
}
