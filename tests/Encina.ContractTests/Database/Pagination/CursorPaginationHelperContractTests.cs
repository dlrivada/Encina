using System.Reflection;

using Shouldly;

using ADOMySQLPagination = Encina.ADO.MySQL.Pagination;
using ADOPostgreSQLPagination = Encina.ADO.PostgreSQL.Pagination;
using ADOSqlServerPagination = Encina.ADO.SqlServer.Pagination;

namespace Encina.ContractTests.Database.Pagination;

/// <summary>
/// Contract tests verifying that all CursorPaginationHelper implementations follow the same interface contracts.
/// These tests ensure behavioral and API consistency across all 4 ADO.NET database providers.
/// </summary>
[Trait("Category", "Contract")]
public sealed class CursorPaginationHelperContractTests
{
    #region CursorPaginationHelper Type Contract Tests

    [Fact]
    public void Contract_AllADOProviders_CursorPaginationHelper_ExistsInAllProviders()
    {
        // Contract: CursorPaginationHelper<TEntity> must exist in all 4 ADO.NET providers
        var SqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var sqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var postgresType = typeof(ADOPostgreSQLPagination.CursorPaginationHelper<>);
        var mysqlType = typeof(ADOMySQLPagination.CursorPaginationHelper<>);

        SqlServerType.ShouldNotBeNull("SqlServer CursorPaginationHelper should exist");
        sqlServerType.ShouldNotBeNull("SQL Server CursorPaginationHelper should exist");
        postgresType.ShouldNotBeNull("PostgreSQL CursorPaginationHelper should exist");
        mysqlType.ShouldNotBeNull("MySQL CursorPaginationHelper should exist");
    }

    [Fact]
    public void Contract_AllADOProviders_CursorPaginationHelper_HaveIdenticalMethods()
    {
        // Contract: All ADO.NET providers must have identical CursorPaginationHelper public methods
        var SqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var sqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var postgresType = typeof(ADOPostgreSQLPagination.CursorPaginationHelper<>);
        var mysqlType = typeof(ADOMySQLPagination.CursorPaginationHelper<>);

        // Verify all providers have the same methods
        VerifyPublicMethodsMatch(SqlServerType, sqlServerType, "SqlServer vs SQL Server");
        VerifyPublicMethodsMatch(SqlServerType, postgresType, "SqlServer vs PostgreSQL");
        VerifyPublicMethodsMatch(SqlServerType, mysqlType, "SqlServer vs MySQL");
    }

    [Fact]
    public void Contract_AllADOProviders_CursorPaginationHelper_HaveExecuteAsync()
    {
        // Contract: All providers must have ExecuteAsync method
        var SqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var sqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var postgresType = typeof(ADOPostgreSQLPagination.CursorPaginationHelper<>);
        var mysqlType = typeof(ADOMySQLPagination.CursorPaginationHelper<>);

        VerifyMethodExists(SqlServerType, "ExecuteAsync", "SqlServer");
        VerifyMethodExists(sqlServerType, "ExecuteAsync", "SQL Server");
        VerifyMethodExists(postgresType, "ExecuteAsync", "PostgreSQL");
        VerifyMethodExists(mysqlType, "ExecuteAsync", "MySQL");
    }

    [Fact]
    public void Contract_AllADOProviders_CursorPaginationHelper_HaveExecuteCompositeAsync()
    {
        // Contract: All providers must have ExecuteCompositeAsync method
        var SqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var sqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var postgresType = typeof(ADOPostgreSQLPagination.CursorPaginationHelper<>);
        var mysqlType = typeof(ADOMySQLPagination.CursorPaginationHelper<>);

        VerifyMethodExists(SqlServerType, "ExecuteCompositeAsync", "SqlServer");
        VerifyMethodExists(sqlServerType, "ExecuteCompositeAsync", "SQL Server");
        VerifyMethodExists(postgresType, "ExecuteCompositeAsync", "PostgreSQL");
        VerifyMethodExists(mysqlType, "ExecuteCompositeAsync", "MySQL");
    }

    [Fact]
    public void Contract_AllADOProviders_CursorPaginationHelper_HaveThreeParameterConstructor()
    {
        // Contract: All providers must have constructor with (DbConnection, ICursorEncoder, Func<IDataReader, TEntity>)
        var SqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var sqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var postgresType = typeof(ADOPostgreSQLPagination.CursorPaginationHelper<>);
        var mysqlType = typeof(ADOMySQLPagination.CursorPaginationHelper<>);

        VerifyConstructorParameterCount(SqlServerType, 3, "SqlServer");
        VerifyConstructorParameterCount(sqlServerType, 3, "SQL Server");
        VerifyConstructorParameterCount(postgresType, 3, "PostgreSQL");
        VerifyConstructorParameterCount(mysqlType, 3, "MySQL");
    }

    #endregion

    #region Return Type Contract Tests

    [Fact]
    public void Contract_AllADOProviders_ExecuteAsync_ReturnsCursorPaginatedResult()
    {
        // Contract: ExecuteAsync must return Task<CursorPaginatedResult<TEntity>>
        var SqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var sqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var postgresType = typeof(ADOPostgreSQLPagination.CursorPaginationHelper<>);
        var mysqlType = typeof(ADOMySQLPagination.CursorPaginationHelper<>);

        VerifyMethodReturnsTask(SqlServerType, "ExecuteAsync", "SqlServer");
        VerifyMethodReturnsTask(sqlServerType, "ExecuteAsync", "SQL Server");
        VerifyMethodReturnsTask(postgresType, "ExecuteAsync", "PostgreSQL");
        VerifyMethodReturnsTask(mysqlType, "ExecuteAsync", "MySQL");
    }

    [Fact]
    public void Contract_AllADOProviders_ExecuteCompositeAsync_ReturnsCursorPaginatedResult()
    {
        // Contract: ExecuteCompositeAsync must return Task<CursorPaginatedResult<TEntity>>
        var SqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var sqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var postgresType = typeof(ADOPostgreSQLPagination.CursorPaginationHelper<>);
        var mysqlType = typeof(ADOMySQLPagination.CursorPaginationHelper<>);

        VerifyMethodReturnsTask(SqlServerType, "ExecuteCompositeAsync", "SqlServer");
        VerifyMethodReturnsTask(sqlServerType, "ExecuteCompositeAsync", "SQL Server");
        VerifyMethodReturnsTask(postgresType, "ExecuteCompositeAsync", "PostgreSQL");
        VerifyMethodReturnsTask(mysqlType, "ExecuteCompositeAsync", "MySQL");
    }

    #endregion

    #region Namespace Contract Tests

    [Fact]
    public void Contract_AllADOProviders_CursorPaginationHelper_InPaginationNamespace()
    {
        // Contract: All CursorPaginationHelper types must be in .Pagination namespace
        var SqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var sqlServerType = typeof(ADOSqlServerPagination.CursorPaginationHelper<>);
        var postgresType = typeof(ADOPostgreSQLPagination.CursorPaginationHelper<>);
        var mysqlType = typeof(ADOMySQLPagination.CursorPaginationHelper<>);

        SqlServerType.Namespace.ShouldEndWith(".Pagination");
        sqlServerType.Namespace.ShouldEndWith(".Pagination");
        postgresType.Namespace.ShouldEndWith(".Pagination");
        mysqlType.Namespace.ShouldEndWith(".Pagination");
    }

    #endregion

    #region Helper Methods

    private static void VerifyPublicMethodsMatch(Type type1, Type type2, string comparison)
    {
        var methods1 = GetPublicMethodNames(type1);
        var methods2 = GetPublicMethodNames(type2);

        var missing1 = methods2.Except(methods1).ToList();
        var missing2 = methods1.Except(methods2).ToList();

        missing1.ShouldBeEmpty($"{comparison}: First type is missing methods: {string.Join(", ", missing1)}");
        missing2.ShouldBeEmpty($"{comparison}: Second type is missing methods: {string.Join(", ", missing2)}");
    }

    private static HashSet<string> GetPublicMethodNames(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName) // Exclude property accessors
            .Select(m => m.Name)
            .ToHashSet();
    }

    private static void VerifyMethodExists(Type type, string methodName, string providerName)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == methodName)
            .ToList();

        methods.ShouldNotBeEmpty($"{providerName} CursorPaginationHelper should have {methodName} method");
    }

    private static void VerifyConstructorParameterCount(Type type, int expectedCount, string providerName)
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        constructors.ShouldContain(c => c.GetParameters().Length == expectedCount,
            $"{providerName} CursorPaginationHelper should have constructor with {expectedCount} parameters");
    }

    private static void VerifyMethodReturnsTask(Type type, string methodName, string providerName)
    {
        var method = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == methodName);

        method.ShouldNotBeNull($"{providerName} should have {methodName} method");
        method!.ReturnType.Name.ShouldStartWith("Task");
    }

    #endregion
}
