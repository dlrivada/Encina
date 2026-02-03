using LanguageExt;

namespace Encina.Messaging.SoftDelete;

/// <summary>
/// Pipeline behavior that configures soft delete filter state based on query requests.
/// </summary>
/// <typeparam name="TRequest">The request type being processed.</typeparam>
/// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
/// <remarks>
/// <para>
/// This behavior intercepts query requests and sets the <see cref="ISoftDeleteFilterContext.IncludeDeleted"/>
/// flag based on whether the request implements <see cref="IIncludeDeleted"/>. Repositories
/// can then check this context to determine whether to apply soft delete filters.
/// </para>
/// <para>
/// <b>Behavior Logic:</b>
/// <list type="bullet">
/// <item><description>If the request implements <see cref="IIncludeDeleted"/>, the context's
/// <c>IncludeDeleted</c> property is set to the request's value.</description></item>
/// <item><description>If the request does not implement <see cref="IIncludeDeleted"/>, the context's
/// <c>IncludeDeleted</c> property defaults to <c>false</c> (filter applied).</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Pipeline Order:</b> This behavior should run early in the pipeline, before validation
/// behaviors, to ensure the filter context is set before any data access occurs. The recommended
/// order is:
/// <list type="number">
/// <item><description>Logging/Tracing behaviors</description></item>
/// <item><description><b>SoftDeleteQueryFilterBehavior</b> (this behavior)</description></item>
/// <item><description>Validation behaviors</description></item>
/// <item><description>Authorization behaviors</description></item>
/// <item><description>Transaction behaviors</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Scope:</b> The filter context is scoped to the current request, ensuring thread safety
/// and isolation between concurrent requests.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query without soft delete support (filter always applied)
/// public sealed record GetActiveOrdersQuery : IQuery&lt;IReadOnlyList&lt;OrderDto&gt;&gt;;
///
/// // Query with optional soft delete bypass
/// public sealed record GetAllOrdersQuery(bool IncludeDeleted = false)
///     : IQuery&lt;IReadOnlyList&lt;OrderDto&gt;&gt;, IIncludeDeleted;
///
/// // Registration
/// services.AddEncinaMessaging(config =>
/// {
///     config.UseSoftDelete = true;
/// });
///
/// // Usage
/// var activeOrders = await encina.SendAsync(new GetActiveOrdersQuery()); // Soft deleted excluded
/// var allOrders = await encina.SendAsync(new GetAllOrdersQuery(IncludeDeleted: true)); // All included
/// </code>
/// </example>
public sealed class SoftDeleteQueryFilterBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISoftDeleteFilterContext _filterContext;
    private readonly SoftDeleteOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeleteQueryFilterBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="filterContext">The scoped soft delete filter context.</param>
    /// <param name="options">The soft delete configuration options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="filterContext"/> or <paramref name="options"/> is <c>null</c>.
    /// </exception>
    public SoftDeleteQueryFilterBehavior(
        ISoftDeleteFilterContext filterContext,
        SoftDeleteOptions options)
    {
        _filterContext = filterContext ?? throw new ArgumentNullException(nameof(filterContext));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Sets the <see cref="ISoftDeleteFilterContext.IncludeDeleted"/> property based on the request:
    /// </para>
    /// <list type="bullet">
    /// <item><description>If <see cref="SoftDeleteOptions.AutoFilterSoftDeletedQueries"/> is disabled,
    /// the filter context is set to include deleted entities.</description></item>
    /// <item><description>If the request implements <see cref="IIncludeDeleted"/>,
    /// the context's value is set from the request's <c>IncludeDeleted</c> property.</description></item>
    /// <item><description>Otherwise, the context defaults to <c>false</c> (filter applied).</description></item>
    /// </list>
    /// </remarks>
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        // Configure the filter context based on the request
        ConfigureFilterContext(request);

        // Continue with the next step in the pipeline
        return await nextStep().ConfigureAwait(false);
    }

    /// <summary>
    /// Configures the filter context based on the request type and options.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    private void ConfigureFilterContext(TRequest request)
    {
        // If auto-filtering is disabled globally, include all entities
        if (!_options.AutoFilterSoftDeletedQueries)
        {
            _filterContext.IncludeDeleted = true;
            return;
        }

        // Check if the request implements IIncludeDeleted
        if (request is IIncludeDeleted includeDeleted)
        {
            _filterContext.IncludeDeleted = includeDeleted.IncludeDeleted;
        }
        else
        {
            // Default: apply soft delete filter (exclude deleted entities)
            _filterContext.IncludeDeleted = false;
        }
    }
}
