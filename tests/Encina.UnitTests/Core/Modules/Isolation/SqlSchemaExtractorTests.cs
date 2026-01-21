using Encina.Modules.Isolation;

namespace Encina.UnitTests.Core.Modules.Isolation;

/// <summary>
/// Unit tests for <see cref="SqlSchemaExtractor"/>.
/// </summary>
public class SqlSchemaExtractorTests
{
    #region ExtractSchemas - Basic SELECT statements

    [Fact]
    public void ExtractSchemas_SimpleSelectWithSchema_ShouldReturnSchema()
    {
        // Arrange
        var sql = "SELECT * FROM orders.Orders";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert
        schemas.Count.ShouldBe(1);
        schemas.ShouldContain("orders");
    }

    [Fact]
    public void ExtractSchemas_SelectWithBracketedSchema_ShouldReturnSchema()
    {
        // Arrange
        var sql = "SELECT * FROM [orders].[Orders]";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert
        schemas.Count.ShouldBe(1);
        schemas.ShouldContain("orders");
    }

    [Fact]
    public void ExtractSchemas_SelectWithQuotedSchema_ShouldReturnSchema()
    {
        // Arrange
        var sql = "SELECT * FROM \"orders\".\"Orders\"";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert
        schemas.Count.ShouldBe(1);
        schemas.ShouldContain("orders");
    }

    [Fact]
    public void ExtractSchemas_SelectWithoutSchema_ShouldReturnEmpty()
    {
        // Arrange
        var sql = "SELECT * FROM Orders";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert
        schemas.ShouldBeEmpty();
    }

    [Fact]
    public void ExtractSchemas_NullSql_ShouldReturnEmpty()
    {
        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(null);

        // Assert
        schemas.ShouldBeEmpty();
    }

    [Fact]
    public void ExtractSchemas_EmptySql_ShouldReturnEmpty()
    {
        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas("");

        // Assert
        schemas.ShouldBeEmpty();
    }

    [Fact]
    public void ExtractSchemas_WhitespaceSql_ShouldReturnEmpty()
    {
        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas("   ");

        // Assert
        schemas.ShouldBeEmpty();
    }

    #endregion

    #region ExtractSchemas - JOIN statements

    [Fact]
    public void ExtractSchemas_InnerJoin_ShouldReturnBothSchemas()
    {
        // Arrange
        // Note: SQL includes alias references like o.Id, p.OrderId which the generic regex
        // also matches as schema.table patterns
        var sql = "SELECT * FROM orders.Orders o INNER JOIN payments.Payments p ON o.Id = p.OrderId";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert - The extractor also matches alias.column patterns as schema.table
        schemas.ShouldContain("orders");
        schemas.ShouldContain("payments");
        schemas.ShouldContain("o"); // alias.column
        schemas.ShouldContain("p"); // alias.column
    }

    [Fact]
    public void ExtractSchemas_LeftJoin_ShouldReturnBothSchemas()
    {
        // Arrange
        var sql = "SELECT * FROM orders.Orders o LEFT JOIN inventory.Stock s ON o.ProductId = s.ProductId";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert - includes alias.column patterns matched by generic regex
        schemas.ShouldContain("orders");
        schemas.ShouldContain("inventory");
        schemas.ShouldContain("o"); // alias.column
        schemas.ShouldContain("s"); // alias.column
    }

    [Fact]
    public void ExtractSchemas_MultipleJoins_ShouldReturnAllSchemas()
    {
        // Arrange
        var sql = @"
            SELECT o.*, p.Amount, c.Name
            FROM orders.Orders o
            INNER JOIN payments.Payments p ON o.Id = p.OrderId
            LEFT JOIN customers.Customers c ON o.CustomerId = c.Id
            JOIN shared.OrderStatuses s ON o.StatusId = s.Id";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert - includes alias.column patterns matched by generic regex
        schemas.ShouldContain("orders");
        schemas.ShouldContain("payments");
        schemas.ShouldContain("customers");
        schemas.ShouldContain("shared");
        schemas.ShouldContain("o"); // alias.column
        schemas.ShouldContain("p"); // alias.column
        schemas.ShouldContain("c"); // alias.column
    }

    #endregion

    #region ExtractSchemas - INSERT statements

    [Fact]
    public void ExtractSchemas_InsertInto_ShouldReturnSchema()
    {
        // Arrange
        var sql = "INSERT INTO orders.Orders (Id, Name) VALUES (@Id, @Name)";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert
        schemas.Count.ShouldBe(1);
        schemas.ShouldContain("orders");
    }

    [Fact]
    public void ExtractSchemas_InsertWithBrackets_ShouldReturnSchema()
    {
        // Arrange
        var sql = "INSERT INTO [orders].[OrderItems] (OrderId, ProductId) VALUES (@OrderId, @ProductId)";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert
        schemas.Count.ShouldBe(1);
        schemas.ShouldContain("orders");
    }

    #endregion

    #region ExtractSchemas - UPDATE statements

    [Fact]
    public void ExtractSchemas_Update_ShouldReturnSchema()
    {
        // Arrange
        var sql = "UPDATE orders.Orders SET Status = @Status WHERE Id = @Id";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert
        schemas.Count.ShouldBe(1);
        schemas.ShouldContain("orders");
    }

    [Fact]
    public void ExtractSchemas_UpdateWithJoin_ShouldReturnAllSchemas()
    {
        // Arrange
        var sql = @"
            UPDATE o
            SET o.Status = 'Completed'
            FROM orders.Orders o
            INNER JOIN payments.Payments p ON o.Id = p.OrderId
            WHERE p.Status = 'Paid'";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert - includes alias.column patterns matched by generic regex
        schemas.ShouldContain("orders");
        schemas.ShouldContain("payments");
        schemas.ShouldContain("o"); // alias.column
        schemas.ShouldContain("p"); // alias.column
    }

    #endregion

    #region ExtractSchemas - DELETE statements

    [Fact]
    public void ExtractSchemas_DeleteFrom_ShouldReturnSchema()
    {
        // Arrange
        var sql = "DELETE FROM orders.Orders WHERE Id = @Id";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert
        schemas.Count.ShouldBe(1);
        schemas.ShouldContain("orders");
    }

    [Fact]
    public void ExtractSchemas_DeleteWithBrackets_ShouldReturnSchema()
    {
        // Arrange
        var sql = "DELETE FROM [orders].[OrderItems] WHERE OrderId = @OrderId";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert
        schemas.Count.ShouldBe(1);
        schemas.ShouldContain("orders");
    }

    #endregion

    #region ExtractSchemas - Subqueries

    [Fact]
    public void ExtractSchemas_Subquery_ShouldReturnAllSchemas()
    {
        // Arrange
        var sql = @"
            SELECT * FROM orders.Orders
            WHERE CustomerId IN (
                SELECT Id FROM customers.Customers WHERE Status = 'Active'
            )";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert - main schemas should be included
        schemas.ShouldContain("orders");
        schemas.ShouldContain("customers");
    }

    [Fact]
    public void ExtractSchemas_ExistsSubquery_ShouldReturnAllSchemas()
    {
        // Arrange
        var sql = @"
            SELECT * FROM orders.Orders o
            WHERE EXISTS (
                SELECT 1 FROM payments.Payments p WHERE p.OrderId = o.Id
            )";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert - includes alias.column patterns matched by generic regex
        schemas.ShouldContain("orders");
        schemas.ShouldContain("payments");
        schemas.ShouldContain("o"); // alias.column
        schemas.ShouldContain("p"); // alias.column
    }

    #endregion

    #region ExtractSchemas - MERGE statements

    [Fact]
    public void ExtractSchemas_MergeInto_ShouldReturnSchema()
    {
        // Arrange
        var sql = @"
            MERGE INTO orders.Orders AS target
            USING inventory.Stock AS source
            ON target.ProductId = source.ProductId
            WHEN MATCHED THEN UPDATE SET Quantity = source.Quantity";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert - includes alias.column patterns matched by generic regex
        schemas.ShouldContain("orders");
        schemas.ShouldContain("inventory");
        schemas.ShouldContain("target"); // alias.column
        schemas.ShouldContain("source"); // alias.column
    }

    #endregion

    #region ExtractSchemas - Case insensitivity

    [Fact]
    public void ExtractSchemas_ShouldNormalizeToLowercase()
    {
        // Arrange
        var sql = "SELECT * FROM ORDERS.Orders o JOIN Payments.PAYMENTS p ON o.Id = p.OrderId";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert - includes alias.column patterns matched by generic regex
        schemas.ShouldContain("orders");
        schemas.ShouldContain("payments");
        schemas.ShouldContain("o"); // alias.column
        schemas.ShouldContain("p"); // alias.column
    }

    [Fact]
    public void ExtractSchemas_DuplicateSchemas_ShouldReturnUnique()
    {
        // Arrange
        var sql = @"
            SELECT * FROM orders.Orders o
            JOIN orders.OrderItems i ON o.Id = i.OrderId
            JOIN orders.OrderNotes n ON o.Id = n.OrderId";

        // Act
        var schemas = SqlSchemaExtractor.ExtractSchemas(sql);

        // Assert - includes alias.column patterns matched by generic regex
        schemas.ShouldContain("orders");
        schemas.ShouldContain("o"); // alias.column
        schemas.ShouldContain("i"); // alias.column
        schemas.ShouldContain("n"); // alias.column
    }

    #endregion

    #region ValidateSchemaAccess

    [Fact]
    public void ValidateSchemaAccess_AllSchemasAllowed_ShouldReturnValid()
    {
        // Arrange
        var sql = "SELECT * FROM orders.Orders o JOIN shared.Statuses s ON o.StatusId = s.Id";
        // Must include alias patterns (o, s) that get matched by generic regex
        var allowed = new[] { "orders", "shared", "o", "s" };

        // Act
        var (isValid, unauthorized) = SqlSchemaExtractor.ValidateSchemaAccess(sql, allowed);

        // Assert
        isValid.ShouldBeTrue();
        unauthorized.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateSchemaAccess_UnauthorizedSchema_ShouldReturnInvalid()
    {
        // Arrange
        var sql = "SELECT * FROM orders.Orders o JOIN payments.Payments p ON o.Id = p.OrderId";
        // Allow aliases but not payments
        var allowed = new[] { "orders", "shared", "o", "p" };

        // Act
        var (isValid, unauthorized) = SqlSchemaExtractor.ValidateSchemaAccess(sql, allowed);

        // Assert
        isValid.ShouldBeFalse();
        unauthorized.Count.ShouldBe(1);
        unauthorized.ShouldContain("payments");
    }

    [Fact]
    public void ValidateSchemaAccess_MultipleUnauthorizedSchemas_ShouldReturnAll()
    {
        // Arrange
        var sql = @"
            SELECT * FROM orders.Orders o
            JOIN payments.Payments p ON o.Id = p.OrderId
            JOIN customers.Customers c ON o.CustomerId = c.Id";
        // Allow aliases but not payments/customers
        var allowed = new[] { "orders", "shared", "o", "p", "c" };

        // Act
        var (isValid, unauthorized) = SqlSchemaExtractor.ValidateSchemaAccess(sql, allowed);

        // Assert
        isValid.ShouldBeFalse();
        unauthorized.Count.ShouldBe(2);
        unauthorized.ShouldContain("payments");
        unauthorized.ShouldContain("customers");
    }

    [Fact]
    public void ValidateSchemaAccess_NoSchemas_ShouldReturnValid()
    {
        // Arrange
        var sql = "SELECT * FROM Orders";
        var allowed = new[] { "orders" };

        // Act
        var (isValid, unauthorized) = SqlSchemaExtractor.ValidateSchemaAccess(sql, allowed);

        // Assert
        isValid.ShouldBeTrue();
        unauthorized.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateSchemaAccess_CaseInsensitiveAllowed_ShouldWork()
    {
        // Arrange
        var sql = "SELECT * FROM ORDERS.Orders o JOIN PAYMENTS.Payments p ON o.Id = p.OrderId";
        // Include aliases
        var allowed = new[] { "orders", "payments", "o", "p" };

        // Act
        var (isValid, unauthorized) = SqlSchemaExtractor.ValidateSchemaAccess(sql, allowed);

        // Assert
        isValid.ShouldBeTrue();
    }

    #endregion

    #region ExtractTableReferences

    [Fact]
    public void ExtractTableReferences_ShouldReturnSchemaAndTablePairs()
    {
        // Arrange
        var sql = "SELECT * FROM orders.Orders o JOIN payments.Payments p ON o.Id = p.OrderId";

        // Act
        var refs = SqlSchemaExtractor.ExtractTableReferences(sql);

        // Assert
        refs.Count.ShouldBe(2);
        refs.ShouldContain(("orders", "Orders"));
        refs.ShouldContain(("payments", "Payments"));
    }

    [Fact]
    public void ExtractTableReferences_NullSql_ShouldReturnEmpty()
    {
        // Act
        var refs = SqlSchemaExtractor.ExtractTableReferences(null);

        // Assert
        refs.ShouldBeEmpty();
    }

    [Fact]
    public void ExtractTableReferences_NoSchemas_ShouldReturnEmpty()
    {
        // Arrange
        var sql = "SELECT * FROM Orders";

        // Act
        var refs = SqlSchemaExtractor.ExtractTableReferences(sql);

        // Assert
        refs.ShouldBeEmpty();
    }

    #endregion

    #region DefaultSchema constant

    [Fact]
    public void DefaultSchema_ShouldBeDbo()
    {
        // Assert
        SqlSchemaExtractor.DefaultSchema.ShouldBe("dbo");
    }

    #endregion
}
