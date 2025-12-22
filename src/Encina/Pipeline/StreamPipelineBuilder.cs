using System.Runtime.CompilerServices;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;

namespace Encina;

/// <summary>
/// Contract for composing the streaming Encina pipeline (behaviors + handler) into a reusable delegate.
/// </summary>
/// <typeparam name="TRequest">Stream request type.</typeparam>
/// <typeparam name="TItem">Type of each item yielded by the stream.</typeparam>
internal interface IStreamPipelineBuilder<TRequest, TItem>
    where TRequest : IStreamRequest<TItem>
{
    /// <summary>
    /// Builds the pipeline delegate that executes stream behaviors and the handler.
    /// </summary>
    /// <param name="serviceProvider">Resolver for behaviors and handler.</param>
    /// <returns>Delegate ready to execute the stream request.</returns>
    StreamHandlerCallback<TItem> Build(IServiceProvider serviceProvider);
}

/// <summary>
/// Default streaming pipeline builder that composes stream behaviors and the handler
/// into a single delegate per request execution.
/// </summary>
/// <remarks>
/// <para><b>Pipeline Order:</b> Behaviors → Handler</para>
/// <para><b>Behavior Composition:</b> Uses nested delegates (Russian doll pattern) to chain behaviors in registration order.</para>
/// <para><b>Error Strategy:</b> Railway Oriented Programming - errors are yielded as Left values in the stream.</para>
/// </remarks>
internal sealed class StreamPipelineBuilder<TRequest, TItem> : IStreamPipelineBuilder<TRequest, TItem>
    where TRequest : IStreamRequest<TItem>
{
    private readonly TRequest _request;
    private readonly IStreamRequestHandler<TRequest, TItem> _handler;
    private readonly IRequestContext _context;
    private readonly CancellationToken _cancellationToken;

    public StreamPipelineBuilder(
        TRequest request,
        IStreamRequestHandler<TRequest, TItem> handler,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Builds the execution pipeline by wrapping the handler with stream behaviors.
    /// </summary>
    /// <remarks>
    /// Behaviors are applied in reverse (right-to-left) to ensure left-to-right execution order.
    /// Example: Register A, B → Execute A → B → Handler
    /// </remarks>
    public StreamHandlerCallback<TItem> Build(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        // Resolve all stream behaviors from DI (fall back to empty array)
        var behaviors = serviceProvider.GetServices<IStreamPipelineBehavior<TRequest, TItem>>()?.ToArray()
                        ?? Array.Empty<IStreamPipelineBehavior<TRequest, TItem>>();

        // Start with the innermost delegate: handler invocation
        StreamHandlerCallback<TItem> current = () => ExecuteHandlerAsync(_handler, _request, _cancellationToken);

        // Wrap handler with behaviors in reverse order (creates nested delegates)
        // This ensures behaviors execute in registration order when the pipeline runs
        if (behaviors.Length > 0)
        {
            for (var index = behaviors.Length - 1; index >= 0; index--)
            {
                var behavior = behaviors[index];
                var nextStep = current; // Capture in closure
                current = () => ExecuteBehaviorAsync(behavior, _request, _context, nextStep, _cancellationToken);
            }
        }

        return current;
    }

    private static async IAsyncEnumerable<Either<EncinaError, TItem>> ExecuteHandlerAsync(
        IStreamRequestHandler<TRequest, TItem> handler,
        TRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in handler.Handle(request, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    private static async IAsyncEnumerable<Either<EncinaError, TItem>> ExecuteBehaviorAsync(
        IStreamPipelineBehavior<TRequest, TItem> behavior,
        TRequest request,
        IRequestContext context,
        StreamHandlerCallback<TItem> nextStep,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in behavior.Handle(request, context, nextStep, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }
}
