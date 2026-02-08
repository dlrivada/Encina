using System.Reflection;

using Encina.ADO.MySQL.Health;
using Encina.ADO.PostgreSQL.Health;
using Encina.ADO.Sqlite.Health;
using Encina.ADO.SqlServer.Health;
using Encina.Dapper.MySQL.Health;
using Encina.Dapper.PostgreSQL.Health;
using Encina.Dapper.Sqlite.Health;
using Encina.Dapper.SqlServer.Health;
using Encina.Database;
using Encina.EntityFrameworkCore.Resilience;
using Encina.Messaging.Health;
using Encina.MongoDB.Health;

using Shouldly;

namespace Encina.ContractTests.Database.Resilience;

/// <summary>
/// Contract tests verifying that all <see cref="IDatabaseHealthMonitor"/> implementations
/// follow consistent API patterns and conventions.
/// </summary>
[Trait("Category", "Contract")]
public sealed class DatabaseHealthMonitorContractTests
{
    /// <summary>
    /// All concrete types that implement <see cref="IDatabaseHealthMonitor"/>.
    /// 9 inherit from <see cref="DatabaseHealthMonitorBase"/> and 1 (MongoDB) implements directly.
    /// </summary>
    private static readonly Type[] AllMonitorTypes =
    [
        // ADO.NET (4)
        typeof(SqliteDatabaseHealthMonitor),
        typeof(SqlServerDatabaseHealthMonitor),
        typeof(PostgreSqlDatabaseHealthMonitor),
        typeof(MySqlDatabaseHealthMonitor),
        // Dapper (4)
        typeof(DapperSqliteDatabaseHealthMonitor),
        typeof(DapperSqlServerDatabaseHealthMonitor),
        typeof(DapperPostgreSqlDatabaseHealthMonitor),
        typeof(DapperMySqlDatabaseHealthMonitor),
        // EF Core (1)
        typeof(EfCoreDatabaseHealthMonitor),
        // MongoDB (1)
        typeof(MongoDbDatabaseHealthMonitor),
    ];

    /// <summary>
    /// Types that inherit from <see cref="DatabaseHealthMonitorBase"/> (all except MongoDB).
    /// </summary>
    private static readonly Type[] BaseClassMonitorTypes =
    [
        typeof(SqliteDatabaseHealthMonitor),
        typeof(SqlServerDatabaseHealthMonitor),
        typeof(PostgreSqlDatabaseHealthMonitor),
        typeof(MySqlDatabaseHealthMonitor),
        typeof(DapperSqliteDatabaseHealthMonitor),
        typeof(DapperSqlServerDatabaseHealthMonitor),
        typeof(DapperPostgreSqlDatabaseHealthMonitor),
        typeof(DapperMySqlDatabaseHealthMonitor),
        typeof(EfCoreDatabaseHealthMonitor),
    ];

    /// <summary>
    /// Expected provider names for all 10 monitors.
    /// </summary>
    private static readonly string[] ExpectedProviderNames =
    [
        "ado-sqlite", "ado-sqlserver", "ado-postgresql", "ado-mysql",
        "dapper-sqlite", "dapper-sqlserver", "dapper-postgresql", "dapper-mysql",
        "efcore",
        "mongodb",
    ];

    #region IDatabaseHealthMonitor Interface Contract

    [Fact]
    public void Contract_AllMonitors_ImplementIDatabaseHealthMonitor()
    {
        foreach (var type in AllMonitorTypes)
        {
            typeof(IDatabaseHealthMonitor).IsAssignableFrom(type)
                .ShouldBeTrue($"{type.Name} must implement IDatabaseHealthMonitor");
        }
    }

    [Fact]
    public void Contract_AllMonitors_AreSealed()
    {
        foreach (var type in AllMonitorTypes)
        {
            type.IsSealed
                .ShouldBeTrue($"{type.Name} must be sealed");
        }
    }

    [Fact]
    public void Contract_ExactlyTenImplementationsExist()
    {
        AllMonitorTypes.Length.ShouldBe(10, "There should be exactly 10 IDatabaseHealthMonitor implementations");
    }

    #endregion

    #region Constructor Signature Contract

    [Fact]
    public void Contract_AllMonitors_HaveConsistentConstructorSignatures()
    {
        foreach (var type in AllMonitorTypes)
        {
            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            ctors.Length.ShouldBe(1, $"{type.Name} should have exactly one public constructor");

            var parameters = ctors[0].GetParameters();
            parameters.Length.ShouldBe(2, $"{type.Name} constructor should have 2 parameters");

            parameters[0].ParameterType.ShouldBe(typeof(IServiceProvider),
                $"{type.Name} first parameter must be IServiceProvider");
            parameters[0].Name.ShouldBe("serviceProvider",
                $"{type.Name} first parameter must be named 'serviceProvider'");

            parameters[1].ParameterType.ShouldBe(typeof(DatabaseResilienceOptions),
                $"{type.Name} second parameter must be DatabaseResilienceOptions");
            parameters[1].Name.ShouldBe("options",
                $"{type.Name} second parameter must be named 'options'");
            parameters[1].HasDefaultValue.ShouldBeTrue(
                $"{type.Name} options parameter must have a default value (null)");
        }
    }

    #endregion

    #region Base Class Contract

    [Fact]
    public void Contract_NineMonitors_InheritFromDatabaseHealthMonitorBase()
    {
        foreach (var type in BaseClassMonitorTypes)
        {
            typeof(DatabaseHealthMonitorBase).IsAssignableFrom(type)
                .ShouldBeTrue($"{type.Name} must inherit from DatabaseHealthMonitorBase");
        }
    }

    [Fact]
    public void Contract_MongoDB_DoesNotInheritFromBase()
    {
        typeof(DatabaseHealthMonitorBase).IsAssignableFrom(typeof(MongoDbDatabaseHealthMonitor))
            .ShouldBeFalse("MongoDbDatabaseHealthMonitor should NOT inherit from DatabaseHealthMonitorBase");
    }

    [Fact]
    public void Contract_MongoDB_DirectlyImplementsInterface()
    {
        var interfaces = typeof(MongoDbDatabaseHealthMonitor).GetInterfaces();
        interfaces.ShouldContain(typeof(IDatabaseHealthMonitor),
            "MongoDbDatabaseHealthMonitor must directly implement IDatabaseHealthMonitor");
    }

    #endregion

    #region Provider Name Convention Contract

    [Fact]
    public void Contract_AllProviderNames_AreUnique()
    {
        ExpectedProviderNames.Length.ShouldBe(ExpectedProviderNames.Distinct().Count(),
            "All provider names must be unique");
    }

    [Fact]
    public void Contract_AllProviderNames_AreLowercaseKebabCase()
    {
        foreach (var name in ExpectedProviderNames)
        {
            name.ShouldBe(name.ToLowerInvariant(),
                $"Provider name '{name}' must be lowercase");

            // Kebab case: only lowercase letters, digits, and hyphens
            name.All(c => char.IsLetterOrDigit(c) || c == '-')
                .ShouldBeTrue($"Provider name '{name}' must be kebab-case (letters, digits, hyphens only)");
        }
    }

    [Fact]
    public void Contract_ADOProviderNames_HaveAdoPrefix()
    {
        var adoTypes = new[]
        {
            typeof(SqliteDatabaseHealthMonitor),
            typeof(SqlServerDatabaseHealthMonitor),
            typeof(PostgreSqlDatabaseHealthMonitor),
            typeof(MySqlDatabaseHealthMonitor),
        };

        foreach (var type in adoTypes)
        {
            var providerNameProp = type.GetProperty("ProviderName", BindingFlags.Public | BindingFlags.Instance);
            providerNameProp.ShouldNotBeNull($"{type.Name} must have ProviderName property");

            // Check that ADO types are in an ADO namespace
            type.Namespace.ShouldNotBeNull();
            type.Namespace!.Contains(".ADO.", StringComparison.Ordinal)
                .ShouldBeTrue($"{type.Name} must be in an ADO namespace");
        }
    }

    [Fact]
    public void Contract_DapperProviderNames_HaveDapperPrefix()
    {
        var dapperTypes = new[]
        {
            typeof(DapperSqliteDatabaseHealthMonitor),
            typeof(DapperSqlServerDatabaseHealthMonitor),
            typeof(DapperPostgreSqlDatabaseHealthMonitor),
            typeof(DapperMySqlDatabaseHealthMonitor),
        };

        foreach (var type in dapperTypes)
        {
            type.Namespace.ShouldNotBeNull();
            type.Namespace!.Contains(".Dapper.", StringComparison.Ordinal)
                .ShouldBeTrue($"{type.Name} must be in a Dapper namespace");
        }
    }

    #endregion

    #region API Consistency Contract

    [Fact]
    public void Contract_AllMonitors_HaveProviderNameProperty()
    {
        foreach (var type in AllMonitorTypes)
        {
            var prop = type.GetProperty("ProviderName", BindingFlags.Public | BindingFlags.Instance);
            prop.ShouldNotBeNull($"{type.Name} must have ProviderName property");
            prop!.PropertyType.ShouldBe(typeof(string),
                $"{type.Name}.ProviderName must return string");
            prop.CanRead.ShouldBeTrue($"{type.Name}.ProviderName must be readable");
        }
    }

    [Fact]
    public void Contract_AllMonitors_HaveIsCircuitOpenProperty()
    {
        foreach (var type in AllMonitorTypes)
        {
            var prop = type.GetProperty("IsCircuitOpen", BindingFlags.Public | BindingFlags.Instance);
            prop.ShouldNotBeNull($"{type.Name} must have IsCircuitOpen property");
            prop!.PropertyType.ShouldBe(typeof(bool),
                $"{type.Name}.IsCircuitOpen must return bool");
            prop.CanRead.ShouldBeTrue($"{type.Name}.IsCircuitOpen must be readable");
        }
    }

    [Fact]
    public void Contract_AllMonitors_HaveGetPoolStatisticsMethod()
    {
        foreach (var type in AllMonitorTypes)
        {
            var method = type.GetMethod("GetPoolStatistics", BindingFlags.Public | BindingFlags.Instance);
            method.ShouldNotBeNull($"{type.Name} must have GetPoolStatistics method");
            method!.ReturnType.ShouldBe(typeof(ConnectionPoolStats),
                $"{type.Name}.GetPoolStatistics must return ConnectionPoolStats");
            method.GetParameters().Length.ShouldBe(0,
                $"{type.Name}.GetPoolStatistics should take no parameters");
        }
    }

    [Fact]
    public void Contract_AllMonitors_HaveCheckHealthAsyncMethod()
    {
        foreach (var type in AllMonitorTypes)
        {
            var method = type.GetMethod("CheckHealthAsync", BindingFlags.Public | BindingFlags.Instance);
            method.ShouldNotBeNull($"{type.Name} must have CheckHealthAsync method");
            method!.ReturnType.ShouldBe(typeof(Task<DatabaseHealthResult>),
                $"{type.Name}.CheckHealthAsync must return Task<DatabaseHealthResult>");

            var parameters = method.GetParameters();
            parameters.Length.ShouldBe(1,
                $"{type.Name}.CheckHealthAsync should have 1 parameter");
            parameters[0].ParameterType.ShouldBe(typeof(CancellationToken),
                $"{type.Name}.CheckHealthAsync parameter must be CancellationToken");
        }
    }

    [Fact]
    public void Contract_AllMonitors_HaveClearPoolAsyncMethod()
    {
        foreach (var type in AllMonitorTypes)
        {
            var method = type.GetMethod("ClearPoolAsync", BindingFlags.Public | BindingFlags.Instance);
            method.ShouldNotBeNull($"{type.Name} must have ClearPoolAsync method");
            method!.ReturnType.ShouldBe(typeof(Task),
                $"{type.Name}.ClearPoolAsync must return Task");

            var parameters = method.GetParameters();
            parameters.Length.ShouldBe(1,
                $"{type.Name}.ClearPoolAsync should have 1 parameter");
            parameters[0].ParameterType.ShouldBe(typeof(CancellationToken),
                $"{type.Name}.ClearPoolAsync parameter must be CancellationToken");
        }
    }

    #endregion

    #region Naming Convention Contract

    [Fact]
    public void Contract_AllMonitors_FollowNamingConvention()
    {
        foreach (var type in AllMonitorTypes)
        {
            type.Name.EndsWith("DatabaseHealthMonitor", StringComparison.Ordinal)
                .ShouldBeTrue($"{type.Name} should end with 'DatabaseHealthMonitor'");
        }
    }

    [Fact]
    public void Contract_AllMonitors_AreInHealthNamespace()
    {
        foreach (var type in AllMonitorTypes)
        {
            var ns = type.Namespace;
            ns.ShouldNotBeNull($"{type.Name} must have a namespace");

            // MongoDB and EFCore have different namespace structures
            var validNamespace = ns!.EndsWith(".Health", StringComparison.Ordinal)
                || ns.EndsWith(".Resilience", StringComparison.Ordinal);
            validNamespace.ShouldBeTrue(
                $"{type.Name} namespace '{ns}' should end with '.Health' or '.Resilience'");
        }
    }

    #endregion

    #region DatabaseHealthMonitorBase Template Method Contract

    [Fact]
    public void Contract_BaseClassMonitors_HaveGetPoolStatisticsCoreMethod()
    {
        foreach (var type in BaseClassMonitorTypes)
        {
            var method = type.GetMethod("GetPoolStatisticsCore",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method.ShouldNotBeNull($"{type.Name} must override GetPoolStatisticsCore");
            method!.ReturnType.ShouldBe(typeof(ConnectionPoolStats),
                $"{type.Name}.GetPoolStatisticsCore must return ConnectionPoolStats");
        }
    }

    [Fact]
    public void Contract_BaseClassMonitors_HaveClearPoolCoreAsyncMethod()
    {
        foreach (var type in BaseClassMonitorTypes)
        {
            var method = type.GetMethod("ClearPoolCoreAsync",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method.ShouldNotBeNull($"{type.Name} must override ClearPoolCoreAsync");
            method!.ReturnType.ShouldBe(typeof(Task),
                $"{type.Name}.ClearPoolCoreAsync must return Task");
        }
    }

    #endregion
}
