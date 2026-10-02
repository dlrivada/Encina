using Encina.Cdc.MySql;
using Encina.UnitTests.Validation.Endpoints;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Encina.UnitTests.Cdc.MySql;

/// <summary>
/// Unit tests for <see cref="MySqlCdcOptionsValidator"/> (#852): Hostname and every server in the
/// connection string are validated.
/// </summary>
public sealed class MySqlCdcOptionsValidatorTests
{
    private static MySqlCdcOptions Options(string hostname, string connectionString, bool local = false) => new()
    {
        Hostname = hostname,
        ConnectionString = connectionString,
        AllowLocalEndpoints = local,
    };

    [Theory]
    [InlineData("Server=mysql.example.com;Database=app")]
    [InlineData("Server=10.0.0.5;Port=3306")]
    [InlineData("Server=db1.example.com, db2.example.com;Database=app")]
    public void Validate_RemoteServers_Succeed(string connectionString)
    {
        new MySqlCdcOptionsValidator().Validate(null, Options("mysql.example.com", connectionString)).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    public void Validate_LoopbackHostname_Fails(string hostname)
    {
        var result = new MySqlCdcOptionsValidator().Validate(null, Options(hostname, "Server=mysql.example.com"));

        result.FailureMessage.ShouldStartWith("MySqlCdcOptions.Hostname");
        result.FailureMessage.ShouldContain("AllowLocalEndpoints");
    }

    [Theory]
    [InlineData("169.254.169.254", "metadata")]
    [InlineData("0.0.0.0", "unspecified")]
    [InlineData("", "must be configured")]
    [InlineData("db:3306", "valid host")]
    public void Validate_ForbiddenHostname_Fails(string hostname, string reason)
    {
        var result = new MySqlCdcOptionsValidator().Validate(null, Options(hostname, "Server=mysql.example.com", local: true));

        result.FailureMessage.ShouldStartWith("MySqlCdcOptions.Hostname");
        result.FailureMessage.ShouldContain(reason);
    }

    [Theory]
    [InlineData("Server=localhost;Password=topsecret", "loopback")]
    [InlineData("Server=mysql.example.com,127.0.0.1;Password=topsecret", "loopback")]
    [InlineData("Database=app;Password=topsecret", "loopback")] // MySqlConnector defaults to localhost
    [InlineData("Server=169.254.169.254;Password=topsecret", "metadata")]
    [InlineData("Server=/var/run/mysqld/mysqld.sock;Protocol=Unix;Password=topsecret", "local transport")]
    [InlineData("Server=.;Protocol=Pipe;Password=topsecret", "local transport")]
    [InlineData("NotAKey=1;Password=topsecret", "not a valid MySQL connection string")]
    [InlineData("", "must be configured")]
    public void Validate_UnsafeConnectionString_FailsWithoutEchoingIt(string connectionString, string reason)
    {
        var result = new MySqlCdcOptionsValidator().Validate(null, Options("mysql.example.com", connectionString));

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldStartWith("MySqlCdcOptions.ConnectionString");
        result.FailureMessage.ShouldContain(reason);
        result.FailureMessage.ShouldNotContain("topsecret");
    }

    [Fact]
    public void Validate_LocalTransportWithOptOut_Succeeds()
    {
        var options = Options("localhost", "Server=/var/run/mysqld/mysqld.sock;Protocol=Unix", local: true);

        new MySqlCdcOptionsValidator().Validate(null, options).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_LocalhostWithOptOut_SucceedsAndWarnsOnce()
    {
        var logs = new CollectingLoggerProvider();
        using var factory = LoggerFactory.Create(b => b.AddProvider(logs));
        var sut = new MySqlCdcOptionsValidator(factory.CreateLogger<MySqlCdcOptionsValidator>());
        var options = Options("localhost", "Server=localhost", local: true);

        sut.Validate(null, options).Succeeded.ShouldBeTrue();
        sut.Validate(null, options).Succeeded.ShouldBeTrue();

        logs.Count(5400, LogLevel.Warning).ShouldBe(1);
    }

    [Fact]
    public void Validate_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new MySqlCdcOptionsValidator().Validate(null, null!));
    }
}
