using Encina.Cdc;
using Encina.Cdc.Messaging;
using Shouldly;

namespace Encina.UnitTests.Cdc.Messaging;

/// <summary>
/// Unit tests for <see cref="CdcMessagingOptions"/>.
/// </summary>
public sealed class CdcMessagingOptionsTests
{
    #region Defaults

    [Fact]
    public void Defaults_TopicPattern_IsTableNameDotOperation()
    {
        var options = new CdcMessagingOptions();
        options.TopicPattern.ShouldBe("{tableName}.{operation}");
    }

    [Fact]
    public void Defaults_IncludeTables_IsEmpty()
    {
        var options = new CdcMessagingOptions();
        options.IncludeTables.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_ExcludeTables_IsEmpty()
    {
        var options = new CdcMessagingOptions();
        options.ExcludeTables.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_IncludeOperations_IsEmpty()
    {
        var options = new CdcMessagingOptions();
        options.IncludeOperations.ShouldBeEmpty();
    }

    #endregion

    #region ShouldPublish - No Filters

    [Fact]
    public void ShouldPublish_NoFilters_ReturnsTrue()
    {
        var options = new CdcMessagingOptions();

        options.ShouldPublish("Orders", ChangeOperation.Insert).ShouldBeTrue();
    }

    [Fact]
    public void ShouldPublish_NoFilters_AllOperationsAllowed()
    {
        var options = new CdcMessagingOptions();

        options.ShouldPublish("T", ChangeOperation.Insert).ShouldBeTrue();
        options.ShouldPublish("T", ChangeOperation.Update).ShouldBeTrue();
        options.ShouldPublish("T", ChangeOperation.Delete).ShouldBeTrue();
        options.ShouldPublish("T", ChangeOperation.Snapshot).ShouldBeTrue();
    }

    #endregion

    #region ShouldPublish - ExcludeTables

    [Fact]
    public void ShouldPublish_ExcludedTable_ReturnsFalse()
    {
        var options = new CdcMessagingOptions
        {
            ExcludeTables = ["__EFMigrationsHistory"]
        };

        options.ShouldPublish("__EFMigrationsHistory", ChangeOperation.Insert).ShouldBeFalse();
    }

    [Fact]
    public void ShouldPublish_ExcludedTable_CaseInsensitive()
    {
        var options = new CdcMessagingOptions
        {
            ExcludeTables = ["Orders"]
        };

        options.ShouldPublish("orders", ChangeOperation.Insert).ShouldBeFalse();
        options.ShouldPublish("ORDERS", ChangeOperation.Insert).ShouldBeFalse();
    }

    [Fact]
    public void ShouldPublish_NotExcludedTable_ReturnsTrue()
    {
        var options = new CdcMessagingOptions
        {
            ExcludeTables = ["__EFMigrationsHistory"]
        };

        options.ShouldPublish("Orders", ChangeOperation.Insert).ShouldBeTrue();
    }

    #endregion

    #region ShouldPublish - IncludeTables

    [Fact]
    public void ShouldPublish_IncludedTable_ReturnsTrue()
    {
        var options = new CdcMessagingOptions
        {
            IncludeTables = ["Orders", "Products"]
        };

        options.ShouldPublish("Orders", ChangeOperation.Insert).ShouldBeTrue();
    }

    [Fact]
    public void ShouldPublish_NotIncludedTable_ReturnsFalse()
    {
        var options = new CdcMessagingOptions
        {
            IncludeTables = ["Orders", "Products"]
        };

        options.ShouldPublish("Users", ChangeOperation.Insert).ShouldBeFalse();
    }

    [Fact]
    public void ShouldPublish_IncludedTable_CaseInsensitive()
    {
        var options = new CdcMessagingOptions
        {
            IncludeTables = ["Orders"]
        };

        options.ShouldPublish("orders", ChangeOperation.Insert).ShouldBeTrue();
    }

    #endregion

    #region ShouldPublish - ExcludeTables takes precedence

    [Fact]
    public void ShouldPublish_BothIncludedAndExcluded_ExcludeWins()
    {
        var options = new CdcMessagingOptions
        {
            IncludeTables = ["Orders", "Products"],
            ExcludeTables = ["Orders"]
        };

        options.ShouldPublish("Orders", ChangeOperation.Insert).ShouldBeFalse();
        options.ShouldPublish("Products", ChangeOperation.Insert).ShouldBeTrue();
    }

    #endregion

    #region ShouldPublish - IncludeOperations

    [Fact]
    public void ShouldPublish_IncludedOperation_ReturnsTrue()
    {
        var options = new CdcMessagingOptions
        {
            IncludeOperations = [ChangeOperation.Insert, ChangeOperation.Update]
        };

        options.ShouldPublish("T", ChangeOperation.Insert).ShouldBeTrue();
        options.ShouldPublish("T", ChangeOperation.Update).ShouldBeTrue();
    }

    [Fact]
    public void ShouldPublish_NotIncludedOperation_ReturnsFalse()
    {
        var options = new CdcMessagingOptions
        {
            IncludeOperations = [ChangeOperation.Insert]
        };

        options.ShouldPublish("T", ChangeOperation.Delete).ShouldBeFalse();
        options.ShouldPublish("T", ChangeOperation.Update).ShouldBeFalse();
    }

    #endregion

    #region ShouldPublish - Combined Filters

    [Fact]
    public void ShouldPublish_CombinedFilters_AllMustPass()
    {
        var options = new CdcMessagingOptions
        {
            IncludeTables = ["Orders"],
            ExcludeTables = ["Products"],
            IncludeOperations = [ChangeOperation.Insert]
        };

        // Passes all filters
        options.ShouldPublish("Orders", ChangeOperation.Insert).ShouldBeTrue();

        // Fails operation filter
        options.ShouldPublish("Orders", ChangeOperation.Delete).ShouldBeFalse();

        // Fails include table filter
        options.ShouldPublish("Users", ChangeOperation.Insert).ShouldBeFalse();

        // Fails exclude filter
        options.ShouldPublish("Products", ChangeOperation.Insert).ShouldBeFalse();
    }

    #endregion
}
