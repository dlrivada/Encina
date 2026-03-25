using Encina.Tenancy;
using Shouldly;

namespace Encina.UnitTests.Tenancy;

/// <summary>
/// Extended unit tests for <see cref="TenantConnectionOptions"/> covering ToString.
/// </summary>
public sealed class TenantConnectionOptionsExtendedTests
{
    [Fact]
    public void ToString_ContainsAutoOpenAndTimeout()
    {
        var options = new TenantConnectionOptions
        {
            AutoOpenConnections = false,
            ConnectionTimeoutSeconds = 45
        };

        var result = options.ToString();
        result.ShouldContain("AutoOpen=False");
        result.ShouldContain("45s");
    }

    [Fact]
    public void ToString_WithDefaults_ContainsDefaultValues()
    {
        var options = new TenantConnectionOptions();

        var result = options.ToString();
        result.ShouldContain("AutoOpen=True");
        result.ShouldContain("30s");
    }
}
