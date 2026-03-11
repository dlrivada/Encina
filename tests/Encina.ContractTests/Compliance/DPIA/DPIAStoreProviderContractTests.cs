using System.Reflection;

using Encina.Compliance.DPIA;

using ADOSqliteDPIA = Encina.ADO.Sqlite.DPIA;
using ADOSqlServerDPIA = Encina.ADO.SqlServer.DPIA;
using ADOPostgreSQLDPIA = Encina.ADO.PostgreSQL.DPIA;
using ADOMySQLDPIA = Encina.ADO.MySQL.DPIA;
using DapperSqliteDPIA = Encina.Dapper.Sqlite.DPIA;
using DapperSqlServerDPIA = Encina.Dapper.SqlServer.DPIA;
using DapperPostgreSQLDPIA = Encina.Dapper.PostgreSQL.DPIA;
using DapperMySQLDPIA = Encina.Dapper.MySQL.DPIA;
using EFCoreDPIA = Encina.EntityFrameworkCore.DPIA;
using MongoDBDPIA = Encina.MongoDB.DPIA;

namespace Encina.ContractTests.Compliance.DPIA;

/// <summary>
/// Provider consistency contract tests that verify all DPIA store implementations
/// maintain API consistency and follow project conventions.
/// </summary>
[Trait("Category", "Contract")]
public sealed class DPIAStoreProviderContractTests
{
    /// <summary>
    /// All IDPIAStore provider implementation types across all 13 database providers + InMemory.
    /// </summary>
    private static readonly Type[] AllDPIAStoreTypes =
    [
        // ADO.NET providers
        typeof(ADOSqliteDPIA.DPIAStoreADO),
        typeof(ADOSqlServerDPIA.DPIAStoreADO),
        typeof(ADOPostgreSQLDPIA.DPIAStoreADO),
        typeof(ADOMySQLDPIA.DPIAStoreADO),
        // Dapper providers
        typeof(DapperSqliteDPIA.DPIAStoreDapper),
        typeof(DapperSqlServerDPIA.DPIAStoreDapper),
        typeof(DapperPostgreSQLDPIA.DPIAStoreDapper),
        typeof(DapperMySQLDPIA.DPIAStoreDapper),
        // EF Core provider
        typeof(EFCoreDPIA.DPIAStoreEF),
        // MongoDB provider
        typeof(MongoDBDPIA.DPIAStoreMongoDB),
    ];

    /// <summary>
    /// All IDPIAAuditStore provider implementation types.
    /// </summary>
    private static readonly Type[] AllDPIAAuditStoreTypes =
    [
        // ADO.NET providers
        typeof(ADOSqliteDPIA.DPIAAuditStoreADO),
        typeof(ADOSqlServerDPIA.DPIAAuditStoreADO),
        typeof(ADOPostgreSQLDPIA.DPIAAuditStoreADO),
        typeof(ADOMySQLDPIA.DPIAAuditStoreADO),
        // Dapper providers
        typeof(DapperSqliteDPIA.DPIAAuditStoreDapper),
        typeof(DapperSqlServerDPIA.DPIAAuditStoreDapper),
        typeof(DapperPostgreSQLDPIA.DPIAAuditStoreDapper),
        typeof(DapperMySQLDPIA.DPIAAuditStoreDapper),
        // EF Core provider
        typeof(EFCoreDPIA.DPIAAuditStoreEF),
        // MongoDB provider
        typeof(MongoDBDPIA.DPIAAuditStoreMongoDB),
    ];

    #region IDPIAStore Provider Contracts

    [Fact]
    public void Contract_AllDPIAStoreProviders_ImplementIDPIAStore()
    {
        foreach (var providerType in AllDPIAStoreTypes)
        {
            typeof(IDPIAStore).IsAssignableFrom(providerType)
                .ShouldBeTrue($"{providerType.Name} must implement IDPIAStore");
        }
    }

    [Fact]
    public void Contract_AllDPIAStoreProviders_AreSealed()
    {
        foreach (var providerType in AllDPIAStoreTypes)
        {
            providerType.IsSealed
                .ShouldBeTrue($"{providerType.Name} must be sealed");
        }
    }

    [Fact]
    public void Contract_AllDPIAStoreProviders_HaveExpectedMethodSignatures()
    {
        var interfaceMethods = typeof(IDPIAStore)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet();

        foreach (var providerType in AllDPIAStoreTypes)
        {
            var providerMethods = providerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Select(m => m.Name)
                .ToHashSet();

            foreach (var method in interfaceMethods)
            {
                providerMethods.ShouldContain(method,
                    $"{providerType.Name} must implement {method}");
            }
        }
    }

    [Fact]
    public void Contract_DPIAStoreProviders_CoverAll13DatabaseProviders()
    {
        // 10 providers: 4 ADO + 4 Dapper + 1 EF Core + 1 MongoDB
        // Note: EF Core has 1 provider type shared across all databases
        AllDPIAStoreTypes.Length.ShouldBeGreaterThanOrEqualTo(10,
            "Should have at least 10 IDPIAStore providers (4 ADO + 4 Dapper + 1 EF + 1 MongoDB)");
    }

    #endregion

    #region IDPIAAuditStore Provider Contracts

    [Fact]
    public void Contract_AllDPIAAuditStoreProviders_ImplementIDPIAAuditStore()
    {
        foreach (var providerType in AllDPIAAuditStoreTypes)
        {
            typeof(IDPIAAuditStore).IsAssignableFrom(providerType)
                .ShouldBeTrue($"{providerType.Name} must implement IDPIAAuditStore");
        }
    }

    [Fact]
    public void Contract_AllDPIAAuditStoreProviders_AreSealed()
    {
        foreach (var providerType in AllDPIAAuditStoreTypes)
        {
            providerType.IsSealed
                .ShouldBeTrue($"{providerType.Name} must be sealed");
        }
    }

    [Fact]
    public void Contract_AllDPIAAuditStoreProviders_HaveExpectedMethodSignatures()
    {
        var interfaceMethods = typeof(IDPIAAuditStore)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet();

        foreach (var providerType in AllDPIAAuditStoreTypes)
        {
            var providerMethods = providerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Select(m => m.Name)
                .ToHashSet();

            foreach (var method in interfaceMethods)
            {
                providerMethods.ShouldContain(method,
                    $"{providerType.Name} must implement {method}");
            }
        }
    }

    [Fact]
    public void Contract_DPIAAuditStoreProviders_CoverAll13DatabaseProviders()
    {
        AllDPIAAuditStoreTypes.Length.ShouldBeGreaterThanOrEqualTo(10,
            "Should have at least 10 IDPIAAuditStore providers (4 ADO + 4 Dapper + 1 EF + 1 MongoDB)");
    }

    #endregion
}
