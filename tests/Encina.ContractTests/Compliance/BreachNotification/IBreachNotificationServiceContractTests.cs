#pragma warning disable CA1859 // Contract tests intentionally use interface types

using System.Reflection;

using Encina.Compliance.BreachNotification.Abstractions;

namespace Encina.ContractTests.Compliance.BreachNotification;

/// <summary>
/// Contract tests verifying that <see cref="IBreachNotificationService"/> follows expected API design contracts.
/// </summary>
[Trait("Category", "Contract")]
public class IBreachNotificationServiceContractTests
{
    private static readonly Type InterfaceType = typeof(IBreachNotificationService);

    [Fact]
    public void IBreachNotificationService_ShouldBeInterface()
    {
        InterfaceType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void IBreachNotificationService_ShouldHaveRecordBreachAsyncMethod()
    {
        var method = InterfaceType.GetMethod("RecordBreachAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IBreachNotificationService_ShouldHaveAssessBreachAsyncMethod()
    {
        var method = InterfaceType.GetMethod("AssessBreachAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IBreachNotificationService_ShouldHaveReportToDPAAsyncMethod()
    {
        var method = InterfaceType.GetMethod("ReportToDPAAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IBreachNotificationService_ShouldHaveNotifySubjectsAsyncMethod()
    {
        var method = InterfaceType.GetMethod("NotifySubjectsAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IBreachNotificationService_ShouldHaveContainBreachAsyncMethod()
    {
        var method = InterfaceType.GetMethod("ContainBreachAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IBreachNotificationService_ShouldHaveCloseBreachAsyncMethod()
    {
        var method = InterfaceType.GetMethod("CloseBreachAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IBreachNotificationService_ShouldHaveGetBreachAsyncMethod()
    {
        var method = InterfaceType.GetMethod("GetBreachAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IBreachNotificationService_ShouldHaveGetApproachingDeadlineBreachesAsyncMethod()
    {
        var method = InterfaceType.GetMethod("GetApproachingDeadlineBreachesAsync");
        method.ShouldNotBeNull();
    }
}
