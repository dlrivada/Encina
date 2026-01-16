namespace Encina.UnitTests.Testing.Architecture.TestTypes;

/// <summary>
/// A sealed handler test fixture with no real implementation, used for testing architecture rules.
/// </summary>
/// <remarks>
/// This is a no-op handler for architecture tests only. Do not add production logic.
/// </remarks>
public sealed class CreateOrderHandler
{
    public void Handle() { }
}

/// <summary>
/// An unsealed handler test fixture with no real implementation, used for testing architecture rules.
/// Intentionally left unsealed to verify the sealed handler rule fails for non-sealed handlers.
/// </summary>
public class UpdateOrderHandler
{
    public void Handle() { }
}
