using Encina.Cdc.MongoDb;
using MongoDB.Driver;
using Shouldly;

namespace Encina.UnitTests.Cdc.MongoDb;

/// <summary>
/// Unit tests for <see cref="MongoCdcOptions"/> configuration class.
/// </summary>
public sealed class MongoCdcOptionsTests
{
    private static readonly string[] OrdersAndCustomers = ["orders", "customers"];
    #region Default Values

    [Fact]
    public void Defaults_ConnectionString_IsEmpty()
    {
        var options = new MongoCdcOptions();
        options.ConnectionString.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_DatabaseName_IsEmpty()
    {
        var options = new MongoCdcOptions();
        options.DatabaseName.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_CollectionNames_IsEmpty()
    {
        var options = new MongoCdcOptions();
        options.CollectionNames.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_FullDocument_IsUpdateLookup()
    {
        var options = new MongoCdcOptions();
        options.FullDocument.ShouldBe(ChangeStreamFullDocumentOption.UpdateLookup);
    }

    [Fact]
    public void Defaults_WatchDatabase_IsTrue()
    {
        var options = new MongoCdcOptions();
        options.WatchDatabase.ShouldBeTrue();
    }

    #endregion

    #region Property Setters

    [Fact]
    public void SetProperties_AllSettable()
    {
        var options = new MongoCdcOptions
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = "mydb",
            CollectionNames = ["orders", "customers"],
            FullDocument = ChangeStreamFullDocumentOption.WhenAvailable,
            WatchDatabase = false
        };

        options.ConnectionString.ShouldBe("mongodb://localhost:27017");
        options.DatabaseName.ShouldBe("mydb");
        options.CollectionNames.ShouldBe(OrdersAndCustomers);
        options.FullDocument.ShouldBe(ChangeStreamFullDocumentOption.WhenAvailable);
        options.WatchDatabase.ShouldBeFalse();
    }

    [Fact]
    public void FullDocument_CanBeSetToDefault()
    {
        var options = new MongoCdcOptions
        {
            FullDocument = ChangeStreamFullDocumentOption.Default
        };

        options.FullDocument.ShouldBe(ChangeStreamFullDocumentOption.Default);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ContainsDatabaseName()
    {
        var options = new MongoCdcOptions { DatabaseName = "mydb" };

        var result = options.ToString();

        result.ShouldContain("mydb");
    }

    [Fact]
    public void ToString_ContainsCollectionCount()
    {
        var options = new MongoCdcOptions
        {
            CollectionNames = ["orders", "customers", "products"]
        };

        var result = options.ToString();

        result.ShouldContain("3");
    }

    [Fact]
    public void ToString_MatchesExpectedFormat()
    {
        var options = new MongoCdcOptions
        {
            DatabaseName = "shopdb",
            CollectionNames = ["orders", "customers"]
        };

        var result = options.ToString();

        result.ShouldBe("MongoCdcOptions { Database=shopdb, Collections=2 }");
    }

    [Fact]
    public void ToString_EmptyCollections()
    {
        var options = new MongoCdcOptions { DatabaseName = "testdb" };

        var result = options.ToString();

        result.ShouldBe("MongoCdcOptions { Database=testdb, Collections=0 }");
    }

    #endregion

    #region JsonIgnore - ConnectionString

    [Fact]
    public void ConnectionString_HasJsonIgnoreAttribute()
    {
        var property = typeof(MongoCdcOptions)
            .GetProperty(nameof(MongoCdcOptions.ConnectionString));

        var attribute = property!.GetCustomAttributes(
            typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), false);

        attribute.ShouldNotBeEmpty();
    }

    #endregion
}
