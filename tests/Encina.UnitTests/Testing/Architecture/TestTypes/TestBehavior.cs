namespace Encina.UnitTests.Testing.Architecture.TestTypes;

/// <summary>
/// A sealed behavior for testing architecture rules.
/// </summary>
public sealed class LoggingBehavior
{
    public void Execute() { }
}

/// <summary>
/// An unsealed behavior (should fail the sealed behavior rule).
/// </summary>
public class ValidationBehavior
{
    public void Execute() { }
}
