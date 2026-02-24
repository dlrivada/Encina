using System.Reflection;
using Encina.Compliance.GDPR;
using ADOMySQLPA = Encina.ADO.MySQL.ProcessingActivity;
using ADOPostgreSQLPA = Encina.ADO.PostgreSQL.ProcessingActivity;
using ADOSqlitePA = Encina.ADO.Sqlite.ProcessingActivity;
using ADOSqlServerPA = Encina.ADO.SqlServer.ProcessingActivity;
using DapperMySQLPA = Encina.Dapper.MySQL.ProcessingActivity;
using DapperPostgreSQLPA = Encina.Dapper.PostgreSQL.ProcessingActivity;
using DapperSqlitePA = Encina.Dapper.Sqlite.ProcessingActivity;
using DapperSqlServerPA = Encina.Dapper.SqlServer.ProcessingActivity;
using EFCorePA = Encina.EntityFrameworkCore.ProcessingActivity;
using MongoDBPA = Encina.MongoDB.ProcessingActivity;

namespace Encina.ContractTests.Compliance.GDPR;

/// <summary>
/// Contract tests verifying that all 13 database provider implementations of <see cref="IProcessingActivityRegistry"/>
/// have consistent type signatures and implement the interface correctly.
/// </summary>
/// <remarks>
/// These tests use Reflection to verify API consistency across all providers without requiring
/// database infrastructure. Behavioral verification for database providers belongs in integration tests.
/// </remarks>
[Trait("Category", "Contract")]
public sealed class ProcessingActivityRegistryProviderContractTests
{
    /// <summary>
    /// All 13 provider types that implement IProcessingActivityRegistry.
    /// </summary>
    private static readonly Type[] AllProviderTypes =
    [
        // ADO.NET (4)
        typeof(ADOSqlitePA.ProcessingActivityRegistryADO),
        typeof(ADOSqlServerPA.ProcessingActivityRegistryADO),
        typeof(ADOPostgreSQLPA.ProcessingActivityRegistryADO),
        typeof(ADOMySQLPA.ProcessingActivityRegistryADO),
        // Dapper (4)
        typeof(DapperSqlitePA.ProcessingActivityRegistryDapper),
        typeof(DapperSqlServerPA.ProcessingActivityRegistryDapper),
        typeof(DapperPostgreSQLPA.ProcessingActivityRegistryDapper),
        typeof(DapperMySQLPA.ProcessingActivityRegistryDapper),
        // EF Core (1 shared)
        typeof(EFCorePA.ProcessingActivityRegistryEF),
        // MongoDB (1)
        typeof(MongoDBPA.ProcessingActivityRegistryMongoDB),
    ];

    /// <summary>
    /// Contract: All providers must implement IProcessingActivityRegistry.
    /// </summary>
    [Fact]
    public void Contract_AllProviders_ImplementIProcessingActivityRegistry()
    {
        foreach (var providerType in AllProviderTypes)
        {
            typeof(IProcessingActivityRegistry).IsAssignableFrom(providerType)
                .ShouldBeTrue($"{providerType.FullName} must implement IProcessingActivityRegistry");
        }
    }

    /// <summary>
    /// Contract: All ADO.NET providers must have consistent public method signatures.
    /// </summary>
    [Fact]
    public void Contract_AllADOProviders_HaveConsistentPublicMethods()
    {
        var adoTypes = new[]
        {
            typeof(ADOSqlitePA.ProcessingActivityRegistryADO),
            typeof(ADOSqlServerPA.ProcessingActivityRegistryADO),
            typeof(ADOPostgreSQLPA.ProcessingActivityRegistryADO),
            typeof(ADOMySQLPA.ProcessingActivityRegistryADO),
        };

        VerifyMethodConsistency(adoTypes, "ADO.NET");
    }

    /// <summary>
    /// Contract: All Dapper providers must have consistent public method signatures.
    /// </summary>
    [Fact]
    public void Contract_AllDapperProviders_HaveConsistentPublicMethods()
    {
        var dapperTypes = new[]
        {
            typeof(DapperSqlitePA.ProcessingActivityRegistryDapper),
            typeof(DapperSqlServerPA.ProcessingActivityRegistryDapper),
            typeof(DapperPostgreSQLPA.ProcessingActivityRegistryDapper),
            typeof(DapperMySQLPA.ProcessingActivityRegistryDapper),
        };

        VerifyMethodConsistency(dapperTypes, "Dapper");
    }

    /// <summary>
    /// Contract: All providers must be sealed classes.
    /// </summary>
    [Fact]
    public void Contract_AllProviders_AreSealed()
    {
        foreach (var providerType in AllProviderTypes)
        {
            providerType.IsSealed
                .ShouldBeTrue($"{providerType.Name} must be sealed for performance");
        }
    }

    /// <summary>
    /// Contract: All IProcessingActivityRegistry interface methods must exist on every provider.
    /// </summary>
    [Fact]
    public void Contract_AllProviders_HaveAllInterfaceMethods()
    {
        var interfaceMethods = typeof(IProcessingActivityRegistry)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet();

        foreach (var providerType in AllProviderTypes)
        {
            var providerMethods = providerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(m => m.Name)
                .ToHashSet();

            foreach (var method in interfaceMethods)
            {
                providerMethods.ShouldContain(method,
                    $"{providerType.Name} is missing IProcessingActivityRegistry method '{method}'");
            }
        }
    }

    /// <summary>
    /// Contract: All ADO.NET and Dapper providers must have a constructor accepting a connection string parameter.
    /// </summary>
    [Fact]
    public void Contract_ADOAndDapperProviders_HaveConnectionStringConstructor()
    {
        var providerTypes = new[]
        {
            typeof(ADOSqlitePA.ProcessingActivityRegistryADO),
            typeof(ADOSqlServerPA.ProcessingActivityRegistryADO),
            typeof(ADOPostgreSQLPA.ProcessingActivityRegistryADO),
            typeof(ADOMySQLPA.ProcessingActivityRegistryADO),
            typeof(DapperSqlitePA.ProcessingActivityRegistryDapper),
            typeof(DapperSqlServerPA.ProcessingActivityRegistryDapper),
            typeof(DapperPostgreSQLPA.ProcessingActivityRegistryDapper),
            typeof(DapperMySQLPA.ProcessingActivityRegistryDapper),
        };

        foreach (var providerType in providerTypes)
        {
            var constructor = providerType.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                [typeof(string)],
                null);

            constructor.ShouldNotBeNull(
                $"{providerType.Name} must have a public constructor accepting a string (connectionString) parameter");
        }
    }

    /// <summary>
    /// Contract: EF Core provider must have a constructor accepting a DbContext parameter.
    /// </summary>
    [Fact]
    public void Contract_EFCoreProvider_HasDbContextConstructor()
    {
        var efCoreType = typeof(EFCorePA.ProcessingActivityRegistryEF);
        var constructors = efCoreType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        constructors.Length.ShouldBeGreaterThan(0,
            $"{efCoreType.Name} must have at least one public constructor");

        var hasDbContextParam = constructors.Any(c =>
            c.GetParameters().Any(p => typeof(Microsoft.EntityFrameworkCore.DbContext).IsAssignableFrom(p.ParameterType)));

        hasDbContextParam.ShouldBeTrue(
            $"{efCoreType.Name} must have a constructor accepting a DbContext parameter");
    }

    /// <summary>
    /// Contract: MongoDB provider must have a constructor accepting connectionString and databaseName parameters.
    /// </summary>
    [Fact]
    public void Contract_MongoDBProvider_HasConnectionStringAndDatabaseNameConstructor()
    {
        var mongoType = typeof(MongoDBPA.ProcessingActivityRegistryMongoDB);
        var constructors = mongoType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        constructors.Length.ShouldBeGreaterThan(0,
            $"{mongoType.Name} must have at least one public constructor");

        var hasTwoStringParams = constructors.Any(c =>
        {
            var parameters = c.GetParameters();
            return parameters.Length >= 2
                && parameters[0].ParameterType == typeof(string)
                && parameters[1].ParameterType == typeof(string);
        });

        hasTwoStringParams.ShouldBeTrue(
            $"{mongoType.Name} must have a constructor accepting (string connectionString, string databaseName)");
    }

    #region Helpers

    private static void VerifyMethodConsistency(Type[] types, string category)
    {
        if (types.Length < 2) return;

        var referenceMethods = GetPublicMethodSignatures(types[0]);

        for (var i = 1; i < types.Length; i++)
        {
            var currentMethods = GetPublicMethodSignatures(types[i]);
            referenceMethods.ShouldBe(currentMethods,
                $"All {category} providers should have identical public method signatures. " +
                $"Mismatch between {types[0].Name} and {types[i].Name}");
        }
    }

    private static SortedSet<string> GetPublicMethodSignatures(Type type)
    {
        var methods = type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName) // Exclude property accessors
            .Select(m =>
            {
                var parameters = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name));
                return $"{m.ReturnType.Name} {m.Name}({parameters})";
            });

        return new SortedSet<string>(methods);
    }

    #endregion
}
