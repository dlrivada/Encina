using System.Data;
using System.Text.Json;
using Encina.EntityFrameworkCore.Caching;

namespace Encina.UnitTests.EntityFrameworkCore.Caching;

/// <summary>
/// Unit tests for <see cref="CachedDataReader"/>.
/// </summary>
public class CachedDataReaderTests : IDisposable
{
    private readonly CachedQueryResult _defaultResult;
    private CachedDataReader _sut;

    public CachedDataReaderTests()
    {
        _defaultResult = CreateTestResult();
        _sut = new CachedDataReader(_defaultResult);
    }

    public void Dispose()
    {
        _sut.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullResult_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CachedDataReader(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("result");
    }

    [Fact]
    public void Constructor_WithValidResult_InitializesCorrectly()
    {
        // Assert
        _sut.FieldCount.ShouldBe(3);
        _sut.HasRows.ShouldBeTrue();
        _sut.IsClosed.ShouldBeFalse();
        _sut.Depth.ShouldBe(0);
        _sut.RecordsAffected.ShouldBe(-1);
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public void Read_FirstCall_ReturnsTrue()
    {
        // Act
        var result = _sut.Read();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Read_PastLastRow_ReturnsFalse()
    {
        // Arrange — read past all rows
        _sut.Read(); // row 0
        _sut.Read(); // row 1

        // Act
        var result = _sut.Read();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ReadAsync_FirstCall_ReturnsTrue()
    {
        // Act
        var result = await _sut.ReadAsync(CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await _sut.ReadAsync(cts.Token));
    }

    [Fact]
    public void NextResult_AlwaysReturnsFalse()
    {
        // Act
        var result = _sut.NextResult();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task NextResultAsync_AlwaysReturnsFalse()
    {
        // Act
        var result = await _sut.NextResultAsync(CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void FieldCount_ReturnsColumnCount()
    {
        // Assert
        _sut.FieldCount.ShouldBe(3);
    }

    [Fact]
    public void HasRows_WithRows_ReturnsTrue()
    {
        // Assert
        _sut.HasRows.ShouldBeTrue();
    }

    [Fact]
    public void HasRows_WithEmptyResult_ReturnsFalse()
    {
        // Arrange
        var emptyResult = new CachedQueryResult
        {
            Columns = CreateTestColumns(),
            Rows = [],
            CachedAtUtc = DateTime.UtcNow
        };

        using var reader = new CachedDataReader(emptyResult);

        // Assert
        reader.HasRows.ShouldBeFalse();
    }

    [Fact]
    public void IsClosed_BeforeClose_ReturnsFalse()
    {
        // Assert
        _sut.IsClosed.ShouldBeFalse();
    }

    [Fact]
    public void IsClosed_AfterClose_ReturnsTrue()
    {
        // Act
        _sut.Close();

        // Assert
        _sut.IsClosed.ShouldBeTrue();
    }

    [Fact]
    public void FieldCount_AfterClose_ReturnsZero()
    {
        // Act
        _sut.Close();

        // Assert
        _sut.FieldCount.ShouldBe(0);
    }

    #endregion

    #region GetValue Tests

    [Fact]
    public void GetValue_BeforeRead_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _sut.GetValue(0));
    }

    [Fact]
    public void GetValue_AfterRead_ReturnsCorrectValue()
    {
        // Arrange
        _sut.Read();

        // Act
        var value = _sut.GetValue(0);

        // Assert
        value.ShouldBe(1);
    }

    [Fact]
    public void GetValue_NullValue_ReturnsDBNull()
    {
        // Arrange
        _sut.Read();

        // Act — column 2 (Price) in row 1 is null
        _sut.Read(); // move to row 1
        var value = _sut.GetValue(2);

        // Assert
        value.ShouldBe(DBNull.Value);
    }

    [Fact]
    public void GetValue_InvalidOrdinal_ThrowsIndexOutOfRange()
    {
        // Arrange
        _sut.Read();

        // Act & Assert
        Should.Throw<IndexOutOfRangeException>(() => _sut.GetValue(99));
    }

    [Fact]
    public void GetValue_NegativeOrdinal_ThrowsIndexOutOfRange()
    {
        // Arrange
        _sut.Read();

        // Act & Assert
        Should.Throw<IndexOutOfRangeException>(() => _sut.GetValue(-1));
    }

    [Fact]
    public void GetValue_OnClosedReader_ThrowsInvalidOperationException()
    {
        // Arrange
        _sut.Read();
        _sut.Close();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _sut.GetValue(0));
    }

    #endregion

    #region GetValues Tests

    [Fact]
    public void GetValues_WithNullArray_ThrowsArgumentNullException()
    {
        // Arrange
        _sut.Read();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => _sut.GetValues(null!));
        ex.ParamName.ShouldBe("values");
    }

    [Fact]
    public void GetValues_ReturnsAllValues()
    {
        // Arrange
        _sut.Read();
        var values = new object[3];

        // Act
        var count = _sut.GetValues(values);

        // Assert
        count.ShouldBe(3);
        values[0].ShouldBe(1);
        values[1].ShouldBe("Widget");
    }

    [Fact]
    public void GetValues_SmallerArray_ReturnsTruncated()
    {
        // Arrange
        _sut.Read();
        var values = new object[1];

        // Act
        var count = _sut.GetValues(values);

        // Assert
        count.ShouldBe(1);
        values[0].ShouldBe(1);
    }

    #endregion

    #region IsDBNull Tests

    [Fact]
    public void IsDBNull_NonNullValue_ReturnsFalse()
    {
        // Arrange
        _sut.Read();

        // Act
        var result = _sut.IsDBNull(0);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsDBNull_NullValue_ReturnsTrue()
    {
        // Arrange — row 1 has null Price
        _sut.Read();
        _sut.Read();

        // Act
        var result = _sut.IsDBNull(2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsDBNullAsync_WorksCorrectly()
    {
        // Arrange
        _sut.Read();

        // Act
        var result = await _sut.IsDBNullAsync(0, CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Indexer Tests

    [Fact]
    public void IntIndexer_ReturnsCorrectValue()
    {
        // Arrange
        _sut.Read();

        // Act
        var value = _sut[0];

        // Assert
        value.ShouldBe(1);
    }

    [Fact]
    public void StringIndexer_ReturnsCorrectValue()
    {
        // Arrange
        _sut.Read();

        // Act
        var value = _sut["Name"];

        // Assert
        value.ShouldBe("Widget");
    }

    #endregion

    #region Typed Getter Tests

    [Fact]
    public void GetFieldValue_Int_ReturnsCorrectValue()
    {
        // Arrange
        _sut.Read();

        // Act
        var value = _sut.GetFieldValue<int>(0);

        // Assert
        value.ShouldBe(1);
    }

    [Fact]
    public void GetFieldValue_String_ReturnsCorrectValue()
    {
        // Arrange
        _sut.Read();

        // Act
        var value = _sut.GetFieldValue<string>(1);

        // Assert
        value.ShouldBe("Widget");
    }

    [Fact]
    public void GetFieldValue_NullForNonNullableType_ThrowsInvalidCastException()
    {
        // Arrange — row 1, column 2 (Price) is null
        _sut.Read();
        _sut.Read();

        // Act & Assert
        Should.Throw<InvalidCastException>(() => _sut.GetFieldValue<int>(2));
    }

    [Fact]
    public void GetFieldValue_NullForNullableReferenceType_ReturnsNull()
    {
        // Arrange — row 1, column 2 (Price) is null
        _sut.Read();
        _sut.Read();

        // Act
        var value = _sut.GetFieldValue<string?>(2);

        // Assert
        value.ShouldBeNull();
    }

    [Fact]
    public void GetFieldValue_JsonElement_ConvertsCorrectly()
    {
        // Arrange — create result with JsonElement values
        var jsonDoc = JsonDocument.Parse("42");
        var jsonElement = jsonDoc.RootElement;

        var result = new CachedQueryResult
        {
            Columns =
            [
                new CachedColumnSchema("Value", 0, "int", typeof(int).AssemblyQualifiedName!, false)
            ],
            Rows = [new object?[] { jsonElement }],
            CachedAtUtc = DateTime.UtcNow
        };

        using var reader = new CachedDataReader(result);
        reader.Read();

        // Act
        var value = reader.GetFieldValue<int>(0);

        // Assert
        value.ShouldBe(42);
    }

    [Fact]
    public void GetFieldValue_JsonElement_String_ConvertsCorrectly()
    {
        // Arrange
        var jsonDoc = JsonDocument.Parse("\"hello\"");
        var jsonElement = jsonDoc.RootElement;

        var result = new CachedQueryResult
        {
            Columns =
            [
                new CachedColumnSchema("Value", 0, "nvarchar", typeof(string).AssemblyQualifiedName!, false)
            ],
            Rows = [new object?[] { jsonElement }],
            CachedAtUtc = DateTime.UtcNow
        };

        using var reader = new CachedDataReader(result);
        reader.Read();

        // Act
        var value = reader.GetFieldValue<string>(0);

        // Assert
        value.ShouldBe("hello");
    }

    [Fact]
    public void GetFieldValue_JsonElement_Bool_ConvertsCorrectly()
    {
        // Arrange
        var jsonDoc = JsonDocument.Parse("true");
        var result = new CachedQueryResult
        {
            Columns = [new CachedColumnSchema("Value", 0, "bit", typeof(bool).AssemblyQualifiedName!, false)],
            Rows = [new object?[] { jsonDoc.RootElement }],
            CachedAtUtc = DateTime.UtcNow
        };
        using var reader = new CachedDataReader(result);
        reader.Read();

        // Act
        var value = reader.GetFieldValue<bool>(0);

        // Assert
        value.ShouldBeTrue();
    }

    [Fact]
    public void GetFieldValue_JsonElement_Guid_ConvertsCorrectly()
    {
        // Arrange
        var expected = Guid.NewGuid();
        var jsonDoc = JsonDocument.Parse($"\"{expected}\"");
        var result = new CachedQueryResult
        {
            Columns = [new CachedColumnSchema("Value", 0, "uniqueidentifier", typeof(Guid).AssemblyQualifiedName!, false)],
            Rows = [new object?[] { jsonDoc.RootElement }],
            CachedAtUtc = DateTime.UtcNow
        };
        using var reader = new CachedDataReader(result);
        reader.Read();

        // Act
        var value = reader.GetFieldValue<Guid>(0);

        // Assert
        value.ShouldBe(expected);
    }

    [Fact]
    public void GetFieldValue_JsonElement_DateTime_ConvertsCorrectly()
    {
        // Arrange
        var expected = new DateTime(2026, 2, 8, 12, 0, 0, DateTimeKind.Utc);
        var jsonDoc = JsonDocument.Parse($"\"{expected:O}\"");
        var result = new CachedQueryResult
        {
            Columns = [new CachedColumnSchema("Value", 0, "datetime2", typeof(DateTime).AssemblyQualifiedName!, false)],
            Rows = [new object?[] { jsonDoc.RootElement }],
            CachedAtUtc = DateTime.UtcNow
        };
        using var reader = new CachedDataReader(result);
        reader.Read();

        // Act
        var value = reader.GetFieldValue<DateTime>(0);

        // Assert
        value.ShouldBe(expected);
    }

    [Fact]
    public void GetFieldValue_JsonElement_Decimal_ConvertsCorrectly()
    {
        // Arrange
        var jsonDoc = JsonDocument.Parse("99.99");
        var result = new CachedQueryResult
        {
            Columns = [new CachedColumnSchema("Value", 0, "decimal", typeof(decimal).AssemblyQualifiedName!, false)],
            Rows = [new object?[] { jsonDoc.RootElement }],
            CachedAtUtc = DateTime.UtcNow
        };
        using var reader = new CachedDataReader(result);
        reader.Read();

        // Act
        var value = reader.GetFieldValue<decimal>(0);

        // Assert
        value.ShouldBe(99.99m);
    }

    [Fact]
    public void GetFieldValue_JsonElement_Long_ConvertsCorrectly()
    {
        // Arrange
        var jsonDoc = JsonDocument.Parse("9999999999");
        var result = new CachedQueryResult
        {
            Columns = [new CachedColumnSchema("Value", 0, "bigint", typeof(long).AssemblyQualifiedName!, false)],
            Rows = [new object?[] { jsonDoc.RootElement }],
            CachedAtUtc = DateTime.UtcNow
        };
        using var reader = new CachedDataReader(result);
        reader.Read();

        // Act
        var value = reader.GetFieldValue<long>(0);

        // Assert
        value.ShouldBe(9999999999L);
    }

    [Fact]
    public async Task GetFieldValueAsync_ReturnsCorrectValue()
    {
        // Arrange
        _sut.Read();

        // Act
        var value = await _sut.GetFieldValueAsync<int>(0, CancellationToken.None);

        // Assert
        value.ShouldBe(1);
    }

    #endregion

    #region Convenience Typed Getters Tests

    [Fact]
    public void GetBoolean_ReturnsCorrectValue()
    {
        // Arrange — create reader with bool column
        var result = new CachedQueryResult
        {
            Columns = [new CachedColumnSchema("Active", 0, "bit", typeof(bool).AssemblyQualifiedName!, false)],
            Rows = [new object?[] { true }],
            CachedAtUtc = DateTime.UtcNow
        };
        using var reader = new CachedDataReader(result);
        reader.Read();

        // Act
        var value = reader.GetBoolean(0);

        // Assert
        value.ShouldBeTrue();
    }

    [Fact]
    public void GetInt32_ReturnsCorrectValue()
    {
        // Arrange
        _sut.Read();

        // Act
        var value = _sut.GetInt32(0);

        // Assert
        value.ShouldBe(1);
    }

    [Fact]
    public void GetString_ReturnsCorrectValue()
    {
        // Arrange
        _sut.Read();

        // Act
        var value = _sut.GetString(1);

        // Assert
        value.ShouldBe("Widget");
    }

    #endregion

    #region Unsupported Methods Tests

    [Fact]
    public void GetBytes_ThrowsNotSupportedException()
    {
        // Arrange
        _sut.Read();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => _sut.GetBytes(0, 0, null, 0, 0));
    }

    [Fact]
    public void GetChars_ThrowsNotSupportedException()
    {
        // Arrange
        _sut.Read();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => _sut.GetChars(0, 0, null, 0, 0));
    }

    [Fact]
    public void GetStream_ThrowsNotSupportedException()
    {
        // Arrange
        _sut.Read();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => _sut.GetStream(0));
    }

    [Fact]
    public void GetTextReader_ThrowsNotSupportedException()
    {
        // Arrange
        _sut.Read();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => _sut.GetTextReader(0));
    }

    #endregion

    #region Schema Access Tests

    [Fact]
    public void GetName_ReturnsCorrectColumnName()
    {
        // Act
        var name = _sut.GetName(0);

        // Assert
        name.ShouldBe("Id");
    }

    [Fact]
    public void GetName_InvalidOrdinal_ThrowsIndexOutOfRange()
    {
        // Act & Assert
        Should.Throw<IndexOutOfRangeException>(() => _sut.GetName(99));
    }

    [Fact]
    public void GetOrdinal_ReturnsCorrectOrdinal()
    {
        // Act
        var ordinal = _sut.GetOrdinal("Name");

        // Assert
        ordinal.ShouldBe(1);
    }

    [Fact]
    public void GetOrdinal_CaseInsensitive_ReturnsCorrectOrdinal()
    {
        // Act
        var ordinal = _sut.GetOrdinal("name");

        // Assert
        ordinal.ShouldBe(1);
    }

    [Fact]
    public void GetOrdinal_NotFound_ThrowsIndexOutOfRange()
    {
        // Act & Assert
        Should.Throw<IndexOutOfRangeException>(() => _sut.GetOrdinal("NonExistent"));
    }

    [Fact]
    public void GetOrdinal_NullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.GetOrdinal(null!));
    }

    [Fact]
    public void GetFieldType_ReturnsCorrectType()
    {
        // Act
        var fieldType = _sut.GetFieldType(0);

        // Assert
        fieldType.ShouldBe(typeof(int));
    }

    [Fact]
    public void GetDataTypeName_ReturnsCorrectTypeName()
    {
        // Act
        var typeName = _sut.GetDataTypeName(0);

        // Assert
        typeName.ShouldBe("int");
    }

    [Fact]
    public void GetSchemaTable_ReturnsValidDataTable()
    {
        // Act
        var schemaTable = _sut.GetSchemaTable();

        // Assert
        schemaTable.ShouldNotBeNull();
        schemaTable.Rows.Count.ShouldBe(3);
        schemaTable.Columns["ColumnName"].ShouldNotBeNull();
        schemaTable.Columns["ColumnOrdinal"].ShouldNotBeNull();
        schemaTable.Columns["DataType"].ShouldNotBeNull();
        schemaTable.Columns["DataTypeName"].ShouldNotBeNull();
        schemaTable.Columns["AllowDBNull"].ShouldNotBeNull();
    }

    [Fact]
    public void GetSchemaTable_OnClosedReader_ThrowsInvalidOperationException()
    {
        // Arrange
        _sut.Close();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _sut.GetSchemaTable());
    }

    #endregion

    #region Lifecycle Tests

    [Fact]
    public void Close_MakesReaderClosed()
    {
        // Act
        _sut.Close();

        // Assert
        _sut.IsClosed.ShouldBeTrue();
    }

    [Fact]
    public void Dispose_ClosesReader()
    {
        // Act
        _sut.Dispose();

        // Assert
        _sut.IsClosed.ShouldBeTrue();
    }

    [Fact]
    public async Task DisposeAsync_ClosesReader()
    {
        // Act
        await _sut.DisposeAsync();

        // Assert
        _sut.IsClosed.ShouldBeTrue();
    }

    [Fact]
    public void GetEnumerator_ReturnsNonNullEnumerator()
    {
        // Act
        var enumerator = _sut.GetEnumerator();

        // Assert
        enumerator.ShouldNotBeNull();
    }

    #endregion

    #region Test Helpers

    private static CachedQueryResult CreateTestResult()
    {
        return new CachedQueryResult
        {
            Columns = CreateTestColumns(),
            Rows =
            [
                new object?[] { 1, "Widget", 9.99m },
                new object?[] { 2, "Gadget", null }
            ],
            CachedAtUtc = DateTime.UtcNow
        };
    }

    private static List<CachedColumnSchema> CreateTestColumns()
    {
        return
        [
            new CachedColumnSchema("Id", 0, "int", typeof(int).AssemblyQualifiedName!, false),
            new CachedColumnSchema("Name", 1, "nvarchar", typeof(string).AssemblyQualifiedName!, false),
            new CachedColumnSchema("Price", 2, "decimal", typeof(decimal).AssemblyQualifiedName!, true)
        ];
    }

    #endregion
}
