using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Encina;

/// <summary>
/// Validates the order of every closed pipeline the host can enumerate when it starts, so a pipeline that breaks a
/// rule stops the host instead of serving requests.
/// </summary>
/// <remarks>
/// <para>
/// For every closed <c>IRequestHandler&lt;,&gt;</c> and <c>IStreamRequestHandler&lt;,&gt;</c> registered, the service reads the
/// descriptors of the behaviors and processors that apply to it and asks the <see cref="PipelineOrderResolver"/> to
/// resolve, validate and cache the order. Ordering depends on types, not instances, so only factory-registered steps
/// (for example module behavior adapters) are instantiated. Open-generic handlers are skipped; their pipelines are
/// validated before their first request.
/// </para>
/// <para>
/// A rule violation fails <see cref="StartAsync"/> with an <see cref="InvalidOperationException"/> naming the rule, the
/// pipeline and the step types. A step factory that throws is reported as a resolution error naming the service
/// type, separate from the order rules. There is no option to downgrade a violation: an order that cannot be proven
/// safe never runs.
/// </para>
/// </remarks>
internal sealed class PipelineOrderValidationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PipelineRegistrationSnapshot _snapshot;
    private readonly PipelineOrderResolver _resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineOrderValidationHostedService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The provider the validation scope is opened on.</param>
    /// <param name="snapshot">The registrations of the host.</param>
    /// <param name="resolver">The resolver that validates and caches the orders.</param>
    public PipelineOrderValidationHostedService(
        IServiceProvider serviceProvider,
        PipelineRegistrationSnapshot snapshot,
        PipelineOrderResolver resolver)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _resolver.ValidateRegistrations();

        using var scope = _serviceProvider.CreateScope();
        foreach (var handlerType in _snapshot.GetClosedHandlerServiceTypes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ValidatePipelineAsync(scope.ServiceProvider, handlerType).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async ValueTask ValidatePipelineAsync(IServiceProvider provider, Type handlerType)
    {
        var arguments = handlerType.GenericTypeArguments;
        if (handlerType.GetGenericTypeDefinition() == typeof(IStreamRequestHandler<,>))
        {
            var streamKey = typeof(IStreamPipelineBehavior<,>).MakeGenericType(arguments);
            await WarmAsync(provider, streamKey, [(streamKey, PipelineStepKind.Behavior)]).ConfigureAwait(false);
            return;
        }

        var key = typeof(IPipelineBehavior<,>).MakeGenericType(arguments);
        await WarmAsync(provider, key,
        [
            (key, PipelineStepKind.Behavior),
            (typeof(IRequestPreProcessor<>).MakeGenericType(arguments[0]), PipelineStepKind.PreProcessor),
            (typeof(IRequestPostProcessor<,>).MakeGenericType(arguments), PipelineStepKind.PostProcessor),
            (typeof(IRequestErrorProcessor<,>).MakeGenericType(arguments), PipelineStepKind.ErrorProcessor),
            (typeof(IRequestPostCommitProcessor<,>).MakeGenericType(arguments), PipelineStepKind.PostCommitProcessor)
        ]).ConfigureAwait(false);
    }

    // The sequence mirrors the builders (behaviors first, then pre, post, error and post-commit processors), so the
    // runtime resolution of the same pipeline is a cache hit
    private async ValueTask WarmAsync(IServiceProvider provider, Type pipelineKey, (Type ServiceType, PipelineStepKind Kind)[] sources)
    {
        var candidates = new List<PipelineStepCandidate>();
        foreach (var (serviceType, kind) in sources)
        {
            foreach (var registration in _snapshot.GetRegistrations(serviceType))
            {
                candidates.Add(await DescribeAsync(provider, pipelineKey, registration, kind, candidates.Count).ConfigureAwait(false));
            }
        }

        _resolver.Warm(pipelineKey, [.. candidates]);
    }

    private static async ValueTask<PipelineStepCandidate> DescribeAsync(
        IServiceProvider provider,
        Type pipelineKey,
        PipelineRegistration registration,
        PipelineStepKind kind,
        int index)
    {
        // Types and instances are described by their type; only a factory has to run to learn what it builds
        if (registration.ImplementationType is { } type)
        {
            return new PipelineStepCandidate(type, null, kind, index);
        }

        var instance = Instantiate(provider, pipelineKey, registration.Descriptor);
        try
        {
            return instance is IPipelineStepIdentity
                ? PipelineStepCandidate.For(instance, index)
                : new PipelineStepCandidate(instance.GetType(), null, kind, index);
        }
        finally
        {
            await DisposeIfOwnedAsync(instance, registration.Descriptor).ConfigureAwait(false);
        }
    }

    // Only the type was needed. A transient factory result belongs to nobody else and is released here; the result
    // of a singleton or scoped factory (for example an alias to a container-owned instance) belongs to the container
    private static async ValueTask DisposeIfOwnedAsync(object instance, ServiceDescriptor descriptor)
    {
        if (descriptor.Lifetime != ServiceLifetime.Transient)
        {
            return;
        }

        if (instance is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else if (instance is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static object Instantiate(IServiceProvider provider, Type pipelineKey, ServiceDescriptor descriptor)
    {
        try
        {
            return descriptor.ImplementationFactory!(provider);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Pipeline step resolution failed for {PipelineOrderValidator.NameOf(pipelineKey)}: "
                + $"the factory registered for {PipelineOrderValidator.NameOf(descriptor.ServiceType)} threw {ex.GetType().Name}. "
                + "This is a registration error, not an order rule.");
        }
    }
}
