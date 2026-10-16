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
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _resolver.ValidateRegistrations();

        using var scope = _serviceProvider.CreateScope();
        foreach (var handlerType in _snapshot.GetClosedHandlerServiceTypes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidatePipeline(scope.ServiceProvider, handlerType);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void ValidatePipeline(IServiceProvider provider, Type handlerType)
    {
        var arguments = handlerType.GenericTypeArguments;
        if (handlerType.GetGenericTypeDefinition() == typeof(IStreamRequestHandler<,>))
        {
            var streamKey = typeof(IStreamPipelineBehavior<,>).MakeGenericType(arguments);
            Warm(provider, streamKey, [(streamKey, PipelineStepKind.Behavior)]);
            return;
        }

        var key = typeof(IPipelineBehavior<,>).MakeGenericType(arguments);
        Warm(provider, key,
        [
            (key, PipelineStepKind.Behavior),
            (typeof(IRequestPreProcessor<>).MakeGenericType(arguments[0]), PipelineStepKind.PreProcessor),
            (typeof(IRequestPostProcessor<,>).MakeGenericType(arguments), PipelineStepKind.PostProcessor),
            (typeof(IRequestErrorProcessor<,>).MakeGenericType(arguments), PipelineStepKind.ErrorProcessor),
            (typeof(IRequestPostCommitProcessor<,>).MakeGenericType(arguments), PipelineStepKind.PostCommitProcessor)
        ]);
    }

    // The sequence mirrors the builders (behaviors first, then pre, post, error and post-commit processors), so the
    // runtime resolution of the same pipeline is a cache hit
    private void Warm(IServiceProvider provider, Type pipelineKey, (Type ServiceType, PipelineStepKind Kind)[] sources)
    {
        var candidates = new List<PipelineStepCandidate>();
        foreach (var (serviceType, kind) in sources)
        {
            foreach (var registration in _snapshot.GetRegistrations(serviceType))
            {
                candidates.Add(Describe(provider, pipelineKey, registration, kind, candidates.Count));
            }
        }

        _resolver.Warm(pipelineKey, [.. candidates]);
    }

    private static PipelineStepCandidate Describe(
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
            // Only the type was needed; the scope does not track an instance created by calling the factory directly
            (instance as IDisposable)?.Dispose();
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
