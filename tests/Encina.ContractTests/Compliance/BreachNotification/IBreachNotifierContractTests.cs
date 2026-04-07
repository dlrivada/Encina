#pragma warning disable CA1859 // Contract tests intentionally use interface types

using System.Reflection;

using Encina.Compliance.BreachNotification;

namespace Encina.ContractTests.Compliance.BreachNotification;

/// <summary>
/// Contract tests verifying the <see cref="IBreachNotifier"/> interface structure
/// to ensure it exposes the expected members.
/// </summary>
[Trait("Category", "Contract")]
public class IBreachNotifierContractTests
{
    private static readonly Type InterfaceType = typeof(IBreachNotifier);

    [Fact]
    public void ShouldBeInterface()
    {
        InterfaceType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void ShouldHaveNotifyAuthorityAsyncMethod()
    {
        var method = InterfaceType.GetMethod("NotifyAuthorityAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void ShouldHaveNotifyDataSubjectsAsyncMethod()
    {
        var method = InterfaceType.GetMethod("NotifyDataSubjectsAsync");
        method.ShouldNotBeNull();
    }
}
