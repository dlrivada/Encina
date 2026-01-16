using Encina.Testing.Bogus;

namespace Encina.UnitTests.AzureFunctions.Fakers;

/// <summary>
/// Faker for generating order saga data used in Durable Functions tests.
/// </summary>
public sealed class OrderSagaDataFaker : EncinaFaker<OrderSagaData>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderSagaDataFaker"/> class.
    /// </summary>
    public OrderSagaDataFaker()
    {
        RuleFor(x => x.OrderId, f => f.Random.Guid());
        RuleFor(x => x.Amount, f => f.Finance.Amount(10, 1000));
    }

    /// <summary>
    /// Configures the faker to generate saga data with specific order ID.
    /// </summary>
    /// <param name="orderId">The order ID to use.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public OrderSagaDataFaker WithOrderId(Guid orderId)
    {
        RuleFor(x => x.OrderId, _ => orderId);
        return this;
    }

    /// <summary>
    /// Configures the faker to generate saga data with specific amount.
    /// </summary>
    /// <param name="amount">The amount to use.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public OrderSagaDataFaker WithAmount(decimal amount)
    {
        RuleFor(x => x.Amount, _ => amount);
        return this;
    }
}

/// <summary>
/// Order saga data type used in Durable Functions tests.
/// </summary>
public sealed record OrderSagaData(Guid OrderId, decimal Amount);
