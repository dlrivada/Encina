using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;
using Shouldly;
using DapperMySql = Encina.Dapper.MySQL.Anonymization.TokenMappingStoreDapper;
using DapperPostgreSql = Encina.Dapper.PostgreSQL.Anonymization.TokenMappingStoreDapper;
using DapperSqlServer = Encina.Dapper.SqlServer.Anonymization.TokenMappingStoreDapper;

namespace Encina.UnitTests.Compliance.StoreFailures;

/// <summary>
/// Exercises the successful read paths (no row, one row) of the Dapper token mapping stores of the three relational
/// providers against a fake data reader, so the error-handling changes of #1322 sit next to tested happy paths.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TokenMappingStoreDapperReadTests
{
    private static readonly DateTime CreatedUtc = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

    public static TheoryData<string> Providers => ["dapper-sqlserver", "dapper-postgresql", "dapper-mysql"];

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task GetByTokenAsync_WhenARowExists_ReturnsTheMapping(string provider)
    {
        var store = CreateStore(provider, withRow: true);

        var result = await store.GetByTokenAsync("token-1");

        AssertMapping(result);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task GetByOriginalValueHashAsync_WhenARowExists_ReturnsTheMapping(string provider)
    {
        var store = CreateStore(provider, withRow: true);

        var result = await store.GetByOriginalValueHashAsync("hash-1");

        AssertMapping(result);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task GetByTokenAsync_WhenNoRowExists_ReturnsNone(string provider)
    {
        var store = CreateStore(provider, withRow: false);

        var result = await store.GetByTokenAsync("token-1");

        result.IsRight.ShouldBeTrue();
        result.IfRight(option => option.IsNone.ShouldBeTrue());
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task GetByOriginalValueHashAsync_WhenNoRowExists_ReturnsNone(string provider)
    {
        var store = CreateStore(provider, withRow: false);

        var result = await store.GetByOriginalValueHashAsync("hash-1");

        result.IsRight.ShouldBeTrue();
        result.IfRight(option => option.IsNone.ShouldBeTrue());
    }

    private static void AssertMapping(LanguageExt.Either<EncinaError, LanguageExt.Option<TokenMapping>> result)
    {
        result.IsRight.ShouldBeTrue();
        result.IfRight(option =>
        {
            option.IsSome.ShouldBeTrue();
            option.IfSome(mapping =>
            {
                mapping.Token.ShouldBe("token-1");
                mapping.OriginalValueHash.ShouldBe("hash-1");
                mapping.KeyId.ShouldBe("key-1");
            });
        });
    }

    private static ITokenMappingStore CreateStore(string provider, bool withRow)
    {
        var connection = ConnectionReturning(BuildTable(provider, withRow));
        return provider switch
        {
            "dapper-sqlserver" => new DapperSqlServer(connection),
            "dapper-postgresql" => new DapperPostgreSql(connection),
            "dapper-mysql" => new DapperMySql(connection),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
    }

    private static DataTable BuildTable(string provider, bool withRow)
    {
        var lower = provider == "dapper-postgresql";
        var useOffset = provider == "dapper-sqlserver";
        string Name(string column) => lower ? column.ToLowerInvariant() : column;

        var table = new DataTable();
        table.Columns.Add(Name("Id"), typeof(string));
        table.Columns.Add(Name("Token"), typeof(string));
        table.Columns.Add(Name("OriginalValueHash"), typeof(string));
        table.Columns.Add(Name("EncryptedOriginalValue"), typeof(byte[]));
        table.Columns.Add(Name("KeyId"), typeof(string));
        table.Columns.Add(Name("CreatedAtUtc"), useOffset ? typeof(DateTimeOffset) : typeof(DateTime));
        table.Columns.Add(Name("ExpiresAtUtc"), useOffset ? typeof(DateTimeOffset) : typeof(DateTime));

        if (withRow)
        {
            object created = useOffset ? (object)new DateTimeOffset(CreatedUtc) : CreatedUtc;
            table.Rows.Add("id-1", "token-1", "hash-1", new byte[] { 1, 2, 3 }, "key-1", created, DBNull.Value);
        }

        return table;
    }

    private static TableConnection ConnectionReturning(DataTable table) => new TableConnection(table);

    /// <summary>A connection whose commands answer every query with the rows of a fixed table.</summary>
    private sealed class TableConnection(DataTable table) : DbConnection
    {
        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "fake";

        public override string DataSource => "fake";

        public override string ServerVersion => "1.0";

        public override ConnectionState State => ConnectionState.Open;

        public override void ChangeDatabase(string databaseName) { }

        public override void Close() { }

        public override void Open() { }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            throw new NotSupportedException();

        protected override DbCommand CreateDbCommand() => new TableCommand(this, table);
    }

    private sealed class TableCommand(DbConnection connection, DataTable table) : DbCommand
    {
        [AllowNull]
        public override string CommandText { get; set; } = string.Empty;

        public override int CommandTimeout { get; set; }

        public override CommandType CommandType { get; set; } = CommandType.Text;

        public override bool DesignTimeVisible { get; set; }

        public override UpdateRowSource UpdatedRowSource { get; set; }

        protected override DbConnection? DbConnection { get; set; } = connection;

        protected override DbParameterCollection DbParameterCollection { get; } = new FakeParameterCollection();

        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel() { }

        public override int ExecuteNonQuery() => 0;

        public override object? ExecuteScalar() => null;

        public override void Prepare() { }

        protected override DbParameter CreateDbParameter() => new FakeParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => table.CreateDataReader();
    }

    private sealed class FakeParameter : DbParameter
    {
        public override DbType DbType { get; set; }

        public override ParameterDirection Direction { get; set; }

        public override bool IsNullable { get; set; }

        [AllowNull]
        public override string ParameterName { get; set; } = string.Empty;

        [AllowNull]
        public override string SourceColumn { get; set; } = string.Empty;

        public override object? Value { get; set; }

        public override bool SourceColumnNullMapping { get; set; }

        public override int Size { get; set; }

        public override void ResetDbType() { }
    }

    private sealed class FakeParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = [];

        public override int Count => _items.Count;

        public override object SyncRoot => _items;

        public override int Add(object value)
        {
            _items.Add((DbParameter)value);
            return _items.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }

        public override void Clear() => _items.Clear();

        public override bool Contains(object value) => _items.Contains((DbParameter)value);

        public override bool Contains(string value) => _items.Exists(p => p.ParameterName == value);

        public override void CopyTo(Array array, int index) => ((System.Collections.ICollection)_items).CopyTo(array, index);

        public override System.Collections.IEnumerator GetEnumerator() => _items.GetEnumerator();

        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);

        public override int IndexOf(string parameterName) => _items.FindIndex(p => p.ParameterName == parameterName);

        public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);

        public override void Remove(object value) => _items.Remove((DbParameter)value);

        public override void RemoveAt(int index) => _items.RemoveAt(index);

        public override void RemoveAt(string parameterName) => _items.RemoveAt(IndexOf(parameterName));

        protected override DbParameter GetParameter(int index) => _items[index];

        protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];

        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;

        protected override void SetParameter(string parameterName, DbParameter value) => _items[IndexOf(parameterName)] = value;
    }
}
