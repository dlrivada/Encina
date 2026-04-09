using Encina.Security.Audit;
using Encina.Security.Audit.Health;
using FluentAssertions;
using NSubstitute;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Guard clause tests for audit health check types.
/// Verifies that null arguments to constructors are properly rejected.
/// </summary>
public class HealthCheckGuardTests
{
    [Fact]
    public void AuditStoreHealthCheck_Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        var act = () => new AuditStoreHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("auditStore");
    }

    [Fact]
    public void AuditStoreHealthCheck_Constructor_ValidStore_DoesNotThrow()
    {
        var store = Substitute.For<IAuditStore>();

        var act = () => new AuditStoreHealthCheck(store);

        act.Should().NotThrow();
    }

    [Fact]
    public void ReadAuditStoreHealthCheck_Constructor_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new ReadAuditStoreHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("readAuditStore");
    }

    [Fact]
    public void ReadAuditStoreHealthCheck_Constructor_ValidStore_DoesNotThrow()
    {
        var store = Substitute.For<IReadAuditStore>();

        var act = () => new ReadAuditStoreHealthCheck(store);

        act.Should().NotThrow();
    }
}
