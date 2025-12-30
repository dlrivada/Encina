namespace Encina.Testing.Architecture.Tests.TestTypes;

/// <summary>
/// A correctly named validator for testing architecture rules.
/// </summary>
public sealed class CreateOrderValidator
{
    public static bool IsValid() => true;
}

/// <summary>
/// Another correctly named validator.
/// </summary>
public sealed class UpdateOrderCommandValidator
{
    public static bool IsValid() => true;
}
