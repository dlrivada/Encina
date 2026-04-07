#pragma warning disable CA1859 // Contract tests intentionally use interface types

using System.Reflection;

using Encina.Compliance.BreachNotification;

namespace Encina.ContractTests.Compliance.BreachNotification;

/// <summary>
/// Contract tests verifying the <see cref="IBreachDetectionRule"/> interface structure
/// to ensure it exposes the expected members.
/// </summary>
[Trait("Category", "Contract")]
public class IBreachDetectionRuleContractTests
{
    private static readonly Type InterfaceType = typeof(IBreachDetectionRule);

    [Fact]
    public void ShouldBeInterface()
    {
        InterfaceType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void ShouldHaveNameProperty()
    {
        var property = InterfaceType.GetProperty("Name");
        property.ShouldNotBeNull();
    }

    [Fact]
    public void ShouldHaveEvaluateAsyncMethod()
    {
        var method = InterfaceType.GetMethod("EvaluateAsync");
        method.ShouldNotBeNull();
    }
}
