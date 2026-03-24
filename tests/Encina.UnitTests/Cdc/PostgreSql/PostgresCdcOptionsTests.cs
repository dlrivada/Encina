using Encina.Cdc.PostgreSql;
using Shouldly;

namespace Encina.UnitTests.Cdc.PostgreSql;

/// <summary>
/// Unit tests for <see cref="PostgresCdcOptions"/> configuration class.
/// </summary>
public sealed class PostgresCdcOptionsTests
{
    private static readonly string[] PublicOrdersAndCustomers = ["public.orders", "public.customers"];
    #region Default Values

    [Fact]
    public void Defaults_ConnectionString_IsEmpty()
    {
        var options = new PostgresCdcOptions();
        options.ConnectionString.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_PublicationName_IsEncinaCdcPublication()
    {
        var options = new PostgresCdcOptions();
        options.PublicationName.ShouldBe("encina_cdc_publication");
    }

    [Fact]
    public void Defaults_ReplicationSlotName_IsEncinaCdcSlot()
    {
        var options = new PostgresCdcOptions();
        options.ReplicationSlotName.ShouldBe("encina_cdc_slot");
    }

    [Fact]
    public void Defaults_CreateSlotIfNotExists_IsTrue()
    {
        var options = new PostgresCdcOptions();
        options.CreateSlotIfNotExists.ShouldBeTrue();
    }

    [Fact]
    public void Defaults_CreatePublicationIfNotExists_IsTrue()
    {
        var options = new PostgresCdcOptions();
        options.CreatePublicationIfNotExists.ShouldBeTrue();
    }

    [Fact]
    public void Defaults_PublicationTables_IsEmpty()
    {
        var options = new PostgresCdcOptions();
        options.PublicationTables.ShouldBeEmpty();
    }

    #endregion

    #region Property Setters

    [Fact]
    public void SetProperties_AllSettable()
    {
        var options = new PostgresCdcOptions
        {
            ConnectionString = "Host=localhost;Database=test",
            PublicationName = "my_pub",
            ReplicationSlotName = "my_slot",
            CreateSlotIfNotExists = false,
            CreatePublicationIfNotExists = false,
            PublicationTables = ["public.orders", "public.customers"]
        };

        options.ConnectionString.ShouldBe("Host=localhost;Database=test");
        options.PublicationName.ShouldBe("my_pub");
        options.ReplicationSlotName.ShouldBe("my_slot");
        options.CreateSlotIfNotExists.ShouldBeFalse();
        options.CreatePublicationIfNotExists.ShouldBeFalse();
        options.PublicationTables.ShouldBe(PublicOrdersAndCustomers);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ContainsPublicationName()
    {
        var options = new PostgresCdcOptions { PublicationName = "test_pub" };

        var result = options.ToString();

        result.ShouldContain("test_pub");
    }

    [Fact]
    public void ToString_ContainsSlotName()
    {
        var options = new PostgresCdcOptions { ReplicationSlotName = "test_slot" };

        var result = options.ToString();

        result.ShouldContain("test_slot");
    }

    [Fact]
    public void ToString_MatchesExpectedFormat()
    {
        var options = new PostgresCdcOptions
        {
            PublicationName = "my_pub",
            ReplicationSlotName = "my_slot"
        };

        var result = options.ToString();

        result.ShouldBe("PostgresCdcOptions { Publication=my_pub, Slot=my_slot }");
    }

    #endregion

    #region JsonIgnore - ConnectionString

    [Fact]
    public void ConnectionString_HasJsonIgnoreAttribute()
    {
        var property = typeof(PostgresCdcOptions)
            .GetProperty(nameof(PostgresCdcOptions.ConnectionString));

        var attribute = property!.GetCustomAttributes(
            typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), false);

        attribute.ShouldNotBeEmpty();
    }

    #endregion
}
