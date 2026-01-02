using LanguageExt;

namespace Encina.Testing.Handlers;

/// <summary>
/// Fluent builder for defining and executing handler test scenarios.
/// Provides an inline BDD-style testing API as an alternative to <see cref="HandlerSpecification{TRequest,TResponse}"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
/// <remarks>
/// <para>
/// This class provides a fluent API for defining test scenarios inline:
/// </para>
/// <list type="bullet">
/// <item><description><b>Describe</b>: Create a new scenario with a description</description></item>
/// <item><description><b>Given</b>: Configure the request with modifications</description></item>
/// <item><description><b>UsingHandler</b>: Specify the handler to test</description></item>
/// <item><description><b>WhenAsync</b>: Execute the handler and get the result</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// [Fact]
/// public async Task Order_creation_scenario()
/// {
///     var result = await Scenario&lt;CreateOrder, OrderId&gt;
///         .Describe("Create order with premium customer")
///         .Given(r => r.CustomerId = "PREMIUM")
///         .Given(r => r.Items.Add(new OrderItem("PROD-001", 2, 50m)))
///         .UsingHandler(() => new CreateOrderHandler(mockRepo.Object))
///         .WhenAsync(new CreateOrder());
///
///     result.ShouldBeSuccess();
/// }
/// </code>
/// </example>
public sealed class Scenario<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly string _description;
    private readonly List<Action<TRequest>> _givenActions;
    private readonly Func<IRequestHandler<TRequest, TResponse>>? _handlerFactory;

    private Scenario(string description)
    {
        ArgumentNullException.ThrowIfNull(description);
        _description = description;
        _givenActions = new List<Action<TRequest>>();
        _handlerFactory = null;
    }

    private Scenario(
        string description,
        List<Action<TRequest>>? givenActions,
        Func<IRequestHandler<TRequest, TResponse>>? handlerFactory)
    {
        ArgumentNullException.ThrowIfNull(description);
        _description = description;
        // Make a defensive copy of the provided actions to ensure immutability
        // of the internal list regardless of the caller's reference.
        _givenActions = givenActions is null
            ? new List<Action<TRequest>>()
            : new List<Action<TRequest>>(givenActions);
        _handlerFactory = handlerFactory;
    }

    /// <summary>
    /// Gets the description of this scenario.
    /// </summary>
    public string Description => _description;

    /// <summary>
    /// Creates a new scenario with the specified description.
    /// </summary>
    /// <param name="description">A description of the scenario being tested.</param>
    /// <returns>A new <see cref="Scenario{TRequest,TResponse}"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="description"/> is null.</exception>
    /// <example>
    /// <code>
    /// Scenario&lt;CreateOrder, OrderId&gt;.Describe("Create order with valid data")
    /// </code>
    /// </example>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1000:Do not declare static members on generic types",
        Justification = "Factory method pattern is intentional for fluent API usage")]
    public static Scenario<TRequest, TResponse> Describe(string description)
    {
        return new Scenario<TRequest, TResponse>(description);
    }

    /// <summary>
    /// Adds a configuration action to modify the request.
    /// Multiple Given calls accumulate modifications.
    /// </summary>
    /// <param name="configure">Action to configure the request.</param>
    /// <returns>This scenario for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    /// <example>
    /// <code>
    /// .Given(r => r.CustomerId = "CUST-001")
    /// .Given(r => r.DiscountCode = "SAVE10")
    /// </code>
    /// </example>
    public Scenario<TRequest, TResponse> Given(Action<TRequest> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        // Return a new Scenario instance with a copied list plus the new configure action.
        var copy = new List<Action<TRequest>>(_givenActions) { configure };
        return new Scenario<TRequest, TResponse>(_description, copy, _handlerFactory);
    }

    /// <summary>
    /// Specifies the handler factory to use for this scenario.
    /// </summary>
    /// <param name="handlerFactory">Factory function that creates the handler instance.</param>
    /// <returns>This scenario for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handlerFactory"/> is null.</exception>
    /// <example>
    /// <code>
    /// .UsingHandler(() => new CreateOrderHandler(mockRepo.Object))
    /// </code>
    /// </example>
    public Scenario<TRequest, TResponse> UsingHandler(Func<IRequestHandler<TRequest, TResponse>> handlerFactory)
    {
        ArgumentNullException.ThrowIfNull(handlerFactory);
        // Return a new Scenario instance with the same given actions and the new handler factory.
        // The constructor performs null-coalescing and creates a defensive copy of the list,
        // so it's safe to pass the internal list reference directly.
        return new Scenario<TRequest, TResponse>(_description, _givenActions, handlerFactory);
    }

    /// <summary>
    /// Executes the scenario by running the handler with the configured request.
    /// </summary>
    /// <param name="request">The base request to use.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="ScenarioResult{TResponse}"/> containing the execution result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="UsingHandler"/> was not called.</exception>
    /// <example>
    /// <code>
    /// var result = await scenario.WhenAsync(new CreateOrder());
    /// result.ShouldBeSuccess();
    /// </code>
    /// </example>
    public async Task<ScenarioResult<TResponse>> WhenAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_handlerFactory is null)
        {
            throw new InvalidOperationException(
                "UsingHandler() must be called before WhenAsync(). " +
                "Specify the handler factory to use for this scenario.");
        }

        // Apply all Given modifications
        foreach (var action in _givenActions)
        {
            action(request);
        }

        try
        {
            var handler = _handlerFactory();
            var result = await handler.Handle(request, cancellationToken);
            return new ScenarioResult<TResponse>(result, request, _description);
        }
        catch (Exception ex)
        {
            return new ScenarioResult<TResponse>(ex, request, _description);
        }
    }
}
