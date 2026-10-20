using Encina.Security.Audit;
using Shouldly;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Guard tests for <see cref="OperationAuditStoreRegistration"/>.
/// </summary>
public sealed class OperationAuditStoreRegistrationGuardTests
{
    [Fact]
    public void RemoveInMemoryDefault_NullServices_ThrowsArgumentNullException()
    {
        var act = () => OperationAuditStoreRegistration.RemoveInMemoryDefault(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void IsInMemoryDefault_NullDescriptor_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => OperationAuditStoreRegistration.IsInMemoryDefault(null!)).ParamName.ShouldBe("descriptor");
    }
}
