using System.Reflection;
using Encina.Sharding.ReferenceTables;
using Shouldly;
using ADOMySQLRefTable = Encina.ADO.MySQL.Sharding.ReferenceTables;
using ADOPostgreSQLRefTable = Encina.ADO.PostgreSQL.Sharding.ReferenceTables;
using ADOSqliteRefTable = Encina.ADO.Sqlite.Sharding.ReferenceTables;
using ADOSqlServerRefTable = Encina.ADO.SqlServer.Sharding.ReferenceTables;
using DapperMySQLRefTable = Encina.Dapper.MySQL.Sharding.ReferenceTables;
using DapperPostgreSQLRefTable = Encina.Dapper.PostgreSQL.Sharding.ReferenceTables;
using DapperSqliteRefTable = Encina.Dapper.Sqlite.Sharding.ReferenceTables;
using DapperSqlServerRefTable = Encina.Dapper.SqlServer.Sharding.ReferenceTables;
using EFCoreRefTable = Encina.EntityFrameworkCore.Sharding.ReferenceTables;
using MongoDBRefTable = Encina.MongoDB.Sharding.ReferenceTables;

namespace Encina.ContractTests.Sharding.ReferenceTables;

/// <summary>
/// Contract tests verifying that all 13 database providers implement the
/// <see cref="IReferenceTableStoreFactory"/> and <see cref="IReferenceTableStore"/>
/// interfaces consistently with matching method signatures and naming conventions.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ReferenceTableStoreContractTests
{
    private static readonly Type StoreInterfaceType = typeof(IReferenceTableStore);
    private static readonly Type FactoryInterfaceType = typeof(IReferenceTableStoreFactory);

    #region IReferenceTableStore Interface Contract

    [Fact]
    public void Contract_IReferenceTableStore_HasExactlyThreeMethods()
    {
        var methods = StoreInterfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Length.ShouldBe(3,
            "IReferenceTableStore should have exactly 3 methods: UpsertAsync, GetAllAsync, GetHashAsync");
    }

    [Fact]
    public void Contract_IReferenceTableStore_UpsertAsync_HasCorrectSignature()
    {
        var method = StoreInterfaceType.GetMethod("UpsertAsync");

        method.ShouldNotBeNull("IReferenceTableStore must have UpsertAsync method");
        method!.IsGenericMethod.ShouldBeTrue("UpsertAsync must be a generic method");
        method.GetGenericArguments().Length.ShouldBe(1, "UpsertAsync must have exactly 1 generic type parameter");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2, "UpsertAsync must have 2 parameters (entities, cancellationToken)");
        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken), "Second parameter must be CancellationToken");
        parameters[1].HasDefaultValue.ShouldBeTrue("CancellationToken parameter must have a default value");
    }

    [Fact]
    public void Contract_IReferenceTableStore_GetAllAsync_HasCorrectSignature()
    {
        var method = StoreInterfaceType.GetMethod("GetAllAsync");

        method.ShouldNotBeNull("IReferenceTableStore must have GetAllAsync method");
        method!.IsGenericMethod.ShouldBeTrue("GetAllAsync must be a generic method");
        method.GetGenericArguments().Length.ShouldBe(1, "GetAllAsync must have exactly 1 generic type parameter");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1, "GetAllAsync must have 1 parameter (cancellationToken)");
        parameters[0].ParameterType.ShouldBe(typeof(CancellationToken), "Parameter must be CancellationToken");
        parameters[0].HasDefaultValue.ShouldBeTrue("CancellationToken parameter must have a default value");
    }

    [Fact]
    public void Contract_IReferenceTableStore_GetHashAsync_HasCorrectSignature()
    {
        var method = StoreInterfaceType.GetMethod("GetHashAsync");

        method.ShouldNotBeNull("IReferenceTableStore must have GetHashAsync method");
        method!.IsGenericMethod.ShouldBeTrue("GetHashAsync must be a generic method");
        method.GetGenericArguments().Length.ShouldBe(1, "GetHashAsync must have exactly 1 generic type parameter");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1, "GetHashAsync must have 1 parameter (cancellationToken)");
        parameters[0].ParameterType.ShouldBe(typeof(CancellationToken), "Parameter must be CancellationToken");
        parameters[0].HasDefaultValue.ShouldBeTrue("CancellationToken parameter must have a default value");
    }

    #endregion

    #region IReferenceTableStoreFactory Interface Contract

    [Fact]
    public void Contract_IReferenceTableStoreFactory_HasCreateForShardMethod()
    {
        var method = FactoryInterfaceType.GetMethod("CreateForShard");

        method.ShouldNotBeNull("IReferenceTableStoreFactory must have CreateForShard method");

        var parameters = method!.GetParameters();
        parameters.Length.ShouldBe(1, "CreateForShard must accept exactly 1 parameter");
        parameters[0].ParameterType.ShouldBe(typeof(string),
            "CreateForShard parameter must be string (connectionString)");
    }

    [Fact]
    public void Contract_IReferenceTableStoreFactory_CreateForShard_ReturnsIReferenceTableStore()
    {
        var method = FactoryInterfaceType.GetMethod("CreateForShard");

        method!.ReturnType.ShouldBe(StoreInterfaceType,
            "CreateForShard must return IReferenceTableStore");
    }

    #endregion

    #region All ADO Providers Implement IReferenceTableStoreFactory

    [Fact]
    public void Contract_ADO_Sqlite_Factory_ImplementsIReferenceTableStoreFactory()
    {
        VerifyImplementsInterface(
            typeof(ADOSqliteRefTable.ReferenceTableStoreFactoryADO),
            FactoryInterfaceType,
            "ADO.Sqlite");
    }

    [Fact]
    public void Contract_ADO_SqlServer_Factory_ImplementsIReferenceTableStoreFactory()
    {
        VerifyImplementsInterface(
            typeof(ADOSqlServerRefTable.ReferenceTableStoreFactoryADO),
            FactoryInterfaceType,
            "ADO.SqlServer");
    }

    [Fact]
    public void Contract_ADO_PostgreSQL_Factory_ImplementsIReferenceTableStoreFactory()
    {
        VerifyImplementsInterface(
            typeof(ADOPostgreSQLRefTable.ReferenceTableStoreFactoryADO),
            FactoryInterfaceType,
            "ADO.PostgreSQL");
    }

    [Fact]
    public void Contract_ADO_MySQL_Factory_ImplementsIReferenceTableStoreFactory()
    {
        VerifyImplementsInterface(
            typeof(ADOMySQLRefTable.ReferenceTableStoreFactoryADO),
            FactoryInterfaceType,
            "ADO.MySQL");
    }

    #endregion

    #region All Dapper Providers Implement IReferenceTableStoreFactory

    [Fact]
    public void Contract_Dapper_Sqlite_Factory_ImplementsIReferenceTableStoreFactory()
    {
        VerifyImplementsInterface(
            typeof(DapperSqliteRefTable.ReferenceTableStoreFactoryDapper),
            FactoryInterfaceType,
            "Dapper.Sqlite");
    }

    [Fact]
    public void Contract_Dapper_SqlServer_Factory_ImplementsIReferenceTableStoreFactory()
    {
        VerifyImplementsInterface(
            typeof(DapperSqlServerRefTable.ReferenceTableStoreFactoryDapper),
            FactoryInterfaceType,
            "Dapper.SqlServer");
    }

    [Fact]
    public void Contract_Dapper_PostgreSQL_Factory_ImplementsIReferenceTableStoreFactory()
    {
        VerifyImplementsInterface(
            typeof(DapperPostgreSQLRefTable.ReferenceTableStoreFactoryDapper),
            FactoryInterfaceType,
            "Dapper.PostgreSQL");
    }

    [Fact]
    public void Contract_Dapper_MySQL_Factory_ImplementsIReferenceTableStoreFactory()
    {
        VerifyImplementsInterface(
            typeof(DapperMySQLRefTable.ReferenceTableStoreFactoryDapper),
            FactoryInterfaceType,
            "Dapper.MySQL");
    }

    #endregion

    #region EF Core and MongoDB Implement IReferenceTableStoreFactory

    [Fact]
    public void Contract_EFCore_Factory_ImplementsIReferenceTableStoreFactory()
    {
        // ReferenceTableStoreFactoryEF<TContext> is generic; verify the open generic type
        var factoryType = typeof(EFCoreRefTable.ReferenceTableStoreFactoryEF<>);
        var interfaces = factoryType.GetInterfaces();

        interfaces.ShouldContain(FactoryInterfaceType,
            "EFCore ReferenceTableStoreFactoryEF<> must implement IReferenceTableStoreFactory");
    }

    [Fact]
    public void Contract_MongoDB_Factory_ImplementsIReferenceTableStoreFactory()
    {
        VerifyImplementsInterface(
            typeof(MongoDBRefTable.ReferenceTableStoreFactoryMongoDB),
            FactoryInterfaceType,
            "MongoDB");
    }

    #endregion

    #region All ADO Providers Implement IReferenceTableStore

    [Fact]
    public void Contract_ADO_Sqlite_Store_ImplementsIReferenceTableStore()
    {
        VerifyImplementsInterface(
            typeof(ADOSqliteRefTable.ReferenceTableStoreADO),
            StoreInterfaceType,
            "ADO.Sqlite");
    }

    [Fact]
    public void Contract_ADO_SqlServer_Store_ImplementsIReferenceTableStore()
    {
        VerifyImplementsInterface(
            typeof(ADOSqlServerRefTable.ReferenceTableStoreADO),
            StoreInterfaceType,
            "ADO.SqlServer");
    }

    [Fact]
    public void Contract_ADO_PostgreSQL_Store_ImplementsIReferenceTableStore()
    {
        VerifyImplementsInterface(
            typeof(ADOPostgreSQLRefTable.ReferenceTableStoreADO),
            StoreInterfaceType,
            "ADO.PostgreSQL");
    }

    [Fact]
    public void Contract_ADO_MySQL_Store_ImplementsIReferenceTableStore()
    {
        VerifyImplementsInterface(
            typeof(ADOMySQLRefTable.ReferenceTableStoreADO),
            StoreInterfaceType,
            "ADO.MySQL");
    }

    #endregion

    #region All Dapper Providers Implement IReferenceTableStore

    [Fact]
    public void Contract_Dapper_Sqlite_Store_ImplementsIReferenceTableStore()
    {
        VerifyImplementsInterface(
            typeof(DapperSqliteRefTable.ReferenceTableStoreDapper),
            StoreInterfaceType,
            "Dapper.Sqlite");
    }

    [Fact]
    public void Contract_Dapper_SqlServer_Store_ImplementsIReferenceTableStore()
    {
        VerifyImplementsInterface(
            typeof(DapperSqlServerRefTable.ReferenceTableStoreDapper),
            StoreInterfaceType,
            "Dapper.SqlServer");
    }

    [Fact]
    public void Contract_Dapper_PostgreSQL_Store_ImplementsIReferenceTableStore()
    {
        VerifyImplementsInterface(
            typeof(DapperPostgreSQLRefTable.ReferenceTableStoreDapper),
            StoreInterfaceType,
            "Dapper.PostgreSQL");
    }

    [Fact]
    public void Contract_Dapper_MySQL_Store_ImplementsIReferenceTableStore()
    {
        VerifyImplementsInterface(
            typeof(DapperMySQLRefTable.ReferenceTableStoreDapper),
            StoreInterfaceType,
            "Dapper.MySQL");
    }

    #endregion

    #region EF Core and MongoDB Implement IReferenceTableStore

    [Fact]
    public void Contract_EFCore_Store_ImplementsIReferenceTableStore()
    {
        VerifyImplementsInterface(
            typeof(EFCoreRefTable.ReferenceTableStoreEF),
            StoreInterfaceType,
            "EFCore");
    }

    [Fact]
    public void Contract_MongoDB_Store_ImplementsIReferenceTableStore()
    {
        VerifyImplementsInterface(
            typeof(MongoDBRefTable.ReferenceTableStoreMongoDB),
            StoreInterfaceType,
            "MongoDB");
    }

    #endregion

    #region Store Method Signature Consistency

    [Fact]
    public void Contract_AllStoreImplementations_HaveConsistentPublicMethods()
    {
        // Use the ADO.Sqlite store as the reference implementation
        var referenceType = typeof(ADOSqliteRefTable.ReferenceTableStoreADO);
        var referenceMethods = GetPublicInstanceMethods(referenceType);

        var storeTypes = new (Type Type, string Name)[]
        {
            (typeof(ADOSqlServerRefTable.ReferenceTableStoreADO), "ADO.SqlServer"),
            (typeof(ADOPostgreSQLRefTable.ReferenceTableStoreADO), "ADO.PostgreSQL"),
            (typeof(ADOMySQLRefTable.ReferenceTableStoreADO), "ADO.MySQL"),
            (typeof(DapperSqliteRefTable.ReferenceTableStoreDapper), "Dapper.Sqlite"),
            (typeof(DapperSqlServerRefTable.ReferenceTableStoreDapper), "Dapper.SqlServer"),
            (typeof(DapperPostgreSQLRefTable.ReferenceTableStoreDapper), "Dapper.PostgreSQL"),
            (typeof(DapperMySQLRefTable.ReferenceTableStoreDapper), "Dapper.MySQL"),
            (typeof(EFCoreRefTable.ReferenceTableStoreEF), "EFCore"),
            (typeof(MongoDBRefTable.ReferenceTableStoreMongoDB), "MongoDB"),
        };

        foreach (var (storeType, providerName) in storeTypes)
        {
            var storeMethods = GetPublicInstanceMethods(storeType);

            foreach (var method in referenceMethods)
            {
                storeMethods.ShouldContain(method,
                    $"{providerName} ReferenceTableStore is missing method '{method}' present in ADO.Sqlite reference");
            }
        }
    }

    [Fact]
    public void Contract_AllFactoryImplementations_HaveConsistentPublicMethods()
    {
        // Use the ADO.Sqlite factory as the reference implementation
        var referenceType = typeof(ADOSqliteRefTable.ReferenceTableStoreFactoryADO);
        var referenceMethods = GetPublicInstanceMethods(referenceType);

        var factoryTypes = new (Type Type, string Name)[]
        {
            (typeof(ADOSqlServerRefTable.ReferenceTableStoreFactoryADO), "ADO.SqlServer"),
            (typeof(ADOPostgreSQLRefTable.ReferenceTableStoreFactoryADO), "ADO.PostgreSQL"),
            (typeof(ADOMySQLRefTable.ReferenceTableStoreFactoryADO), "ADO.MySQL"),
            (typeof(DapperSqliteRefTable.ReferenceTableStoreFactoryDapper), "Dapper.Sqlite"),
            (typeof(DapperSqlServerRefTable.ReferenceTableStoreFactoryDapper), "Dapper.SqlServer"),
            (typeof(DapperPostgreSQLRefTable.ReferenceTableStoreFactoryDapper), "Dapper.PostgreSQL"),
            (typeof(DapperMySQLRefTable.ReferenceTableStoreFactoryDapper), "Dapper.MySQL"),
            (typeof(MongoDBRefTable.ReferenceTableStoreFactoryMongoDB), "MongoDB"),
        };

        foreach (var (factoryType, providerName) in factoryTypes)
        {
            var factoryMethods = GetPublicInstanceMethods(factoryType);

            foreach (var method in referenceMethods)
            {
                factoryMethods.ShouldContain(method,
                    $"{providerName} ReferenceTableStoreFactory is missing method '{method}' present in ADO.Sqlite reference");
            }
        }
    }

    #endregion

    #region Sealed Class Contract

    [Fact]
    public void Contract_AllStoreImplementations_AreSealed()
    {
        var storeTypes = new (Type Type, string Name)[]
        {
            (typeof(ADOSqliteRefTable.ReferenceTableStoreADO), "ADO.Sqlite"),
            (typeof(ADOSqlServerRefTable.ReferenceTableStoreADO), "ADO.SqlServer"),
            (typeof(ADOPostgreSQLRefTable.ReferenceTableStoreADO), "ADO.PostgreSQL"),
            (typeof(ADOMySQLRefTable.ReferenceTableStoreADO), "ADO.MySQL"),
            (typeof(DapperSqliteRefTable.ReferenceTableStoreDapper), "Dapper.Sqlite"),
            (typeof(DapperSqlServerRefTable.ReferenceTableStoreDapper), "Dapper.SqlServer"),
            (typeof(DapperPostgreSQLRefTable.ReferenceTableStoreDapper), "Dapper.PostgreSQL"),
            (typeof(DapperMySQLRefTable.ReferenceTableStoreDapper), "Dapper.MySQL"),
            (typeof(EFCoreRefTable.ReferenceTableStoreEF), "EFCore"),
            (typeof(MongoDBRefTable.ReferenceTableStoreMongoDB), "MongoDB"),
        };

        foreach (var (storeType, providerName) in storeTypes)
        {
            storeType.IsSealed.ShouldBeTrue(
                $"{providerName} {storeType.Name} should be sealed");
        }
    }

    [Fact]
    public void Contract_AllFactoryImplementations_AreSealed()
    {
        var factoryTypes = new (Type Type, string Name)[]
        {
            (typeof(ADOSqliteRefTable.ReferenceTableStoreFactoryADO), "ADO.Sqlite"),
            (typeof(ADOSqlServerRefTable.ReferenceTableStoreFactoryADO), "ADO.SqlServer"),
            (typeof(ADOPostgreSQLRefTable.ReferenceTableStoreFactoryADO), "ADO.PostgreSQL"),
            (typeof(ADOMySQLRefTable.ReferenceTableStoreFactoryADO), "ADO.MySQL"),
            (typeof(DapperSqliteRefTable.ReferenceTableStoreFactoryDapper), "Dapper.Sqlite"),
            (typeof(DapperSqlServerRefTable.ReferenceTableStoreFactoryDapper), "Dapper.SqlServer"),
            (typeof(DapperPostgreSQLRefTable.ReferenceTableStoreFactoryDapper), "Dapper.PostgreSQL"),
            (typeof(DapperMySQLRefTable.ReferenceTableStoreFactoryDapper), "Dapper.MySQL"),
            (typeof(EFCoreRefTable.ReferenceTableStoreFactoryEF<>), "EFCore"),
            (typeof(MongoDBRefTable.ReferenceTableStoreFactoryMongoDB), "MongoDB"),
        };

        foreach (var (factoryType, providerName) in factoryTypes)
        {
            factoryType.IsSealed.ShouldBeTrue(
                $"{providerName} {factoryType.Name} should be sealed");
        }
    }

    #endregion

    #region Naming Convention Contract

    [Fact]
    public void Contract_AllADOProviders_UseReferenceTableStoreADONaming()
    {
        typeof(ADOSqliteRefTable.ReferenceTableStoreADO).Name.ShouldBe("ReferenceTableStoreADO");
        typeof(ADOSqlServerRefTable.ReferenceTableStoreADO).Name.ShouldBe("ReferenceTableStoreADO");
        typeof(ADOPostgreSQLRefTable.ReferenceTableStoreADO).Name.ShouldBe("ReferenceTableStoreADO");
        typeof(ADOMySQLRefTable.ReferenceTableStoreADO).Name.ShouldBe("ReferenceTableStoreADO");
    }

    [Fact]
    public void Contract_AllADOProviders_UseReferenceTableStoreFactoryADONaming()
    {
        typeof(ADOSqliteRefTable.ReferenceTableStoreFactoryADO).Name.ShouldBe("ReferenceTableStoreFactoryADO");
        typeof(ADOSqlServerRefTable.ReferenceTableStoreFactoryADO).Name.ShouldBe("ReferenceTableStoreFactoryADO");
        typeof(ADOPostgreSQLRefTable.ReferenceTableStoreFactoryADO).Name.ShouldBe("ReferenceTableStoreFactoryADO");
        typeof(ADOMySQLRefTable.ReferenceTableStoreFactoryADO).Name.ShouldBe("ReferenceTableStoreFactoryADO");
    }

    [Fact]
    public void Contract_AllDapperProviders_UseReferenceTableStoreDapperNaming()
    {
        typeof(DapperSqliteRefTable.ReferenceTableStoreDapper).Name.ShouldBe("ReferenceTableStoreDapper");
        typeof(DapperSqlServerRefTable.ReferenceTableStoreDapper).Name.ShouldBe("ReferenceTableStoreDapper");
        typeof(DapperPostgreSQLRefTable.ReferenceTableStoreDapper).Name.ShouldBe("ReferenceTableStoreDapper");
        typeof(DapperMySQLRefTable.ReferenceTableStoreDapper).Name.ShouldBe("ReferenceTableStoreDapper");
    }

    [Fact]
    public void Contract_AllDapperProviders_UseReferenceTableStoreFactoryDapperNaming()
    {
        typeof(DapperSqliteRefTable.ReferenceTableStoreFactoryDapper).Name.ShouldBe("ReferenceTableStoreFactoryDapper");
        typeof(DapperSqlServerRefTable.ReferenceTableStoreFactoryDapper).Name.ShouldBe("ReferenceTableStoreFactoryDapper");
        typeof(DapperPostgreSQLRefTable.ReferenceTableStoreFactoryDapper).Name.ShouldBe("ReferenceTableStoreFactoryDapper");
        typeof(DapperMySQLRefTable.ReferenceTableStoreFactoryDapper).Name.ShouldBe("ReferenceTableStoreFactoryDapper");
    }

    [Fact]
    public void Contract_EFCore_UsesConsistentNaming()
    {
        typeof(EFCoreRefTable.ReferenceTableStoreEF).Name.ShouldBe("ReferenceTableStoreEF");
        typeof(EFCoreRefTable.ReferenceTableStoreFactoryEF<>).Name.ShouldBe("ReferenceTableStoreFactoryEF`1");
    }

    [Fact]
    public void Contract_MongoDB_UsesConsistentNaming()
    {
        typeof(MongoDBRefTable.ReferenceTableStoreMongoDB).Name.ShouldBe("ReferenceTableStoreMongoDB");
        typeof(MongoDBRefTable.ReferenceTableStoreFactoryMongoDB).Name.ShouldBe("ReferenceTableStoreFactoryMongoDB");
    }

    #endregion

    #region Namespace Convention Contract

    [Fact]
    public void Contract_AllProviders_UseShardingReferenceTablesNamespace()
    {
        var expectedSuffix = ".Sharding.ReferenceTables";

        var types = new (Type Type, string Provider)[]
        {
            (typeof(ADOSqliteRefTable.ReferenceTableStoreADO), "ADO.Sqlite"),
            (typeof(ADOSqlServerRefTable.ReferenceTableStoreADO), "ADO.SqlServer"),
            (typeof(ADOPostgreSQLRefTable.ReferenceTableStoreADO), "ADO.PostgreSQL"),
            (typeof(ADOMySQLRefTable.ReferenceTableStoreADO), "ADO.MySQL"),
            (typeof(DapperSqliteRefTable.ReferenceTableStoreDapper), "Dapper.Sqlite"),
            (typeof(DapperSqlServerRefTable.ReferenceTableStoreDapper), "Dapper.SqlServer"),
            (typeof(DapperPostgreSQLRefTable.ReferenceTableStoreDapper), "Dapper.PostgreSQL"),
            (typeof(DapperMySQLRefTable.ReferenceTableStoreDapper), "Dapper.MySQL"),
            (typeof(EFCoreRefTable.ReferenceTableStoreEF), "EFCore"),
            (typeof(MongoDBRefTable.ReferenceTableStoreMongoDB), "MongoDB"),
        };

        foreach (var (type, providerName) in types)
        {
            type.Namespace.ShouldNotBeNull($"{providerName} type should have a namespace");
            type.Namespace!.EndsWith(expectedSuffix, StringComparison.Ordinal).ShouldBeTrue(
                $"{providerName} {type.Name} namespace '{type.Namespace}' should end with '{expectedSuffix}'");
        }
    }

    #endregion

    #region Provider Count Verification

    [Fact]
    public void Contract_ThirteenProviders_HaveReferenceTableStoreImplementations()
    {
        // Verify all 13 providers have implementations by counting unique assemblies
        // that contain IReferenceTableStore implementations
        var storeImplementations = new[]
        {
            typeof(ADOSqliteRefTable.ReferenceTableStoreADO),
            typeof(ADOSqlServerRefTable.ReferenceTableStoreADO),
            typeof(ADOPostgreSQLRefTable.ReferenceTableStoreADO),
            typeof(ADOMySQLRefTable.ReferenceTableStoreADO),
            typeof(DapperSqliteRefTable.ReferenceTableStoreDapper),
            typeof(DapperSqlServerRefTable.ReferenceTableStoreDapper),
            typeof(DapperPostgreSQLRefTable.ReferenceTableStoreDapper),
            typeof(DapperMySQLRefTable.ReferenceTableStoreDapper),
            typeof(EFCoreRefTable.ReferenceTableStoreEF),
            typeof(MongoDBRefTable.ReferenceTableStoreMongoDB),
        };

        // 10 distinct types across 10 assemblies (EF Core has 1 shared implementation
        // used by all 4 EF database providers via different DbContext configurations)
        storeImplementations.Length.ShouldBe(10,
            "There should be 10 store implementation types (ADO x4, Dapper x4, EFCore x1, MongoDB x1)");

        // Verify all implement the interface
        foreach (var storeType in storeImplementations)
        {
            StoreInterfaceType.IsAssignableFrom(storeType).ShouldBeTrue(
                $"{storeType.FullName} must implement IReferenceTableStore");
        }
    }

    #endregion

    #region Helper Methods

    private static void VerifyImplementsInterface(Type implementationType, Type interfaceType, string providerName)
    {
        interfaceType.IsAssignableFrom(implementationType).ShouldBeTrue(
            $"{providerName} {implementationType.Name} should implement {interfaceType.Name}");
    }

    private static readonly HashSet<string> LifecycleMethods =
        ["Dispose", "DisposeAsync", "Close", "CloseAsync"];

    private static HashSet<string> GetPublicInstanceMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && !LifecycleMethods.Contains(m.Name))
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);
    }

    #endregion
}
