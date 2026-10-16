using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina;

/// <summary>
/// Contract for composing the Encina pipeline (behaviors + handler) into a reusable delegate.
/// Intended for future refactor to minimize reflection and allocations in the hot path.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
internal interface IPipelineBuilder<TRequest, TResponse> // NOSONAR S2326: TRequest constraint enforces type safety
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Builds the pipeline delegate that executes behaviors and the handler.
    /// Implementations should keep the rail funcional (ValueTask&lt;Either&lt;EncinaError,TResponse&gt;&gt;).
    /// </summary>
    /// <param name="serviceProvider">Resolver for behaviors, pre/post processors y handler.</param>
    /// <returns>Delegate ready to execute the request.</returns>
    RequestHandlerCallback<TResponse> Build(IServiceProvider serviceProvider);
}

/// <summary>
/// Default pipeline builder that composes behaviors and processor steps around the handler
/// into a single delegate per request execution.
/// </summary>
/// <remarks>
/// <para><b>Pipeline Order:</b> steps run from the outermost to the innermost in (<see cref="PipelineStage"/>, order,
/// tie-break) order, computed once per closed pipeline by the <see cref="PipelineOrderResolver"/>. Registration order
/// only breaks ties between behaviors that declare the same stage and order.</para>
/// <para><b>Processors:</b> pre-, post-, error- and post-commit processors are adapted into steps and placed by the same
/// rule (default stage <see cref="PipelineStage.Application"/>, inside every gate).</para>
/// <para><b>Behavior Composition:</b> Uses nested delegates (Russian doll pattern) over the resolved order.</para>
/// <para><b>Error Strategy:</b> Railway Oriented Programming - errors short-circuit via Either&lt;EncinaError, TResponse&gt;.</para>
/// </remarks>
internal sealed class PipelineBuilder<TRequest, TResponse> : IPipelineBuilder<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly TRequest _request;
    private readonly IRequestHandler<TRequest, TResponse> _handler;
    private readonly IRequestContext _context;
    private readonly CancellationToken _cancellationToken;
    private readonly PipelineOrderResolver _resolver;

    public PipelineBuilder(
        TRequest request,
        IRequestHandler<TRequest, TResponse> handler,
        IRequestContext context,
        PipelineOrderResolver resolver,
        CancellationToken cancellationToken)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Builds the execution pipeline by wrapping the handler with the behaviors and processor steps.
    /// </summary>
    /// <remarks>
    /// Steps are wrapped from the innermost to the outermost position of the resolved order, so the first
    /// element of the order runs first. With no behaviors and no processors the handler is returned directly.
    /// </remarks>
    public RequestHandlerCallback<TResponse> Build(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var steps = ResolveSteps(serviceProvider, out var candidates);

        // Start with the innermost delegate: handler invocation
        RequestHandlerCallback<TResponse> current = () => ExecuteHandlerAsync(_handler, _request, _cancellationToken);
        if (steps.Length == 0)
        {
            return current;
        }

        var order = _resolver.Resolve(typeof(IPipelineBehavior<TRequest, TResponse>), candidates);
        for (var index = order.Length - 1; index >= 0; index--)
        {
            var step = steps[order[index]];
            var nextStep = current; // Capture in closure
            current = () => ExecuteBehaviorAsync(step, _request, _context, nextStep, _cancellationToken);
        }

        return current;
    }

    /// <summary>
    /// Resolves the behaviors from DI, wraps the processors into steps and describes every step to the resolver.
    /// </summary>
    /// <remarks>
    /// Behaviors come first in DI order, then pre-, post-, error- and post-commit processors. Processors are
    /// wrapped only when registered, so a pipeline without processors allocates no step.
    /// </remarks>
    private static IPipelineBehavior<TRequest, TResponse>[] ResolveSteps(
        IServiceProvider serviceProvider,
        out PipelineStepCandidate[] candidates)
    {
        // GetServices<T>() never returns null - always empty enumeration
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();
        var processors = WrapProcessors(serviceProvider);

        // No processor to wrap: the resolved behaviors are the steps
        var steps = processors is null ? behaviors : [.. behaviors, .. processors];
        candidates = steps.Length == 0 ? [] : DescribeSteps(steps);
        return steps;
    }

    private static List<IPipelineBehavior<TRequest, TResponse>>? WrapProcessors(IServiceProvider serviceProvider)
    {
        List<IPipelineBehavior<TRequest, TResponse>>? steps = null;
        AddSteps(ref steps, serviceProvider.GetServices<IRequestPreProcessor<TRequest>>().ToArray(),
            static processor => new PreProcessorStep<TRequest, TResponse>(processor));
        AddSteps(ref steps, serviceProvider.GetServices<IRequestPostProcessor<TRequest, TResponse>>().ToArray(),
            static processor => new PostProcessorStep<TRequest, TResponse>(processor));
        AddSteps(ref steps, serviceProvider.GetServices<IRequestErrorProcessor<TRequest, TResponse>>().ToArray(),
            static processor => new ErrorProcessorStep<TRequest, TResponse>(processor));
        AddSteps(ref steps, serviceProvider.GetServices<IRequestPostCommitProcessor<TRequest, TResponse>>().ToArray(),
            static processor => new PostCommitProcessorStep<TRequest, TResponse>(processor));
        return steps;
    }

    private static void AddSteps<TProcessor>(
        ref List<IPipelineBehavior<TRequest, TResponse>>? steps,
        TProcessor[] processors,
        Func<TProcessor, IPipelineBehavior<TRequest, TResponse>> toStep)
    {
        for (var i = 0; i < processors.Length; i++)
        {
            (steps ??= []).Add(toStep(processors[i]));
        }
    }

    private static PipelineStepCandidate[] DescribeSteps(IPipelineBehavior<TRequest, TResponse>[] steps)
    {
        var candidates = new PipelineStepCandidate[steps.Length];
        for (var i = 0; i < steps.Length; i++)
        {
            candidates[i] = PipelineStepCandidate.For(steps[i], i);
        }

        return candidates;
    }

    /// <summary>
    /// Executes the handler and returns its result directly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pure Railway Oriented Programming: Handlers return <see cref="Either{L,R}"/> and are responsible
    /// for converting all expected errors to Left values. Any exception that escapes indicates a bug
    /// in the handler and will propagate (fail-fast).
    /// </para>
    /// <para>
    /// Cancellation is the only exception we handle, as it represents expected cooperative cancellation
    /// rather than a programming error.
    /// </para>
    /// </remarks>
    private static async ValueTask<Either<EncinaError, TResponse>> ExecuteHandlerAsync(
        IRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await handler.Handle(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation is expected behavior, convert to Left
            var message = $"Handler {handler.GetType().Name} was cancelled while processing {typeof(TRequest).Name}.";
            var metadata = new Dictionary<string, object?>
            {
                ["handler"] = handler.GetType().FullName,
                ["request"] = typeof(TRequest).FullName,
                ["stage"] = "handler"
            };
            return Left<EncinaError, TResponse>(EncinaErrors.Create(EncinaErrorCodes.HandlerCancelled, message, ex, metadata)); // NOSONAR S6966: LanguageExt Left is a pure function
        }
        // Any other exception (e.g., NullReferenceException, InvalidOperationException) indicates
        // a bug in the handler and will propagate to let the application crash (fail-fast)
    }

    private static async ValueTask<Either<EncinaError, TResponse>> ExecuteBehaviorAsync(
        IPipelineBehavior<TRequest, TResponse> behavior,
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        try
        {
            return await behavior.Handle(request, context, nextStep, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"Behavior {behavior.GetType().Name} cancelled the {typeof(TRequest).Name} request.";
            var metadata = new Dictionary<string, object?>
            {
                ["behavior"] = behavior.GetType().FullName,
                ["request"] = typeof(TRequest).FullName,
                ["stage"] = "behavior"
            };
            return Left<EncinaError, TResponse>(EncinaErrors.Create(EncinaErrorCodes.BehaviorCancelled, message, ex, metadata)); // NOSONAR S6966: LanguageExt Left is a pure function
        }
        // Pure ROP: Any other exception indicates a bug in the behavior and will propagate (fail-fast)
    }
}
