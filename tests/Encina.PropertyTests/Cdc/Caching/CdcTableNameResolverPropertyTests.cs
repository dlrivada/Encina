using Encina.Cdc.Caching;
using FsCheck;
using FsCheck.Xunit;
using Shouldly;

namespace Encina.PropertyTests.Cdc.Caching;

/// <summary>
/// Property-based tests for <see cref="CdcTableNameResolver"/> invariants.
/// Verifies schema stripping consistency, explicit mapping precedence, and
/// case-insensitive matching behavior.
/// </summary>
[Trait("Category", "Property")]
public sealed class CdcTableNameResolverPropertyTests
{
    #region Schema Stripping

    /// <summary>
    /// Property: Schema stripping always produces a non-empty result for valid table names.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_ResolveEntityType_AlwaysReturnsNonEmpty_ForNonEmptyTableName(NonEmptyString tableName)
    {
        var result = CdcTableNameResolver.ResolveEntityType(tableName.Get, null);
        return !string.IsNullOrEmpty(result);
    }

    /// <summary>
    /// Property: Schema stripping produces the same result for the same input (deterministic).
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_ResolveEntityType_IsDeterministic(NonEmptyString tableName)
    {
        var result1 = CdcTableNameResolver.ResolveEntityType(tableName.Get, null);
        var result2 = CdcTableNameResolver.ResolveEntityType(tableName.Get, null);
        return result1 == result2;
    }

    /// <summary>
    /// Property: Table names without dots are returned unchanged.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_ResolveEntityType_WithoutDot_ReturnsOriginal(NonEmptyString tableName)
    {
        var name = tableName.Get.Replace(".", "");
        if (string.IsNullOrEmpty(name)) return true; // Skip degenerate case

        var result = CdcTableNameResolver.ResolveEntityType(name, null);
        return result == name;
    }

    /// <summary>
    /// Property: Schema-qualified names always produce a result shorter than or equal to the input.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_ResolveEntityType_ResultLengthNeverExceedsInput(NonEmptyString tableName)
    {
        var result = CdcTableNameResolver.ResolveEntityType(tableName.Get, null);
        return result.Length <= tableName.Get.Length;
    }

    #endregion

    #region Explicit Mapping Precedence

    /// <summary>
    /// Property: Explicit mappings always take precedence over default schema-stripping resolution.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_ResolveEntityType_ExplicitMapping_AlwaysTakesPrecedence(
        NonEmptyString tableName, NonEmptyString entityType)
    {
        var mappings = new Dictionary<string, string>
        {
            [tableName.Get] = entityType.Get
        };

        var result = CdcTableNameResolver.ResolveEntityType(tableName.Get, mappings);
        return result == entityType.Get;
    }

    /// <summary>
    /// Property: When a mapping exists, the schema-stripped result is never used.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_ResolveEntityType_MappedTable_IgnoresSchemaStripping(
        NonEmptyString schema, NonEmptyString table, NonEmptyString entityType)
    {
        var qualifiedName = $"{schema.Get}.{table.Get}";
        var mappings = new Dictionary<string, string>
        {
            [qualifiedName] = entityType.Get
        };

        var result = CdcTableNameResolver.ResolveEntityType(qualifiedName, mappings);
        return result == entityType.Get;
    }

    /// <summary>
    /// Property: Unmapped tables fall back to schema-stripping even when other mappings exist.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_ResolveEntityType_UnmappedTable_UsesSchemaStripping()
    {
        var mappings = new Dictionary<string, string>
        {
            ["dbo.Orders"] = "Order"
        };

        var result = CdcTableNameResolver.ResolveEntityType("dbo.Products", mappings);
        return result == "Products";
    }

    #endregion

    #region Case-Insensitive Matching

    /// <summary>
    /// Property: Mapping lookup is case-insensitive.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_ResolveEntityType_CaseInsensitiveMapping(NonEmptyString entityType)
    {
        var mappings = new Dictionary<string, string>
        {
            ["dbo.Orders"] = entityType.Get
        };

        var lowerResult = CdcTableNameResolver.ResolveEntityType("dbo.orders", mappings);
        var upperResult = CdcTableNameResolver.ResolveEntityType("DBO.ORDERS", mappings);
        var mixedResult = CdcTableNameResolver.ResolveEntityType("dbo.Orders", mappings);

        return lowerResult == entityType.Get
            && upperResult == entityType.Get
            && mixedResult == entityType.Get;
    }

    #endregion

    #region Schema Stripping Specific Cases

    /// <summary>
    /// Verifies well-known schema patterns resolve correctly.
    /// </summary>
    [Theory]
    [InlineData("dbo.Orders", "Orders")]
    [InlineData("public.products", "products")]
    [InlineData("schema.Table", "Table")]
    [InlineData("a.b.c", "c")]
    [InlineData("Orders", "Orders")]
    public void ResolveEntityType_WellKnownPatterns_ResolvesCorrectly(
        string tableName, string expected)
    {
        var result = CdcTableNameResolver.ResolveEntityType(tableName, null);
        result.ShouldBe(expected);
    }

    #endregion
}
