#pragma warning disable CA1859 // Contract tests intentionally use interface types

using System.Reflection;

using Encina.Compliance.BreachNotification;

namespace Encina.ContractTests.Compliance.BreachNotification;

/// <summary>
/// Contract tests verifying the <see cref="IBreachDetector"/> interface structure
/// to ensure it exposes the expected members.
/// </summary>
[Trait("Category", "Contract")]
public class IBreachDetectorContractTests
{
    private static readonly Type InterfaceType = typeof(IBreachDetector);

    [Fact]
    public void ShouldBeInterface()
    {
        InterfaceType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void ShouldHaveDetectAsyncMethod()
    {
        var method = InterfaceType.GetMethod("DetectAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void ShouldHaveRegisterDetectionRuleMethod()
    {
        var method = InterfaceType.GetMethod("RegisterDetectionRule");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void ShouldHaveGetRegisteredRulesAsyncMethod()
    {
        var method = InterfaceType.GetMethod("GetRegisteredRulesAsync");
        method.ShouldNotBeNull();
    }
}
