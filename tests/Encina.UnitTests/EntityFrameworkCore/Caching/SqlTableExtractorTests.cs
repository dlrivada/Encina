using Encina.EntityFrameworkCore.Caching;

namespace Encina.UnitTests.EntityFrameworkCore.Caching;

/// <summary>
/// Unit tests for <see cref="SqlTableExtractor"/>.
/// </summary>
public class SqlTableExtractorTests
{
    #region Null and Empty Input Tests

    [Fact]
    public void ExtractTableNames_WithNull_ReturnsEmptyList()
    {
        // Act
        var result = SqlTableExtractor.ExtractTableNames(null);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ExtractTableNames_WithEmptyString_ReturnsEmptyList()
    {
        // Act
        var result = SqlTableExtractor.ExtractTableNames(string.Empty);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ExtractTableNames_WithWhitespaceOnly_ReturnsEmptyList()
    {
        // Act
        var result = SqlTableExtractor.ExtractTableNames("   ");

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region Simple SELECT Tests

    [Fact]
    public void ExtractTableNames_SimpleSelect_ExtractsTable()
    {
        // Arrange
        const string sql = "SELECT * FROM Orders";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBe("Orders");
    }

    [Fact]
    public void ExtractTableNames_SelectWithColumns_ExtractsTable()
    {
        // Arrange
        const string sql = "SELECT Id, Name, Price FROM Products WHERE Id = @Id";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBe("Products");
    }

    [Fact]
    public void ExtractTableNames_SelectWithAlias_ExtractsTable()
    {
        // Arrange
        const string sql = "SELECT o.Id, o.Total FROM Orders AS o WHERE o.Id = @Id";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBe("Orders");
    }

    #endregion

    #region Provider-Specific Quoting Tests

    [Fact]
    public void ExtractTableNames_SqlServerBracketQuoting_ExtractsTable()
    {
        // Arrange
        const string sql = "SELECT * FROM [Orders] WHERE [Id] = @Id";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBe("Orders");
    }

    [Fact]
    public void ExtractTableNames_PostgreSqlDoubleQuoting_ExtractsTable()
    {
        // Arrange
        const string sql = "SELECT * FROM \"Orders\" WHERE \"Id\" = @Id";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBe("Orders");
    }

    [Fact]
    public void ExtractTableNames_MySqlBacktickQuoting_ExtractsTable()
    {
        // Arrange
        const string sql = "SELECT * FROM `Orders` WHERE `Id` = @Id";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBe("Orders");
    }

    #endregion

    #region Schema-Qualified Table Tests

    [Fact]
    public void ExtractTableNames_SqlServerSchemaQualified_ExtractsTableNameOnly()
    {
        // Arrange
        const string sql = "SELECT * FROM [dbo].[Orders]";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBe("Orders");
    }

    [Fact]
    public void ExtractTableNames_PostgreSqlSchemaQualified_ExtractsTableNameOnly()
    {
        // Arrange
        const string sql = "SELECT * FROM \"public\".\"Orders\"";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBe("Orders");
    }

    [Fact]
    public void ExtractTableNames_MySqlSchemaQualified_ExtractsTableNameOnly()
    {
        // Arrange
        const string sql = "SELECT * FROM `mydb`.`Orders`";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBe("Orders");
    }

    [Fact]
    public void ExtractTableNames_PlainSchemaQualified_ExtractsTableNameOnly()
    {
        // Arrange
        const string sql = "SELECT * FROM dbo.Orders";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBe("Orders");
    }

    #endregion

    #region JOIN Tests

    [Fact]
    public void ExtractTableNames_InnerJoin_ExtractsBothTables()
    {
        // Arrange
        const string sql = "SELECT * FROM Orders INNER JOIN Customers ON Orders.CustomerId = Customers.Id";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.Count.ShouldBe(2);
        result[0].ShouldBe("Orders");
        result[1].ShouldBe("Customers");
    }

    [Fact]
    public void ExtractTableNames_LeftJoin_ExtractsBothTables()
    {
        // Arrange
        const string sql = "SELECT * FROM Orders LEFT JOIN OrderItems ON Orders.Id = OrderItems.OrderId";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.Count.ShouldBe(2);
        result[0].ShouldBe("Orders");
        result[1].ShouldBe("OrderItems");
    }

    [Fact]
    public void ExtractTableNames_RightOuterJoin_ExtractsBothTables()
    {
        // Arrange
        const string sql = "SELECT * FROM Orders RIGHT OUTER JOIN Customers ON Orders.CustomerId = Customers.Id";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.Count.ShouldBe(2);
        result[0].ShouldBe("Orders");
        result[1].ShouldBe("Customers");
    }

    [Fact]
    public void ExtractTableNames_CrossJoin_ExtractsBothTables()
    {
        // Arrange
        const string sql = "SELECT * FROM Products CROSS JOIN Categories";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.Count.ShouldBe(2);
        result[0].ShouldBe("Products");
        result[1].ShouldBe("Categories");
    }

    [Fact]
    public void ExtractTableNames_MultipleJoins_ExtractsAllTables()
    {
        // Arrange
        const string sql = """
            SELECT o.Id, c.Name, p.Title
            FROM Orders o
            INNER JOIN Customers c ON o.CustomerId = c.Id
            LEFT JOIN Products p ON o.ProductId = p.Id
            """;

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.Count.ShouldBe(3);
        result[0].ShouldBe("Orders");
        result[1].ShouldBe("Customers");
        result[2].ShouldBe("Products");
    }

    [Fact]
    public void ExtractTableNames_JoinWithBracketQuoting_ExtractsAllTables()
    {
        // Arrange
        const string sql = "SELECT * FROM [dbo].[Orders] INNER JOIN [dbo].[Customers] ON [Orders].[CustomerId] = [Customers].[Id]";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.Count.ShouldBe(2);
        result[0].ShouldBe("Orders");
        result[1].ShouldBe("Customers");
    }

    #endregion

    #region Deduplication Tests

    [Fact]
    public void ExtractTableNames_DuplicateFromTable_ReturnsUnique()
    {
        // Arrange — self-join scenario
        const string sql = "SELECT a.* FROM Employees a INNER JOIN Employees b ON a.ManagerId = b.Id";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBe("Employees");
    }

    [Fact]
    public void ExtractTableNames_CaseInsensitiveDedup_ReturnsUnique()
    {
        // Arrange — FROM and JOIN reference same table in different case
        const string sql = "SELECT * FROM orders INNER JOIN ORDERS ON orders.Id = ORDERS.ParentId";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.ShouldHaveSingleItem();
    }

    #endregion

    #region SQL Keyword Filtering Tests

    [Theory]
    [InlineData("SELECT")]
    [InlineData("FROM")]
    [InlineData("WHERE")]
    [InlineData("JOIN")]
    [InlineData("ON")]
    [InlineData("AND")]
    [InlineData("OR")]
    [InlineData("AS")]
    [InlineData("IN")]
    [InlineData("NOT")]
    [InlineData("NULL")]
    [InlineData("INNER")]
    [InlineData("LEFT")]
    [InlineData("RIGHT")]
    [InlineData("OUTER")]
    [InlineData("CROSS")]
    [InlineData("FULL")]
    [InlineData("GROUP")]
    [InlineData("ORDER")]
    [InlineData("BY")]
    [InlineData("HAVING")]
    [InlineData("UNION")]
    [InlineData("ALL")]
    [InlineData("DISTINCT")]
    [InlineData("TOP")]
    [InlineData("LIMIT")]
    [InlineData("OFFSET")]
    [InlineData("LATERAL")]
    public void ExtractTableNames_SqlKeywordAsTableName_IsFilteredOut(string keyword)
    {
        // This test verifies keywords are not returned when they would
        // appear as false positive table names from regex matching.
        // We verify the keyword is in the filter list by checking a
        // crafted SQL that might produce false positives.
        var sql = $"SELECT * FROM RealTable WHERE Id IN (SELECT Id FROM {keyword})";

        var result = SqlTableExtractor.ExtractTableNames(sql);

        // RealTable should always be found; the keyword may or may not match
        // depending on regex behavior but if it matches, it should be filtered
        result.ShouldContain("RealTable");
        result.ShouldNotContain(keyword);
    }

    #endregion

    #region Ordering Tests

    [Fact]
    public void ExtractTableNames_FromTablesComesFirst()
    {
        // Arrange — FROM table should be first (primary entity)
        const string sql = "SELECT * FROM Orders INNER JOIN Customers ON Orders.CustomerId = Customers.Id";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result[0].ShouldBe("Orders");
    }

    #endregion

    #region Complex SQL Tests

    [Fact]
    public void ExtractTableNames_SubqueryIsNotParsedAsSeparateFrom()
    {
        // Arrange — subquery has a separate FROM clause
        const string sql = """
            SELECT * FROM Orders
            WHERE CustomerId IN (SELECT Id FROM Customers WHERE Country = 'US')
            """;

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain("Orders");
        result.ShouldContain("Customers");
    }

    [Fact]
    public void ExtractTableNames_UnionQuery_ExtractsAllTables()
    {
        // Arrange
        const string sql = """
            SELECT Id, Name FROM Products
            UNION ALL
            SELECT Id, Name FROM ArchivedProducts
            """;

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain("Products");
        result.ShouldContain("ArchivedProducts");
    }

    [Fact]
    public void ExtractTableNames_NoTableReference_ReturnsEmpty()
    {
        // Arrange — a SQL statement with no FROM clause
        const string sql = "SELECT 1";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ExtractTableNames_FullOuterJoin_ExtractsBothTables()
    {
        // Arrange
        const string sql = "SELECT * FROM LeftTable FULL OUTER JOIN RightTable ON LeftTable.Id = RightTable.LeftId";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.Count.ShouldBe(2);
        result[0].ShouldBe("LeftTable");
        result[1].ShouldBe("RightTable");
    }

    [Fact]
    public void ExtractTableNames_LeftOuterJoin_ExtractsBothTables()
    {
        // Arrange
        const string sql = "SELECT * FROM Orders LEFT OUTER JOIN Returns ON Orders.Id = Returns.OrderId";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.Count.ShouldBe(2);
        result[0].ShouldBe("Orders");
        result[1].ShouldBe("Returns");
    }

    #endregion

    #region Case Sensitivity Tests

    [Fact]
    public void ExtractTableNames_LowercaseKeywords_StillExtractsTables()
    {
        // Arrange
        const string sql = "select * from Orders inner join Customers on Orders.CustomerId = Customers.Id";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain("Orders");
        result.ShouldContain("Customers");
    }

    [Fact]
    public void ExtractTableNames_MixedCaseKeywords_StillExtractsTables()
    {
        // Arrange
        const string sql = "Select * From Products Left Join Categories On Products.CategoryId = Categories.Id";

        // Act
        var result = SqlTableExtractor.ExtractTableNames(sql);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain("Products");
        result.ShouldContain("Categories");
    }

    #endregion
}
