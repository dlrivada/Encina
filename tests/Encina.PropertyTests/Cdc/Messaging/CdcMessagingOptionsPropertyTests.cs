using Encina.Cdc;
using Encina.Cdc.Messaging;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Cdc.Messaging;

/// <summary>
/// Property-based tests for <see cref="CdcMessagingOptions"/> invariants.
/// Verifies filter logic including table inclusion/exclusion, operation filtering, and precedence rules.
/// </summary>
[Trait("Category", "Property")]
public sealed class CdcMessagingOptionsPropertyTests
{
    private static readonly ChangeOperation[] AllOperations =
        Enum.GetValues<ChangeOperation>();

    #region Empty Filters Always Return True

    [Property(MaxTest = 100)]
    public bool Property_ShouldPublish_EmptyFilters_AlwaysReturnsTrue(NonEmptyString tableName, int operationIndex)
    {
        // Property: ShouldPublish with empty filters always returns true for any table/operation
        var options = new CdcMessagingOptions();
        var operation = AllOperations[((operationIndex % AllOperations.Length) + AllOperations.Length) % AllOperations.Length];

        return options.ShouldPublish(tableName.Get, operation);
    }

    #endregion

    #region ExcludeTables Filtering

    [Property(MaxTest = 100)]
    public bool Property_ShouldPublish_ExcludeTables_ExcludesExactTable(NonEmptyString tableName, int operationIndex)
    {
        // Property: ShouldPublish with ExcludeTables excludes the exact table
        var operation = AllOperations[((operationIndex % AllOperations.Length) + AllOperations.Length) % AllOperations.Length];
        var options = new CdcMessagingOptions
        {
            ExcludeTables = [tableName.Get],
        };

        return !options.ShouldPublish(tableName.Get, operation);
    }

    [Property(MaxTest = 100)]
    public bool Property_ShouldPublish_ExcludeTables_IsCaseInsensitive(NonEmptyString tableName, int operationIndex)
    {
        // Property: ExcludeTables comparison is case-insensitive
        var operation = AllOperations[((operationIndex % AllOperations.Length) + AllOperations.Length) % AllOperations.Length];
        var options = new CdcMessagingOptions
        {
            ExcludeTables = [tableName.Get.ToUpperInvariant()],
        };

        return !options.ShouldPublish(tableName.Get.ToLowerInvariant(), operation);
    }

    [Property(MaxTest = 100)]
    public bool Property_ShouldPublish_ExcludeTables_DoesNotExcludeOtherTables(int operationIndex)
    {
        // Property: Excluding one table does not affect other tables
        var operation = AllOperations[((operationIndex % AllOperations.Length) + AllOperations.Length) % AllOperations.Length];
        var options = new CdcMessagingOptions
        {
            ExcludeTables = ["ExcludedTable"],
        };

        return options.ShouldPublish("OtherTable", operation);
    }

    #endregion

    #region IncludeTables Filtering

    [Property(MaxTest = 100)]
    public bool Property_ShouldPublish_IncludeTables_IncludesSpecifiedTable(NonEmptyString tableName, int operationIndex)
    {
        // Property: ShouldPublish with IncludeTables includes the specified table
        var operation = AllOperations[((operationIndex % AllOperations.Length) + AllOperations.Length) % AllOperations.Length];
        var options = new CdcMessagingOptions
        {
            IncludeTables = [tableName.Get],
        };

        return options.ShouldPublish(tableName.Get, operation);
    }

    [Property(MaxTest = 100)]
    public bool Property_ShouldPublish_IncludeTables_ExcludesNonSpecifiedTables(int operationIndex)
    {
        // Property: ShouldPublish with IncludeTables excludes tables not in the list
        var operation = AllOperations[((operationIndex % AllOperations.Length) + AllOperations.Length) % AllOperations.Length];
        var options = new CdcMessagingOptions
        {
            IncludeTables = ["IncludedTable"],
        };

        return !options.ShouldPublish("NonIncludedTable", operation);
    }

    [Property(MaxTest = 100)]
    public bool Property_ShouldPublish_IncludeTables_IsCaseInsensitive(NonEmptyString tableName, int operationIndex)
    {
        // Property: IncludeTables comparison is case-insensitive
        var operation = AllOperations[((operationIndex % AllOperations.Length) + AllOperations.Length) % AllOperations.Length];
        var options = new CdcMessagingOptions
        {
            IncludeTables = [tableName.Get.ToUpperInvariant()],
        };

        return options.ShouldPublish(tableName.Get.ToLowerInvariant(), operation);
    }

    #endregion

    #region IncludeOperations Filtering

    [Property(MaxTest = 100)]
    public bool Property_ShouldPublish_IncludeOperations_IncludesSpecifiedOperation(NonEmptyString tableName, int operationIndex)
    {
        // Property: ShouldPublish with IncludeOperations includes the specified operation
        var operation = AllOperations[((operationIndex % AllOperations.Length) + AllOperations.Length) % AllOperations.Length];
        var options = new CdcMessagingOptions
        {
            IncludeOperations = [operation],
        };

        return options.ShouldPublish(tableName.Get, operation);
    }

    [Property(MaxTest = 100)]
    public bool Property_ShouldPublish_IncludeOperations_ExcludesNonSpecifiedOperations(NonEmptyString tableName)
    {
        // Property: ShouldPublish with IncludeOperations containing only Insert excludes Update
        var options = new CdcMessagingOptions
        {
            IncludeOperations = [ChangeOperation.Insert],
        };

        return !options.ShouldPublish(tableName.Get, ChangeOperation.Update)
            && !options.ShouldPublish(tableName.Get, ChangeOperation.Delete)
            && !options.ShouldPublish(tableName.Get, ChangeOperation.Snapshot);
    }

    #endregion

    #region ExcludeTables Takes Precedence Over IncludeTables

    [Property(MaxTest = 100)]
    public bool Property_ShouldPublish_ExcludeTakesPrecedenceOverInclude(NonEmptyString tableName, int operationIndex)
    {
        // Property: A table that appears in both ExcludeTables and IncludeTables is excluded
        var operation = AllOperations[((operationIndex % AllOperations.Length) + AllOperations.Length) % AllOperations.Length];
        var options = new CdcMessagingOptions
        {
            IncludeTables = [tableName.Get],
            ExcludeTables = [tableName.Get],
        };

        return !options.ShouldPublish(tableName.Get, operation);
    }

    [Property(MaxTest = 100)]
    public bool Property_ShouldPublish_ExcludeTakesPrecedenceOverInclude_CaseInsensitive(NonEmptyString tableName, int operationIndex)
    {
        // Property: Precedence rule also holds with different casing
        var operation = AllOperations[((operationIndex % AllOperations.Length) + AllOperations.Length) % AllOperations.Length];
        var options = new CdcMessagingOptions
        {
            IncludeTables = [tableName.Get.ToLowerInvariant()],
            ExcludeTables = [tableName.Get.ToUpperInvariant()],
        };

        return !options.ShouldPublish(tableName.Get, operation);
    }

    #endregion

    #region Combined Filters

    [Property(MaxTest = 100)]
    public bool Property_ShouldPublish_CombinedFilters_AllMustPass(NonEmptyString tableName)
    {
        // Property: When both IncludeTables and IncludeOperations are set, both must match
        var options = new CdcMessagingOptions
        {
            IncludeTables = [tableName.Get],
            IncludeOperations = [ChangeOperation.Insert],
        };

        // Correct table + correct operation = true
        var matchesBoth = options.ShouldPublish(tableName.Get, ChangeOperation.Insert);
        // Correct table + wrong operation = false
        var wrongOp = !options.ShouldPublish(tableName.Get, ChangeOperation.Delete);
        // Wrong table + correct operation = false
        var wrongTable = !options.ShouldPublish("NonExistentTable", ChangeOperation.Insert);

        return matchesBoth && wrongOp && wrongTable;
    }

    #endregion

    #region Default TopicPattern

    [Property(MaxTest = 50)]
    public bool Property_TopicPattern_DefaultValue()
    {
        // Property: Default TopicPattern is "{tableName}.{operation}"
        var options = new CdcMessagingOptions();

        return options.TopicPattern == "{tableName}.{operation}";
    }

    #endregion

    #region Property Round-Trips

    [Property(MaxTest = 100)]
    public bool Property_TopicPattern_SetThenGet_RoundTrips(NonEmptyString pattern)
    {
        // Property: Setting TopicPattern always returns what was set
        var options = new CdcMessagingOptions { TopicPattern = pattern.Get };

        return options.TopicPattern == pattern.Get;
    }

    [Property(MaxTest = 100)]
    public bool Property_IncludeTables_SetThenGet_RoundTrips(List<NonEmptyString> tables)
    {
        // Property: Setting IncludeTables always preserves the array content
        var input = (tables ?? []).Select(t => t.Get).ToArray();
        var options = new CdcMessagingOptions { IncludeTables = input };

        return options.IncludeTables.SequenceEqual(input);
    }

    [Property(MaxTest = 100)]
    public bool Property_ExcludeTables_SetThenGet_RoundTrips(List<NonEmptyString> tables)
    {
        // Property: Setting ExcludeTables always preserves the array content
        var input = (tables ?? []).Select(t => t.Get).ToArray();
        var options = new CdcMessagingOptions { ExcludeTables = input };

        return options.ExcludeTables.SequenceEqual(input);
    }

    #endregion
}
