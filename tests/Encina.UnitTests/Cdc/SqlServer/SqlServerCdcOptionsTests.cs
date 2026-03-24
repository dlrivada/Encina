using Encina.Cdc.SqlServer;
using Shouldly;

namespace Encina.UnitTests.Cdc.SqlServer;

/// <summary>
/// Unit tests for <see cref="SqlServerCdcOptions"/> configuration class.
/// </summary>
public sealed class SqlServerCdcOptionsTests
{
    private static readonly string[] DboOrdersAndCustomers = ["dbo.Orders", "dbo.Customers"];
    #region Default Values

    [Fact]
    public void Defaults_ConnectionString_IsEmpty()
    {
        var options = new SqlServerCdcOptions();
        options.ConnectionString.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_TrackedTables_IsEmpty()
    {
        var options = new SqlServerCdcOptions();
        options.TrackedTables.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_SchemaName_IsDbo()
    {
        var options = new SqlServerCdcOptions();
        options.SchemaName.ShouldBe("dbo");
    }

    [Fact]
    public void Defaults_StartFromVersion_IsNull()
    {
        var options = new SqlServerCdcOptions();
        options.StartFromVersion.ShouldBeNull();
    }

    #endregion

    #region Property Setters

    [Fact]
    public void SetProperties_AllSettable()
    {
        var options = new SqlServerCdcOptions
        {
            ConnectionString = "Server=.;Database=MyDb;Trusted_Connection=True",
            TrackedTables = ["dbo.Orders", "dbo.Customers"],
            SchemaName = "sales",
            StartFromVersion = 42
        };

        options.ConnectionString.ShouldBe("Server=.;Database=MyDb;Trusted_Connection=True");
        options.TrackedTables.ShouldBe(DboOrdersAndCustomers);
        options.SchemaName.ShouldBe("sales");
        options.StartFromVersion.ShouldBe(42);
    }

    [Fact]
    public void StartFromVersion_SetToZero_ReadsAllHistory()
    {
        var options = new SqlServerCdcOptions { StartFromVersion = 0 };

        options.StartFromVersion.ShouldBe(0);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ContainsSchemaName()
    {
        var options = new SqlServerCdcOptions { SchemaName = "sales" };

        var result = options.ToString();

        result.ShouldContain("sales");
    }

    [Fact]
    public void ToString_ContainsTableCount()
    {
        var options = new SqlServerCdcOptions
        {
            TrackedTables = ["dbo.Orders", "dbo.Customers", "dbo.Products"]
        };

        var result = options.ToString();

        result.ShouldContain("3");
    }

    [Fact]
    public void ToString_MatchesExpectedFormat()
    {
        var options = new SqlServerCdcOptions
        {
            SchemaName = "dbo",
            TrackedTables = ["dbo.Orders", "dbo.Customers"]
        };

        var result = options.ToString();

        result.ShouldBe("SqlServerCdcOptions { Schema=dbo, Tables=2 }");
    }

    [Fact]
    public void ToString_EmptyTables()
    {
        var options = new SqlServerCdcOptions();

        var result = options.ToString();

        result.ShouldBe("SqlServerCdcOptions { Schema=dbo, Tables=0 }");
    }

    #endregion

    #region JsonIgnore - ConnectionString

    [Fact]
    public void ConnectionString_HasJsonIgnoreAttribute()
    {
        var property = typeof(SqlServerCdcOptions)
            .GetProperty(nameof(SqlServerCdcOptions.ConnectionString));

        var attribute = property!.GetCustomAttributes(
            typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), false);

        attribute.ShouldNotBeEmpty();
    }

    #endregion
}
