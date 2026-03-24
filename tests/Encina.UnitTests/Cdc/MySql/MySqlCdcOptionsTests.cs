using Encina.Cdc.MySql;
using Shouldly;

namespace Encina.UnitTests.Cdc.MySql;

/// <summary>
/// Unit tests for <see cref="MySqlCdcOptions"/> configuration class.
/// </summary>
public sealed class MySqlCdcOptionsTests
{
    private static readonly string[] MydbAndOtherdb = ["mydb", "otherdb"];
    private static readonly string[] MydbOrdersAndCustomers = ["mydb.orders", "mydb.customers"];
    #region Default Values

    [Fact]
    public void Defaults_ConnectionString_IsEmpty()
    {
        var options = new MySqlCdcOptions();
        options.ConnectionString.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_Hostname_IsLocalhost()
    {
        var options = new MySqlCdcOptions();
        options.Hostname.ShouldBe("localhost");
    }

    [Fact]
    public void Defaults_Port_Is3306()
    {
        var options = new MySqlCdcOptions();
        options.Port.ShouldBe(3306);
    }

    [Fact]
    public void Defaults_Username_IsEmpty()
    {
        var options = new MySqlCdcOptions();
        options.Username.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_Password_IsEmpty()
    {
        var options = new MySqlCdcOptions();
        options.Password.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_ServerId_Is1()
    {
        var options = new MySqlCdcOptions();
        options.ServerId.ShouldBe(1);
    }

    [Fact]
    public void Defaults_UseGtid_IsTrue()
    {
        var options = new MySqlCdcOptions();
        options.UseGtid.ShouldBeTrue();
    }

    [Fact]
    public void Defaults_IncludeDatabases_IsEmpty()
    {
        var options = new MySqlCdcOptions();
        options.IncludeDatabases.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_IncludeTables_IsEmpty()
    {
        var options = new MySqlCdcOptions();
        options.IncludeTables.ShouldBeEmpty();
    }

    #endregion

    #region Property Setters

    [Fact]
    public void SetProperties_AllSettable()
    {
        var options = new MySqlCdcOptions
        {
            ConnectionString = "Server=localhost;Database=test",
            Hostname = "db.example.com",
            Port = 3307,
            Username = "repl_user",
            Password = "secret",
            ServerId = 42,
            UseGtid = false,
            IncludeDatabases = ["mydb", "otherdb"],
            IncludeTables = ["mydb.orders", "mydb.customers"]
        };

        options.ConnectionString.ShouldBe("Server=localhost;Database=test");
        options.Hostname.ShouldBe("db.example.com");
        options.Port.ShouldBe(3307);
        options.Username.ShouldBe("repl_user");
        options.Password.ShouldBe("secret");
        options.ServerId.ShouldBe(42);
        options.UseGtid.ShouldBeFalse();
        options.IncludeDatabases.ShouldBe(MydbAndOtherdb);
        options.IncludeTables.ShouldBe(MydbOrdersAndCustomers);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ContainsHostnameAndPort()
    {
        var options = new MySqlCdcOptions
        {
            Hostname = "db.example.com",
            Port = 3307
        };

        var result = options.ToString();

        result.ShouldContain("db.example.com:3307");
    }

    [Fact]
    public void ToString_ContainsServerId()
    {
        var options = new MySqlCdcOptions { ServerId = 42 };

        var result = options.ToString();

        result.ShouldContain("42");
    }

    [Fact]
    public void ToString_MatchesExpectedFormat()
    {
        var options = new MySqlCdcOptions
        {
            Hostname = "myhost",
            Port = 3306,
            ServerId = 7
        };

        var result = options.ToString();

        result.ShouldBe("MySqlCdcOptions { Host=myhost:3306, ServerId=7 }");
    }

    #endregion

    #region JsonIgnore Attributes

    [Fact]
    public void ConnectionString_HasJsonIgnoreAttribute()
    {
        var property = typeof(MySqlCdcOptions)
            .GetProperty(nameof(MySqlCdcOptions.ConnectionString));

        var attribute = property!.GetCustomAttributes(
            typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), false);

        attribute.ShouldNotBeEmpty();
    }

    [Fact]
    public void Password_HasJsonIgnoreAttribute()
    {
        var property = typeof(MySqlCdcOptions)
            .GetProperty(nameof(MySqlCdcOptions.Password));

        var attribute = property!.GetCustomAttributes(
            typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), false);

        attribute.ShouldNotBeEmpty();
    }

    #endregion
}
