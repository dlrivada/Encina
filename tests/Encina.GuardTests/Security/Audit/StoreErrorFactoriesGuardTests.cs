using Encina.Compliance.Anonymization;
using Encina.Compliance.GDPR;
using Encina.Security.Audit;
using Shouldly;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Guard clause tests for the store error factories that take the exception of a failed store operation (#1322, #2445).
/// </summary>
public class StoreErrorFactoriesGuardTests
{
    private static readonly InvalidOperationException Failure = new("failure");

    [Fact]
    public void OperationAuditErrors_StoreError_NullOperation_Throws() =>
        Should.Throw<ArgumentNullException>(() => OperationAuditErrors.StoreError(null!, Failure))
            .ParamName.ShouldBe("operation");

    [Fact]
    public void OperationAuditErrors_StoreError_NullException_Throws() =>
        Should.Throw<ArgumentNullException>(() => OperationAuditErrors.StoreError("Record", null!))
            .ParamName.ShouldBe("exception");

    [Fact]
    public void ReadAuditErrors_StoreError_NullOperation_Throws() =>
        Should.Throw<ArgumentNullException>(() => ReadAuditErrors.StoreError(null!, Failure))
            .ParamName.ShouldBe("operation");

    [Fact]
    public void ReadAuditErrors_StoreError_NullException_Throws() =>
        Should.Throw<ArgumentNullException>(() => ReadAuditErrors.StoreError("LogRead", null!))
            .ParamName.ShouldBe("exception");

    [Fact]
    public void ReadAuditErrors_PurgeFailed_NullException_Throws() =>
        Should.Throw<ArgumentNullException>(() => ReadAuditErrors.PurgeFailed(null!))
            .ParamName.ShouldBe("exception");

    [Fact]
    public void AnonymizationErrors_StoreError_NullOperation_Throws() =>
        Should.Throw<ArgumentNullException>(() => AnonymizationErrors.StoreError(null!, Failure))
            .ParamName.ShouldBe("operation");

    [Fact]
    public void AnonymizationErrors_StoreError_NullException_Throws() =>
        Should.Throw<ArgumentNullException>(() => AnonymizationErrors.StoreError("Store", null!))
            .ParamName.ShouldBe("exception");

    [Fact]
    public void GDPRErrors_ProcessingActivityStoreError_NullOperation_Throws() =>
        Should.Throw<ArgumentNullException>(() => GDPRErrors.ProcessingActivityStoreError(null!, Failure))
            .ParamName.ShouldBe("operation");

    [Fact]
    public void GDPRErrors_ProcessingActivityStoreError_NullException_Throws() =>
        Should.Throw<ArgumentNullException>(() => GDPRErrors.ProcessingActivityStoreError("GetAllActivities", null!))
            .ParamName.ShouldBe("exception");
}
