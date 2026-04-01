using Encina.Modules.Isolation;

namespace Encina.GuardTests.Core.Modules;

/// <summary>
/// Guard clause tests for <see cref="SqlSchemaExtractor"/>.
/// Verifies null/empty SQL handling, schema extraction accuracy, and validation edge cases.
/// </summary>
public sealed class SqlSchemaExtractorGuardTests
{
    private static readonly string[] OrdersSchemaAllowed = ["orders"];
    private static readonly string[] OrdersSharedAllowed = ["orders", "shared"];
    #region ExtractSchemas Guards

    /// <summary>
    /// Verifies that ExtractSchemas returns empty set for null SQL.
    /// </summary>
    [Fact]
    public void ExtractSchemas_NullSql_ReturnsEmptySet()
    {
        var result = SqlSchemaExtractor.ExtractSchemas(null);

        result.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that ExtractSchemas returns empty set for empty string.
    /// </summary>
    [Fact]
    public void ExtractSchemas_EmptyString_ReturnsEmptySet()
    {
        var result = SqlSchemaExtractor.ExtractSchemas(string.Empty);

        result.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that ExtractSchemas returns empty set for whitespace.
    /// </summary>
    [Fact]
    public void ExtractSchemas_Whitespace_ReturnsEmptySet()
    {
        var result = SqlSchemaExtractor.ExtractSchemas("   ");

        result.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that ExtractSchemas returns empty for SQL without schema-qualified references.
    /// </summary>
    [Fact]
    public void ExtractSchemas_NoSchemaQualified_ReturnsEmptySet()
    {
        var result = SqlSchemaExtractor.ExtractSchemas("SELECT * FROM Orders");

        result.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that ExtractSchemas extracts schemas from standard FROM clause.
    /// </summary>
    [Fact]
    public void ExtractSchemas_FromClause_ExtractsSchemas()
    {
        var result = SqlSchemaExtractor.ExtractSchemas("SELECT * FROM orders.Orders");

        result.ShouldContain("orders");
    }

    /// <summary>
    /// Verifies that ExtractSchemas extracts schemas from JOIN clause.
    /// </summary>
    [Fact]
    public void ExtractSchemas_JoinClause_ExtractsSchemas()
    {
        var result = SqlSchemaExtractor.ExtractSchemas(
            "SELECT * FROM orders.Orders o JOIN payments.Payments p ON o.Id = p.OrderId");

        result.ShouldContain("orders");
        result.ShouldContain("payments");
    }

    /// <summary>
    /// Verifies that ExtractSchemas handles bracketed identifiers.
    /// </summary>
    [Fact]
    public void ExtractSchemas_BracketedIdentifiers_ExtractsSchemas()
    {
        var result = SqlSchemaExtractor.ExtractSchemas("SELECT * FROM [orders].[Orders]");

        result.ShouldContain("orders");
    }

    /// <summary>
    /// Verifies that ExtractSchemas handles quoted identifiers.
    /// </summary>
    [Fact]
    public void ExtractSchemas_QuotedIdentifiers_ExtractsSchemas()
    {
        var result = SqlSchemaExtractor.ExtractSchemas("SELECT * FROM \"orders\".\"Orders\"");

        result.ShouldContain("orders");
    }

    /// <summary>
    /// Verifies that ExtractSchemas extracts from DELETE FROM statements.
    /// </summary>
    [Fact]
    public void ExtractSchemas_DeleteFrom_ExtractsSchemas()
    {
        var result = SqlSchemaExtractor.ExtractSchemas("DELETE FROM orders.Orders WHERE Id = 1");

        result.ShouldContain("orders");
    }

    /// <summary>
    /// Verifies that ExtractSchemas extracts from UPDATE statements.
    /// </summary>
    [Fact]
    public void ExtractSchemas_Update_ExtractsSchemas()
    {
        var result = SqlSchemaExtractor.ExtractSchemas("UPDATE orders.Orders SET Status = 1");

        result.ShouldContain("orders");
    }

    /// <summary>
    /// Verifies that ExtractSchemas extracts from INSERT INTO statements.
    /// </summary>
    [Fact]
    public void ExtractSchemas_InsertInto_ExtractsSchemas()
    {
        var result = SqlSchemaExtractor.ExtractSchemas("INSERT INTO orders.Orders (Id) VALUES (1)");

        result.ShouldContain("orders");
    }

    /// <summary>
    /// Verifies that ExtractSchemas extracts from MERGE INTO statements.
    /// </summary>
    [Fact]
    public void ExtractSchemas_MergeInto_ExtractsSchemas()
    {
        var result = SqlSchemaExtractor.ExtractSchemas(
            "MERGE INTO orders.Orders AS target USING staging.Orders AS source ON target.Id = source.Id");

        result.ShouldContain("orders");
    }

    /// <summary>
    /// Verifies that ExtractSchemas normalizes schemas to lowercase.
    /// </summary>
    [Fact]
    public void ExtractSchemas_MixedCase_NormalizesToLowercase()
    {
        var result = SqlSchemaExtractor.ExtractSchemas("SELECT * FROM ORDERS.Orders");

        result.ShouldContain("orders");
    }

    /// <summary>
    /// Verifies that ExtractSchemas deduplicates multiple references to the same schema.
    /// </summary>
    [Fact]
    public void ExtractSchemas_DuplicateSchemas_Deduplicates()
    {
        var result = SqlSchemaExtractor.ExtractSchemas(
            "SELECT * FROM orders.Orders o JOIN orders.OrderItems i ON o.Id = i.OrderId");

        result.Count(s => string.Equals(s, "orders", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
    }

    /// <summary>
    /// Verifies that ExtractSchemas filters out sys reserved keyword.
    /// </summary>
    [Fact]
    public void ExtractSchemas_SysSchema_IsFilteredOut()
    {
        var result = SqlSchemaExtractor.ExtractSchemas("SELECT * FROM sys.tables");

        result.ShouldNotContain("sys");
    }

    /// <summary>
    /// Verifies that ExtractSchemas extracts SQLite prefix-style table names.
    /// </summary>
    [Fact]
    public void ExtractSchemas_SqlitePrefixStyle_ExtractsPrefix()
    {
        var result = SqlSchemaExtractor.ExtractSchemas("SELECT * FROM orders_Orders");

        result.ShouldContain("orders");
    }

    /// <summary>
    /// Verifies the DefaultSchema constant.
    /// </summary>
    [Fact]
    public void DefaultSchema_IsDbo()
    {
        SqlSchemaExtractor.DefaultSchema.ShouldBe("dbo");
    }

    #endregion

    #region ValidateSchemaAccess Guards

    /// <summary>
    /// Verifies that ValidateSchemaAccess returns valid for null SQL.
    /// </summary>
    [Fact]
    public void ValidateSchemaAccess_NullSql_ReturnsValid()
    {
        var (isValid, unauthorized) = SqlSchemaExtractor.ValidateSchemaAccess(null, OrdersSchemaAllowed);

        isValid.ShouldBeTrue();
        unauthorized.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that ValidateSchemaAccess returns valid when all schemas are allowed.
    /// </summary>
    [Fact]
    public void ValidateSchemaAccess_AllAllowed_ReturnsValid()
    {
        var (isValid, unauthorized) = SqlSchemaExtractor.ValidateSchemaAccess(
            "SELECT * FROM orders.Orders", OrdersSchemaAllowed);

        isValid.ShouldBeTrue();
        unauthorized.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that ValidateSchemaAccess returns invalid when accessing unauthorized schema.
    /// </summary>
    [Fact]
    public void ValidateSchemaAccess_UnauthorizedSchema_ReturnsInvalid()
    {
        var (isValid, unauthorized) = SqlSchemaExtractor.ValidateSchemaAccess(
            "SELECT * FROM orders.Orders JOIN payments.Payments ON 1=1",
            OrdersSchemaAllowed);

        isValid.ShouldBeFalse();
        unauthorized.ShouldContain("payments");
    }

    /// <summary>
    /// Verifies that ValidateSchemaAccess is case-insensitive for allowed schemas.
    /// </summary>
    [Fact]
    public void ValidateSchemaAccess_CaseInsensitiveAllowed_ReturnsValid()
    {
        var (isValid, _) = SqlSchemaExtractor.ValidateSchemaAccess(
            "SELECT * FROM ORDERS.Orders", OrdersSchemaAllowed);

        isValid.ShouldBeTrue();
    }

    #endregion

    #region ExtractTableReferences Guards

    /// <summary>
    /// Verifies that ExtractTableReferences returns empty list for null SQL.
    /// </summary>
    [Fact]
    public void ExtractTableReferences_NullSql_ReturnsEmptyList()
    {
        var result = SqlSchemaExtractor.ExtractTableReferences(null);

        result.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that ExtractTableReferences returns empty list for empty SQL.
    /// </summary>
    [Fact]
    public void ExtractTableReferences_EmptyString_ReturnsEmptyList()
    {
        var result = SqlSchemaExtractor.ExtractTableReferences(string.Empty);

        result.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that ExtractTableReferences returns empty list for whitespace SQL.
    /// </summary>
    [Fact]
    public void ExtractTableReferences_Whitespace_ReturnsEmptyList()
    {
        var result = SqlSchemaExtractor.ExtractTableReferences("   ");

        result.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that ExtractTableReferences returns correct schema-table pairs.
    /// </summary>
    [Fact]
    public void ExtractTableReferences_ValidSql_ReturnsPairs()
    {
        var result = SqlSchemaExtractor.ExtractTableReferences(
            "SELECT * FROM orders.Orders o JOIN payments.Payments p ON o.Id = p.OrderId");

        result.Count.ShouldBeGreaterThanOrEqualTo(2);
        result.ShouldContain(r => r.Schema == "orders");
        result.ShouldContain(r => r.Schema == "payments");
    }

    /// <summary>
    /// Verifies that ExtractTableReferences deduplicates identical references.
    /// </summary>
    [Fact]
    public void ExtractTableReferences_DuplicateReferences_Deduplicates()
    {
        var result = SqlSchemaExtractor.ExtractTableReferences(
            "SELECT * FROM orders.Orders UNION SELECT * FROM orders.Orders");

        result.Count(r => r.Schema == "orders" && r.Table == "Orders").ShouldBe(1);
    }

    /// <summary>
    /// Verifies that ExtractTableReferences handles multiple SQL statement types.
    /// </summary>
    [Fact]
    public void ExtractTableReferences_MultipleStatementTypes_ExtractsAll()
    {
        var sql = """
            SELECT * FROM orders.Orders;
            DELETE FROM orders.OrderItems WHERE 1=1;
            """;

        var result = SqlSchemaExtractor.ExtractTableReferences(sql);

        result.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    #endregion
}
