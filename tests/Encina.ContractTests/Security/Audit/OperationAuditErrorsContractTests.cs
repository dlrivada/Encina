using Encina.Security.Audit;
using Shouldly;

namespace Encina.ContractTests.Security.Audit;

/// <summary>
/// Contract tests for the error codes and shapes <see cref="OperationAuditErrors"/> exposes to callers.
/// </summary>
public class OperationAuditErrorsContractTests
{
    [Fact]
    public void MaskingFailed_CarriesTheStableCodeAndTheDetails()
    {
        var error = OperationAuditErrors.MaskingFailed("PatientCommand", "InvalidOperationException");

        OperationAuditErrors.MaskingFailedCode.ShouldBe("audit.masking_failed");
        error.GetCode().IfNone(string.Empty).ShouldBe(OperationAuditErrors.MaskingFailedCode);
        var details = error.GetDetails();
        details["requestType"].ShouldBe("PatientCommand");
        details["cause"].ShouldBe("InvalidOperationException");
    }

    [Fact]
    public void WriteFailed_CarriesTheStableCodeAndTheDetails()
    {
        var error = OperationAuditErrors.WriteFailed("PatientCommand", "store.failed");

        OperationAuditErrors.WriteFailedCode.ShouldBe("audit.write_failed");
        error.GetCode().IfNone(string.Empty).ShouldBe(OperationAuditErrors.WriteFailedCode);
        var details = error.GetDetails();
        details["requestType"].ShouldBe("PatientCommand");
        details["cause"].ShouldBe("store.failed");
    }

    [Fact]
    public void StoreError_CarriesTheStableCodeTheExceptionTypeAndNeverItsMessage()
    {
        var exception = new InvalidOperationException("SECRET-42");

        var error = OperationAuditErrors.StoreError("Record", exception);

        OperationAuditErrors.StoreErrorCode.ShouldBe("audit.store_error");
        error.GetCode().IfNone(string.Empty).ShouldBe(OperationAuditErrors.StoreErrorCode);
        error.GetDetails()["operation"].ShouldBe("Record");
        error.Message.ShouldContain(nameof(InvalidOperationException));
        error.Message.ShouldNotContain("SECRET-42");
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void MaskingFailedAndWriteFailed_UseDistinctCodes()
    {
        OperationAuditErrors.MaskingFailedCode.ShouldNotBe(OperationAuditErrors.WriteFailedCode);
    }
}
