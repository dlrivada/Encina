using Encina.Security.Audit;
using Shouldly;

namespace Encina.ContractTests.Security.Audit;

/// <summary>The fail-closed default and the audit error code are part of the package contract (#1869).</summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Audit")]
public sealed class OperationAuditFailureModeContractTests
{
    [Fact]
    public void Options_DefaultToFailClosed() =>
        new OperationAuditOptions().FailureMode.ShouldBe(OperationAuditFailureMode.FailClosed);

    [Fact]
    public void FailClosed_IsTheZeroValue() =>
        ((int)OperationAuditFailureMode.FailClosed).ShouldBe(0);

    [Fact]
    public void WriteFailed_CarriesTheContractCodeAndNoCauseMessage()
    {
        var error = OperationAuditErrors.WriteFailed("CreateOrder", "InvalidOperationException");

        error.GetCode().IfNone(string.Empty).ShouldBe("audit.write_failed");
        error.GetCode().IfNone(string.Empty).ShouldBe(OperationAuditErrors.WriteFailedCode);
        error.Message.ShouldContain("CreateOrder");
    }
}
