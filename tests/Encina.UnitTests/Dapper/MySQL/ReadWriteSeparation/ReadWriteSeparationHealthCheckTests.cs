using Encina.Dapper.MySQL.ReadWriteSeparation;
using Encina.Messaging.Health;
using Encina.Messaging.ReadWriteSeparation;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.MySQL.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="ReadWriteSeparationHealthCheck"/> (Dapper, MySQL).
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReadWriteSeparationHealthCheckTests
{
    // An unsupported keyword makes the connection fail instantly with an exception whose message
    // names the keyword; the host name stands in for sensitive connection data.
    private const string FailingConnectionString = "Server=db-secret-host.internal;Bogus Keyword=1";

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new ReadWriteSeparationHealthCheck(null!));
    }

    [Fact]
    public void DefaultName_HasExpectedValue()
    {
        ReadWriteSeparationHealthCheck.DefaultName.ShouldBe("encina-read-write-separation-dapper-mysql");
    }

    [Fact]
    public void NameAndTags_AreExposed()
    {
        var healthCheck = new ReadWriteSeparationHealthCheck(new ReadWriteSeparationOptions());

        healthCheck.Name.ShouldBe(ReadWriteSeparationHealthCheck.DefaultName);
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("read-write-separation");
        healthCheck.Tags.ShouldContain("dapper");
        healthCheck.Tags.ShouldContain("mysql");
        (healthCheck is EncinaHealthCheck).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CheckHealthAsync_WithoutWriteConnectionString_ReturnsUnhealthy(string? writeConnectionString)
    {
        var healthCheck = new ReadWriteSeparationHealthCheck(
            new ReadWriteSeparationOptions { WriteConnectionString = writeConnectionString });

        var result = await healthCheck.CheckHealthAsync();

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("not configured");
        result.Data.ShouldContainKey("primary");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPrimaryConnectionFails_ReportsOnlyTheExceptionType()
    {
        var healthCheck = new ReadWriteSeparationHealthCheck(
            new ReadWriteSeparationOptions { WriteConnectionString = FailingConnectionString });

        var result = await healthCheck.CheckHealthAsync();

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Exception");
        result.Description!.ShouldNotContain("bogus", Case.Insensitive);
        result.Description!.ShouldNotContain("db-secret-host");
        var primary = result.Data["primary"].ToString()!;
        primary.ShouldStartWith("unreachable: ");
        primary.ShouldNotContain("bogus", Case.Insensitive);
        primary.ShouldNotContain("db-secret-host");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPrimaryFails_DoesNotReachReplicas()
    {
        var options = new ReadWriteSeparationOptions { WriteConnectionString = FailingConnectionString };
        options.ReadConnectionStrings.Add(FailingConnectionString);
        var healthCheck = new ReadWriteSeparationHealthCheck(options);

        var result = await healthCheck.CheckHealthAsync();

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Data.ShouldNotContainKey("replica_0");
    }
}
