using System.Reflection;

using Encina.Compliance.Consent;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using ADOSqliteConsent = Encina.ADO.Sqlite.Consent;
using ADOSqlServerConsent = Encina.ADO.SqlServer.Consent;
using ADOPostgreSQLConsent = Encina.ADO.PostgreSQL.Consent;
using ADOMySQLConsent = Encina.ADO.MySQL.Consent;
using DapperSqliteConsent = Encina.Dapper.Sqlite.Consent;
using DapperSqlServerConsent = Encina.Dapper.SqlServer.Consent;
using DapperPostgreSQLConsent = Encina.Dapper.PostgreSQL.Consent;
using DapperMySQLConsent = Encina.Dapper.MySQL.Consent;
using EFCoreConsent = Encina.EntityFrameworkCore.Consent;
using MongoDBConsent = Encina.MongoDB.Consent;

namespace Encina.ContractTests.Compliance.Consent;

#region Abstract Base Class

/// <summary>
/// Abstract base class defining behavioral contracts that ALL <see cref="IConsentStore"/>
/// implementations must satisfy.
/// </summary>
/// <remarks>
/// <para>
/// Each concrete subclass provides a store instance via <see cref="CreateStore"/>.
/// The behavioral tests verify the IConsentStore contract independent of the backing store.
/// </para>
/// <para>
/// For database-backed providers (ADO, Dapper, EF Core, MongoDB), concrete implementations
/// belong in integration tests where real databases are available. The InMemory implementation
/// runs without infrastructure and serves as the reference contract verification.
/// </para>
/// </remarks>
[Trait("Category", "Contract")]
public abstract class ConsentStoreContractTestsBase
{
    private static readonly string[] ExpectedPurposes = ["analytics", "marketing"];

    /// <summary>
    /// Creates a fresh <see cref="IConsentStore"/> instance for each test.
    /// </summary>
    protected abstract IConsentStore CreateStore();

    #region Record → Get Contract

    /// <summary>
    /// Contract: Recording consent then retrieving it must return the same record.
    /// </summary>
    [Fact]
    public async Task Contract_RecordConsent_ThenGetConsent_ReturnsSameRecord()
    {
        // Arrange
        var store = CreateStore();
        var consent = CreateActiveConsent("user-1", "marketing");

        // Act
        var recordResult = await store.RecordConsentAsync(consent);
        var getResult = await store.GetConsentAsync(consent.SubjectId, consent.Purpose);

        // Assert
        recordResult.IsRight.ShouldBeTrue("RecordConsentAsync should succeed");
        getResult.IsRight.ShouldBeTrue("GetConsentAsync should succeed");

        var opt = (Option<ConsentRecord>)getResult;
        opt.IsSome.ShouldBeTrue("Should return a consent record");

        var retrieved = (ConsentRecord)opt;
        retrieved.SubjectId.ShouldBe(consent.SubjectId);
        retrieved.Purpose.ShouldBe(consent.Purpose);
        retrieved.Status.ShouldBe(ConsentStatus.Active);
        retrieved.ConsentVersionId.ShouldBe(consent.ConsentVersionId);
        retrieved.Source.ShouldBe(consent.Source);
    }

    /// <summary>
    /// Contract: Getting consent for a non-existent subject must return None.
    /// </summary>
    [Fact]
    public async Task Contract_GetConsent_NonExistent_ReturnsNone()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetConsentAsync("non-existent-user", "marketing");

        // Assert
        result.IsRight.ShouldBeTrue("GetConsentAsync should succeed even when not found");

        var opt = (Option<ConsentRecord>)result;
        opt.IsNone.ShouldBeTrue("Should return None for non-existent consent");
    }

    /// <summary>
    /// Contract: Recording consent with same SubjectId+Purpose overwrites the previous record.
    /// </summary>
    [Fact]
    public async Task Contract_RecordConsent_SameKey_OverwritesPrevious()
    {
        // Arrange
        var store = CreateStore();
        var v1 = CreateActiveConsent("user-1", "marketing", versionId: "v1");
        var v2 = CreateActiveConsent("user-1", "marketing", versionId: "v2");

        // Act
        await store.RecordConsentAsync(v1);
        await store.RecordConsentAsync(v2);
        var result = await store.GetConsentAsync("user-1", "marketing");

        // Assert
        var retrieved = (ConsentRecord)(Option<ConsentRecord>)result;
        retrieved.ConsentVersionId.ShouldBe("v2");
    }

    #endregion

    #region Withdraw → HasValid Contract

    /// <summary>
    /// Contract: Withdrawing consent must cause HasValidConsent to return false.
    /// </summary>
    [Fact]
    public async Task Contract_WithdrawConsent_ThenHasValidConsent_ReturnsFalse()
    {
        // Arrange
        var store = CreateStore();
        var consent = CreateActiveConsent("user-1", "marketing");
        await store.RecordConsentAsync(consent);

        // Act
        var withdrawResult = await store.WithdrawConsentAsync("user-1", "marketing");
        var hasValidResult = await store.HasValidConsentAsync("user-1", "marketing");

        // Assert
        withdrawResult.IsRight.ShouldBeTrue("WithdrawConsentAsync should succeed");
        hasValidResult.IsRight.ShouldBeTrue("HasValidConsentAsync should succeed");
        ((bool)hasValidResult).ShouldBeFalse("Withdrawn consent must not be valid");
    }

    /// <summary>
    /// Contract: Active consent must have HasValidConsent return true.
    /// </summary>
    [Fact]
    public async Task Contract_ActiveConsent_HasValidConsent_ReturnsTrue()
    {
        // Arrange
        var store = CreateStore();
        var consent = CreateActiveConsent("user-1", "analytics");
        await store.RecordConsentAsync(consent);

        // Act
        var result = await store.HasValidConsentAsync("user-1", "analytics");

        // Assert
        result.IsRight.ShouldBeTrue();
        ((bool)result).ShouldBeTrue("Active consent must be valid");
    }

    /// <summary>
    /// Contract: Withdrawing non-existent consent must return an error.
    /// </summary>
    [Fact]
    public async Task Contract_WithdrawConsent_NonExistent_ReturnsError()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.WithdrawConsentAsync("non-existent", "marketing");

        // Assert
        result.IsLeft.ShouldBeTrue("Withdrawing non-existent consent should return an error");
    }

    #endregion

    #region Expiration Contract

    /// <summary>
    /// Contract: Expired consent must have HasValidConsent return false.
    /// </summary>
    [Fact]
    public async Task Contract_ExpiredConsent_HasValidConsent_ReturnsFalse()
    {
        // Arrange
        var store = CreateStore();
        var consent = CreateExpiredConsent("user-1", "marketing");
        await store.RecordConsentAsync(consent);

        // Act
        var result = await store.HasValidConsentAsync("user-1", "marketing");

        // Assert
        result.IsRight.ShouldBeTrue();
        ((bool)result).ShouldBeFalse("Expired consent must not be valid");
    }

    #endregion

    #region GetAllConsents Contract

    /// <summary>
    /// Contract: GetAllConsents returns all consents for a subject across purposes.
    /// </summary>
    [Fact]
    public async Task Contract_GetAllConsents_ReturnsAllPurposes()
    {
        // Arrange
        var store = CreateStore();
        var marketing = CreateActiveConsent("user-1", "marketing");
        var analytics = CreateActiveConsent("user-1", "analytics");
        await store.RecordConsentAsync(marketing);
        await store.RecordConsentAsync(analytics);

        // Act
        var result = await store.GetAllConsentsAsync("user-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        var consents = result.Match<IReadOnlyList<ConsentRecord>>(
            Right: r => r,
            Left: _ => []);
        consents.Count.ShouldBe(2);
        consents.Select(c => c.Purpose).ShouldBe(ExpectedPurposes, ignoreOrder: true);
    }

    /// <summary>
    /// Contract: GetAllConsents for non-existent subject returns empty list.
    /// </summary>
    [Fact]
    public async Task Contract_GetAllConsents_NonExistent_ReturnsEmptyList()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetAllConsentsAsync("non-existent");

        // Assert
        result.IsRight.ShouldBeTrue();
        var consents = result.Match<IReadOnlyList<ConsentRecord>>(
            Right: r => r,
            Left: _ => []);
        consents.Count.ShouldBe(0);
    }

    #endregion

    #region Bulk Operations Contract

    /// <summary>
    /// Contract: BulkRecordConsent with N records results in SuccessCount == N.
    /// </summary>
    [Fact]
    public async Task Contract_BulkRecord_AllSucceed()
    {
        // Arrange
        var store = CreateStore();
        var consents = new[]
        {
            CreateActiveConsent("user-1", "marketing"),
            CreateActiveConsent("user-2", "analytics"),
            CreateActiveConsent("user-3", "personalization")
        };

        // Act
        var result = await store.BulkRecordConsentAsync(consents);

        // Assert
        result.IsRight.ShouldBeTrue();
        var bulkResult = (BulkOperationResult)result;
        bulkResult.SuccessCount.ShouldBe(3);
        bulkResult.AllSucceeded.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: BulkWithdrawConsent withdraws only existing consents.
    /// </summary>
    [Fact]
    public async Task Contract_BulkWithdraw_MixedResults()
    {
        // Arrange
        var store = CreateStore();
        await store.RecordConsentAsync(CreateActiveConsent("user-1", "marketing"));
        await store.RecordConsentAsync(CreateActiveConsent("user-1", "analytics"));

        // Act - withdraw 3 purposes, only 2 exist
        var result = await store.BulkWithdrawConsentAsync("user-1", ["marketing", "analytics", "nonexistent"]);

        // Assert
        result.IsRight.ShouldBeTrue();
        var bulkResult = (BulkOperationResult)result;
        bulkResult.SuccessCount.ShouldBe(2);
        bulkResult.AllSucceeded.ShouldBeFalse();
        bulkResult.Errors.Count.ShouldBe(1);
    }

    #endregion

    #region Helpers

    protected static ConsentRecord CreateActiveConsent(
        string subjectId,
        string purpose,
        string versionId = "v1",
        string source = "test")
    {
        return new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            Purpose = purpose,
            Status = ConsentStatus.Active,
            ConsentVersionId = versionId,
            GivenAtUtc = DateTimeOffset.UtcNow,
            Source = source,
            Metadata = new Dictionary<string, object?>()
        };
    }

    protected static ConsentRecord CreateExpiredConsent(string subjectId, string purpose)
    {
        return new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            Purpose = purpose,
            Status = ConsentStatus.Expired,
            ConsentVersionId = "v1",
            GivenAtUtc = DateTimeOffset.UtcNow.AddDays(-30),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            Source = "test",
            Metadata = new Dictionary<string, object?>()
        };
    }

    #endregion
}

#endregion

#region InMemory Concrete Implementation

/// <summary>
/// Concrete contract tests for <see cref="InMemoryConsentStore"/>.
/// Runs without infrastructure — the reference implementation for contract verification.
/// </summary>
[Trait("Category", "Contract")]
public sealed class InMemoryConsentStoreContractTests : ConsentStoreContractTestsBase
{
    protected override IConsentStore CreateStore()
    {
        return new InMemoryConsentStore(
            TimeProvider.System,
            NullLogger<InMemoryConsentStore>.Instance);
    }
}

#endregion

#region Provider Type Consistency Contracts

/// <summary>
/// Contract tests verifying that all 13 database provider implementations of <see cref="IConsentStore"/>
/// have consistent type signatures and implement the interface correctly.
/// </summary>
/// <remarks>
/// These tests use Reflection to verify API consistency across all providers without requiring
/// database infrastructure. Behavioral verification for database providers belongs in integration tests.
/// </remarks>
[Trait("Category", "Contract")]
public sealed class ConsentStoreProviderContractTests
{
    /// <summary>
    /// All 13 provider types that implement IConsentStore.
    /// </summary>
    private static readonly Type[] AllProviderTypes =
    [
        // ADO.NET (4)
        typeof(ADOSqliteConsent.ConsentStoreADO),
        typeof(ADOSqlServerConsent.ConsentStoreADO),
        typeof(ADOPostgreSQLConsent.ConsentStoreADO),
        typeof(ADOMySQLConsent.ConsentStoreADO),
        // Dapper (4)
        typeof(DapperSqliteConsent.ConsentStoreDapper),
        typeof(DapperSqlServerConsent.ConsentStoreDapper),
        typeof(DapperPostgreSQLConsent.ConsentStoreDapper),
        typeof(DapperMySQLConsent.ConsentStoreDapper),
        // EF Core (1 shared)
        typeof(EFCoreConsent.ConsentStoreEF),
        // MongoDB (1)
        typeof(MongoDBConsent.ConsentStoreMongoDB),
    ];

    /// <summary>
    /// Contract: All providers must implement IConsentStore.
    /// </summary>
    [Fact]
    public void Contract_AllProviders_ImplementIConsentStore()
    {
        foreach (var providerType in AllProviderTypes)
        {
            typeof(IConsentStore).IsAssignableFrom(providerType)
                .ShouldBeTrue($"{providerType.FullName} must implement IConsentStore");
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
            typeof(ADOSqliteConsent.ConsentStoreADO),
            typeof(ADOSqlServerConsent.ConsentStoreADO),
            typeof(ADOPostgreSQLConsent.ConsentStoreADO),
            typeof(ADOMySQLConsent.ConsentStoreADO),
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
            typeof(DapperSqliteConsent.ConsentStoreDapper),
            typeof(DapperSqlServerConsent.ConsentStoreDapper),
            typeof(DapperPostgreSQLConsent.ConsentStoreDapper),
            typeof(DapperMySQLConsent.ConsentStoreDapper),
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
    /// Contract: All IConsentStore interface methods must exist on every provider.
    /// </summary>
    [Fact]
    public void Contract_AllProviders_HaveAllInterfaceMethods()
    {
        var interfaceMethods = typeof(IConsentStore)
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
                    $"{providerType.Name} is missing IConsentStore method '{method}'");
            }
        }
    }

    /// <summary>
    /// Contract: All ADO.NET and Dapper providers must have a constructor accepting a connection factory.
    /// </summary>
    [Fact]
    public void Contract_ADOAndDapperProviders_HaveConstructor()
    {
        var providerTypes = new[]
        {
            typeof(ADOSqliteConsent.ConsentStoreADO),
            typeof(ADOSqlServerConsent.ConsentStoreADO),
            typeof(ADOPostgreSQLConsent.ConsentStoreADO),
            typeof(ADOMySQLConsent.ConsentStoreADO),
            typeof(DapperSqliteConsent.ConsentStoreDapper),
            typeof(DapperSqlServerConsent.ConsentStoreDapper),
            typeof(DapperPostgreSQLConsent.ConsentStoreDapper),
            typeof(DapperMySQLConsent.ConsentStoreDapper),
        };

        foreach (var providerType in providerTypes)
        {
            var constructors = providerType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            constructors.Length.ShouldBeGreaterThan(0,
                $"{providerType.Name} must have at least one public constructor");
        }
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

#endregion
