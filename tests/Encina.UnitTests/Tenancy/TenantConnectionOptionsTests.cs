using Encina.Tenancy;

namespace Encina.UnitTests.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantConnectionOptions"/>.
/// </summary>
public class TenantConnectionOptionsTests
{
    [Fact]
    public void TenantConnectionOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new TenantConnectionOptions();

        // Assert
        options.DefaultConnectionString.ShouldBeNull();
        options.AutoOpenConnections.ShouldBeTrue();
        options.ConnectionTimeoutSeconds.ShouldBe(30);
        options.ThrowOnMissingConnectionString.ShouldBeTrue();
    }

    [Fact]
    public void TenantConnectionOptions_CanSetAllProperties()
    {
        // Arrange & Act
        var options = new TenantConnectionOptions
        {
            DefaultConnectionString = "Server=test;Database=TestDb;",
            AutoOpenConnections = false,
            ConnectionTimeoutSeconds = 60,
            ThrowOnMissingConnectionString = false
        };

        // Assert
        options.DefaultConnectionString.ShouldBe("Server=test;Database=TestDb;");
        options.AutoOpenConnections.ShouldBeFalse();
        options.ConnectionTimeoutSeconds.ShouldBe(60);
        options.ThrowOnMissingConnectionString.ShouldBeFalse();
    }
}
