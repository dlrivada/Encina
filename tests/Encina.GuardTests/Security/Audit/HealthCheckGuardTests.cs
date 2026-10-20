using Encina.Security.Audit;
using Encina.Security.Audit.Health;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Guard clause tests for audit health check types.
/// Verifies that null arguments to constructors are properly rejected.
/// </summary>
public class HealthCheckGuardTests
{
    [Fact]
    public void OperationAuditStoreHealthCheck_Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        var act = () => new OperationAuditStoreHealthCheck(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("auditStore");
    }

    [Fact]
    public void OperationAuditStoreHealthCheck_Constructor_ValidStore_DoesNotThrow()
    {
        var store = Substitute.For<IOperationAuditStore>();

        var act = () => new OperationAuditStoreHealthCheck(store);

        Should.NotThrow(act);
    }

    [Fact]
    public void ReadAuditStoreHealthCheck_Constructor_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new ReadAuditStoreHealthCheck(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("readAuditStore");
    }

    [Fact]
    public void ReadAuditStoreHealthCheck_Constructor_ValidStore_DoesNotThrow()
    {
        var store = Substitute.For<IReadAuditStore>();

        var act = () => new ReadAuditStoreHealthCheck(store);

        Should.NotThrow(act);
    }
}
