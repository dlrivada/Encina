using Encina.DomainModeling;

namespace Encina.GuardTests.DomainModeling;

public class RepositoryErrorsTenantRequiredGuardTests
{
    [Fact]
    public void TenantRequired_NullOperation_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            RepositoryErrors.TenantRequired<RepositoryErrorsTenantRequiredGuardTests>(null!));
}
