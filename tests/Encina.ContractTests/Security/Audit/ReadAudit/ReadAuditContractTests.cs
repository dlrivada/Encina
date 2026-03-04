using System.Reflection;
using Encina.Security.Audit;
using ADOMySQLStore = Encina.ADO.MySQL.Auditing.ReadAuditStoreADO;
using ADOPostgreSQLStore = Encina.ADO.PostgreSQL.Auditing.ReadAuditStoreADO;
using ADOSqliteStore = Encina.ADO.Sqlite.Auditing.ReadAuditStoreADO;
using ADOSqlServerStore = Encina.ADO.SqlServer.Auditing.ReadAuditStoreADO;
using DapperMySQLStore = Encina.Dapper.MySQL.Auditing.ReadAuditStoreDapper;
using DapperPostgreSQLStore = Encina.Dapper.PostgreSQL.Auditing.ReadAuditStoreDapper;
using DapperSqliteStore = Encina.Dapper.Sqlite.Auditing.ReadAuditStoreDapper;
using DapperSqlServerStore = Encina.Dapper.SqlServer.Auditing.ReadAuditStoreDapper;
using EFCoreStore = Encina.EntityFrameworkCore.Auditing.ReadAuditStoreEF;
using MongoDBStore = Encina.MongoDB.Auditing.ReadAuditStoreMongoDB;

namespace Encina.ContractTests.Security.Audit.ReadAudit;

/// <summary>
/// Contract tests for the Encina Read Audit public surface area.
/// Verifies that all 13 provider implementations follow the <see cref="IReadAuditStore"/>
/// contract, interface shapes, data type structures, and error code conventions remain stable.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ReadAuditContractTests
{
    #region Provider Implementation Data

    /// <summary>
    /// All 10 concrete IReadAuditStore implementations (4 ADO + 4 Dapper + 1 EF + 1 MongoDB).
    /// EF Core covers all 4 databases with a single implementation.
    /// </summary>
    private static readonly Type[] AllProviderTypes =
    [
        typeof(ADOSqliteStore),
        typeof(ADOSqlServerStore),
        typeof(ADOPostgreSQLStore),
        typeof(ADOMySQLStore),
        typeof(DapperSqliteStore),
        typeof(DapperSqlServerStore),
        typeof(DapperPostgreSQLStore),
        typeof(DapperMySQLStore),
        typeof(EFCoreStore),
        typeof(MongoDBStore),
        typeof(InMemoryReadAuditStore)
    ];

    public static TheoryData<Type> ProviderTypes
    {
        get
        {
            var data = new TheoryData<Type>();
            foreach (var type in AllProviderTypes)
            {
                data.Add(type);
            }

            return data;
        }
    }

    #endregion

    #region IReadAuditStore Interface Shape

    [Fact]
    public void IReadAuditStore_ShouldDefine_FiveMethods()
    {
        // Arrange
        var type = typeof(IReadAuditStore);

        // Act
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        methods.Length.ShouldBe(5,
            "IReadAuditStore must define exactly 5 methods: " +
            "LogReadAsync, GetAccessHistoryAsync, GetUserAccessHistoryAsync, QueryAsync, PurgeEntriesAsync");

        var methodNames = methods.Select(m => m.Name).ToHashSet(StringComparer.Ordinal);
        methodNames.ShouldContain("LogReadAsync");
        methodNames.ShouldContain("GetAccessHistoryAsync");
        methodNames.ShouldContain("GetUserAccessHistoryAsync");
        methodNames.ShouldContain("QueryAsync");
        methodNames.ShouldContain("PurgeEntriesAsync");
    }

    [Fact]
    public void IReadAuditStore_AllMethods_ShouldReturn_ValueTaskEither()
    {
        // Arrange
        var type = typeof(IReadAuditStore);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Act & Assert
        foreach (var method in methods)
        {
            var returnType = method.ReturnType;

            returnType.IsGenericType.ShouldBeTrue(
                $"Method '{method.Name}' must return a generic type (ValueTask<Either<...>>)");

            var outerGenericDef = returnType.GetGenericTypeDefinition();
            outerGenericDef.ShouldBe(typeof(ValueTask<>),
                $"Method '{method.Name}' must return ValueTask<T>, got {returnType.Name}");

            var innerType = returnType.GetGenericArguments()[0];
            innerType.IsGenericType.ShouldBeTrue(
                $"Method '{method.Name}' inner type must be generic (Either<EncinaError, T>)");

            var fullName = innerType.GetGenericTypeDefinition().FullName ?? string.Empty;
            (fullName.StartsWith("LanguageExt.Either", StringComparison.Ordinal)).ShouldBeTrue(
                $"Method '{method.Name}' must return ValueTask<Either<EncinaError, T>>, got {innerType.Name}");

            var eitherArgs = innerType.GetGenericArguments();
            eitherArgs[0].ShouldBe(typeof(EncinaError),
                $"Method '{method.Name}' Either left type must be EncinaError");
        }
    }

    [Fact]
    public void LogReadAsync_ShouldAccept_EntryAndCancellationToken()
    {
        // Arrange
        var method = typeof(IReadAuditStore).GetMethod("LogReadAsync");

        // Assert
        method.ShouldNotBeNull("LogReadAsync must exist on IReadAuditStore");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2, "LogReadAsync must accept (ReadAuditEntry entry, CancellationToken ct)");

        parameters[0].ParameterType.ShouldBe(typeof(ReadAuditEntry));
        parameters[0].Name.ShouldBe("entry");

        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[1].HasDefaultValue.ShouldBeTrue("CancellationToken should have a default value");
    }

    [Fact]
    public void GetAccessHistoryAsync_ShouldAccept_EntityTypeEntityIdAndCancellationToken()
    {
        // Arrange
        var method = typeof(IReadAuditStore).GetMethod("GetAccessHistoryAsync");

        // Assert
        method.ShouldNotBeNull("GetAccessHistoryAsync must exist on IReadAuditStore");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3,
            "GetAccessHistoryAsync must accept (string entityType, string entityId, CancellationToken ct)");

        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[0].Name.ShouldBe("entityType");

        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[1].Name.ShouldBe("entityId");

        parameters[2].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[2].HasDefaultValue.ShouldBeTrue();
    }

    [Fact]
    public void GetUserAccessHistoryAsync_ShouldAccept_UserIdDateRangeAndCancellationToken()
    {
        // Arrange
        var method = typeof(IReadAuditStore).GetMethod("GetUserAccessHistoryAsync");

        // Assert
        method.ShouldNotBeNull("GetUserAccessHistoryAsync must exist on IReadAuditStore");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(4,
            "GetUserAccessHistoryAsync must accept (string userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)");

        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[0].Name.ShouldBe("userId");

        parameters[1].ParameterType.ShouldBe(typeof(DateTimeOffset));
        parameters[1].Name.ShouldBe("fromUtc");

        parameters[2].ParameterType.ShouldBe(typeof(DateTimeOffset));
        parameters[2].Name.ShouldBe("toUtc");

        parameters[3].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[3].HasDefaultValue.ShouldBeTrue();
    }

    [Fact]
    public void QueryAsync_ShouldAccept_QueryAndCancellationToken()
    {
        // Arrange
        var method = typeof(IReadAuditStore).GetMethod("QueryAsync");

        // Assert
        method.ShouldNotBeNull("QueryAsync must exist on IReadAuditStore");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2, "QueryAsync must accept (ReadAuditQuery query, CancellationToken ct)");

        parameters[0].ParameterType.ShouldBe(typeof(ReadAuditQuery));
        parameters[0].Name.ShouldBe("query");

        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[1].HasDefaultValue.ShouldBeTrue();
    }

    [Fact]
    public void PurgeEntriesAsync_ShouldAccept_DateTimeOffsetAndCancellationToken()
    {
        // Arrange
        var method = typeof(IReadAuditStore).GetMethod("PurgeEntriesAsync");

        // Assert
        method.ShouldNotBeNull("PurgeEntriesAsync must exist on IReadAuditStore");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2,
            "PurgeEntriesAsync must accept (DateTimeOffset olderThanUtc, CancellationToken ct)");

        parameters[0].ParameterType.ShouldBe(typeof(DateTimeOffset));
        parameters[0].Name.ShouldBe("olderThanUtc");

        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[1].HasDefaultValue.ShouldBeTrue();
    }

    #endregion

    #region All Providers Implement IReadAuditStore

    [Theory]
    [MemberData(nameof(ProviderTypes))]
    public void Provider_ShouldImplement_IReadAuditStore(Type providerType)
    {
        // Act
        var implements = typeof(IReadAuditStore).IsAssignableFrom(providerType);

        // Assert
        implements.ShouldBeTrue(
            $"{providerType.Name} must implement IReadAuditStore");
    }

    [Theory]
    [MemberData(nameof(ProviderTypes))]
    public void Provider_ShouldBeSealed(Type providerType)
    {
        // Assert
        providerType.IsSealed.ShouldBeTrue(
            $"{providerType.Name} must be sealed (security requirement for store implementations)");
    }

    [Theory]
    [MemberData(nameof(ProviderTypes))]
    public void Provider_ShouldHaveAllInterfaceMethods(Type providerType)
    {
        // Arrange
        var interfaceMethods = typeof(IReadAuditStore)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        // Act
        var providerMethods = providerType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        // Assert
        foreach (var requiredMethod in interfaceMethods)
        {
            providerMethods.ShouldContain(requiredMethod,
                $"{providerType.Name} must implement {requiredMethod}");
        }
    }

    #endregion

    #region ReadAuditEntry Record Contract

    [Fact]
    public void ReadAuditEntry_ShouldBeSealed()
    {
        typeof(ReadAuditEntry).IsSealed.ShouldBeTrue("ReadAuditEntry must be sealed");
    }

    [Fact]
    public void ReadAuditEntry_ShouldHave_ElevenProperties()
    {
        // Arrange
        var type = typeof(ReadAuditEntry);

        // Act
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        properties.Length.ShouldBe(11,
            "ReadAuditEntry must define exactly 11 properties: " +
            "Id, EntityType, EntityId, UserId, TenantId, AccessedAtUtc, " +
            "CorrelationId, Purpose, AccessMethod, EntityCount, Metadata");

        var propertyNames = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        propertyNames.ShouldContain("Id");
        propertyNames.ShouldContain("EntityType");
        propertyNames.ShouldContain("EntityId");
        propertyNames.ShouldContain("AccessedAtUtc");
        propertyNames.ShouldContain("AccessMethod");
        propertyNames.ShouldContain("EntityCount");
    }

    [Fact]
    public void ReadAuditEntry_Id_ShouldBe_Guid()
    {
        var prop = typeof(ReadAuditEntry).GetProperty("Id");
        prop.ShouldNotBeNull();
        prop!.PropertyType.ShouldBe(typeof(Guid));
    }

    [Fact]
    public void ReadAuditEntry_AccessMethod_ShouldBe_ReadAccessMethod()
    {
        var prop = typeof(ReadAuditEntry).GetProperty("AccessMethod");
        prop.ShouldNotBeNull();
        prop!.PropertyType.ShouldBe(typeof(ReadAccessMethod));
    }

    [Fact]
    public void ReadAuditEntry_AccessedAtUtc_ShouldBe_DateTimeOffset()
    {
        var prop = typeof(ReadAuditEntry).GetProperty("AccessedAtUtc");
        prop.ShouldNotBeNull();
        prop!.PropertyType.ShouldBe(typeof(DateTimeOffset));
    }

    #endregion

    #region ReadAccessMethod Enum Contract

    [Fact]
    public void ReadAccessMethod_ShouldDefine_FiveValues()
    {
        // Act
        var values = Enum.GetValues<ReadAccessMethod>();

        // Assert
        values.Length.ShouldBe(5,
            "ReadAccessMethod must define exactly 5 values: " +
            "Repository, DirectQuery, Api, Export, Custom");
    }

    [Theory]
    [InlineData(ReadAccessMethod.Repository, 0)]
    [InlineData(ReadAccessMethod.DirectQuery, 1)]
    [InlineData(ReadAccessMethod.Api, 2)]
    [InlineData(ReadAccessMethod.Export, 3)]
    [InlineData(ReadAccessMethod.Custom, 4)]
    public void ReadAccessMethod_ShouldHaveExpectedValue(ReadAccessMethod method, int expectedValue)
    {
        ((int)method).ShouldBe(expectedValue,
            $"ReadAccessMethod.{method} must have underlying value {expectedValue}");
    }

    #endregion

    #region ReadAuditErrors Contract

    [Fact]
    public void ReadAuditErrors_ShouldDefine_FiveErrorCodes()
    {
        // Arrange
        var type = typeof(ReadAuditErrors);

        // Act
        var constants = type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToList();

        // Assert
        constants.Count.ShouldBe(5,
            "ReadAuditErrors must define exactly 5 error code constants");
    }

    [Fact]
    public void AllErrorCodes_ShouldStartWith_ReadAuditPrefix()
    {
        // Arrange
        const string expectedPrefix = "read_audit.";
        var type = typeof(ReadAuditErrors);

        var constants = type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToList();

        // Assert
        constants.ShouldNotBeEmpty("ReadAuditErrors must define at least one error code constant");

        foreach (var constant in constants)
        {
            var value = (string?)constant.GetRawConstantValue();
            value.ShouldNotBeNull($"Error code constant '{constant.Name}' must not be null");
            (value!.StartsWith(expectedPrefix, StringComparison.Ordinal)).ShouldBeTrue(
                $"Error code '{constant.Name}' = '{value}' must start with '{expectedPrefix}'");
        }
    }

    #endregion

    #region ReadAuditQuery Contract

    [Fact]
    public void ReadAuditQuery_ShouldBeSealed()
    {
        typeof(ReadAuditQuery).IsSealed.ShouldBeTrue("ReadAuditQuery must be sealed");
    }

    [Fact]
    public void ReadAuditQuery_ShouldHave_BuilderStaticMethod()
    {
        // Arrange
        var method = typeof(ReadAuditQuery).GetMethod("Builder",
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        method.ShouldNotBeNull("ReadAuditQuery must have a static Builder() method");
        method!.ReturnType.Name.ShouldContain("ReadAuditQueryBuilder");
    }

    [Fact]
    public void ReadAuditQuery_DefaultPageSize_ShouldBe_50()
    {
        ReadAuditQuery.DefaultPageSize.ShouldBe(50);
    }

    [Fact]
    public void ReadAuditQuery_MaxPageSize_ShouldBe_1000()
    {
        ReadAuditQuery.MaxPageSize.ShouldBe(1000);
    }

    #endregion

    #region ReadAuditOptions Contract

    [Fact]
    public void ReadAuditOptions_ShouldBeSealed()
    {
        typeof(ReadAuditOptions).IsSealed.ShouldBeTrue("ReadAuditOptions must be sealed");
    }

    [Fact]
    public void ReadAuditOptions_Defaults_ShouldBeStable()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Assert
        options.Enabled.ShouldBeTrue("Default Enabled must be true");
        options.ExcludeSystemAccess.ShouldBeFalse("Default ExcludeSystemAccess must be false");
        options.RequirePurpose.ShouldBeFalse("Default RequirePurpose must be false");
        options.BatchSize.ShouldBe(1, "Default BatchSize must be 1");
        options.RetentionDays.ShouldBe(365, "Default RetentionDays must be 365");
        options.EnableAutoPurge.ShouldBeFalse("Default EnableAutoPurge must be false");
        options.PurgeIntervalHours.ShouldBe(24, "Default PurgeIntervalHours must be 24");
    }

    [Fact]
    public void ReadAuditOptions_ShouldHave_AuditReadsForGenericMethod()
    {
        // Arrange
        var methods = typeof(ReadAuditOptions)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "AuditReadsFor")
            .ToList();

        // Assert
        methods.Count.ShouldBe(2,
            "ReadAuditOptions must have exactly 2 AuditReadsFor overloads " +
            "(default rate and explicit rate)");

        methods.ShouldContain(m => m.IsGenericMethod && m.GetParameters().Length == 0,
            "Must have parameterless generic AuditReadsFor<T>()");
        methods.ShouldContain(m => m.IsGenericMethod && m.GetParameters().Length == 1,
            "Must have AuditReadsFor<T>(double samplingRate)");
    }

    #endregion

    #region IReadAuditContext Interface Shape

    [Fact]
    public void IReadAuditContext_ShouldHave_PurposeProperty()
    {
        // Arrange
        var type = typeof(IReadAuditContext);

        // Act
        var property = type.GetProperty("Purpose", BindingFlags.Public | BindingFlags.Instance);

        // Assert
        property.ShouldNotBeNull("IReadAuditContext must have a Purpose property");
        property!.PropertyType.ShouldBe(typeof(string),
            "Purpose property must be of type string (nullable)");
    }

    [Fact]
    public void IReadAuditContext_ShouldHave_WithPurposeMethod()
    {
        // Arrange
        var method = typeof(IReadAuditContext).GetMethod("WithPurpose",
            BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.ShouldNotBeNull("IReadAuditContext must have a WithPurpose method");
        method!.ReturnType.ShouldBe(typeof(IReadAuditContext),
            "WithPurpose must return IReadAuditContext for fluent chaining");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1, "WithPurpose must accept exactly one string parameter");
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[0].Name.ShouldBe("purpose");
    }

    #endregion

    #region InMemoryReadAuditStore Contract

    [Fact]
    public void InMemoryReadAuditStore_ShouldBeSealed()
    {
        typeof(InMemoryReadAuditStore).IsSealed.ShouldBeTrue(
            "InMemoryReadAuditStore must be sealed");
    }

    [Fact]
    public void InMemoryReadAuditStore_ShouldHave_TestHelpers()
    {
        // Arrange
        var type = typeof(InMemoryReadAuditStore);
        var publicMembers = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var memberNames = publicMembers.Select(m => m.Name).ToHashSet(StringComparer.Ordinal);

        // Assert — test helpers for unit testing
        memberNames.ShouldContain("GetAllEntries",
            "InMemoryReadAuditStore must expose GetAllEntries() test helper");
        memberNames.ShouldContain("Clear",
            "InMemoryReadAuditStore must expose Clear() test helper");
        memberNames.ShouldContain("Count",
            "InMemoryReadAuditStore must expose Count property");
    }

    #endregion

    #region PagedResult<T> Contract

    [Fact]
    public void PagedResult_ShouldHave_CoreProperties()
    {
        // Arrange
        var type = typeof(PagedResult<ReadAuditEntry>);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var propertyNames = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        // Assert
        propertyNames.ShouldContain("Items");
        propertyNames.ShouldContain("TotalCount");
        propertyNames.ShouldContain("PageNumber");
        propertyNames.ShouldContain("PageSize");
        propertyNames.ShouldContain("TotalPages");
        propertyNames.ShouldContain("HasPreviousPage");
        propertyNames.ShouldContain("HasNextPage");
    }

    #endregion
}
