using System.Reflection;

using Encina.Diagnostics;
using Encina.Testing.Architecture;

using Shouldly;

namespace Encina.UnitTests.Testing.Architecture;

/// <summary>
/// ADR-021 enforcement over the whole solution (issue #1120): every <c>[LoggerMessage]</c> EventId declared by a
/// shipped <c>Encina*</c> assembly is unique, lies inside a range of <see cref="EventIdRanges"/> mapped to that
/// assembly in <see cref="AssemblyRanges"/>, and the registered ranges do not overlap.
/// </summary>
/// <remarks>
/// <para>
/// The assemblies are the <c>Encina*.dll</c> files in the test output directory, minus the test projects
/// themselves. <c>Encina.UnitTests</c> references every <c>src/</c> package that declares <c>[LoggerMessage]</c>
/// methods, directly or transitively, and <see cref="EveryPackageWithLoggerMessagesIsScanned"/> fails if one
/// goes missing. EventIds created with <c>LoggerMessage.Define</c> / <c>new EventId(n)</c> are not attributes
/// and are not seen by this test.
/// </para>
/// <para>
/// When a package gains structured logging, register its range in <c>EventIdRanges.cs</c> and add the
/// assembly to <see cref="AssemblyRanges"/>; this test tells you when either step is missing.
/// </para>
/// </remarks>
public sealed class EncinaEventIdAllocationTests
{
    /// <summary>
    /// Assembly name → the <see cref="EventIdRanges"/> fields its EventIds may use.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> AssemblyRanges =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["Encina"] = [nameof(EventIdRanges.Core), nameof(EventIdRanges.ModuleIsolation)],
            ["Encina.ADO.MySQL"] = [nameof(EventIdRanges.ADOMySQL)],
            ["Encina.ADO.PostgreSQL"] = [nameof(EventIdRanges.ADOPostgreSQL)],
            ["Encina.ADO.SqlServer"] = [nameof(EventIdRanges.ADOSqlServer)],
            ["Encina.AmazonSQS"] = [nameof(EventIdRanges.AmazonSQS)],
            ["Encina.Audit.Marten"] = [nameof(EventIdRanges.AuditMarten)],
            ["Encina.AwsLambda"] = [nameof(EventIdRanges.AwsLambda)],
            ["Encina.AzureFunctions"] = [nameof(EventIdRanges.AzureFunctions)],
            ["Encina.AzureServiceBus"] = [nameof(EventIdRanges.AzureServiceBus)],
            ["Encina.Caching"] = [nameof(EventIdRanges.Caching)],
            ["Encina.Caching.Hybrid"] = [nameof(EventIdRanges.CachingHybrid)],
            ["Encina.Caching.Memory"] = [nameof(EventIdRanges.CachingMemory)],
            ["Encina.Caching.Redis"] = [nameof(EventIdRanges.CachingRedis)],
            ["Encina.Cdc"] = [nameof(EventIdRanges.Cdc)],
            ["Encina.Cdc.Debezium"] = [nameof(EventIdRanges.CdcDebezium)],
            ["Encina.Cdc.SqlServer"] = [nameof(EventIdRanges.CdcSqlServer)],
            ["Encina.Compliance.Anonymization"] = [nameof(EventIdRanges.ComplianceAnonymization)],
            ["Encina.Compliance.Attestation"] = [nameof(EventIdRanges.ComplianceAttestation)],
            ["Encina.Compliance.BreachNotification"] = [nameof(EventIdRanges.ComplianceBreachNotification)],
            ["Encina.Compliance.DataResidency"] = [nameof(EventIdRanges.ComplianceDataResidency)],
            ["Encina.Compliance.DataSubjectRights"] = [nameof(EventIdRanges.ComplianceDSR)],
            ["Encina.Compliance.DPIA"] = [nameof(EventIdRanges.ComplianceDPIA)],
            ["Encina.Compliance.GDPR"] = [nameof(EventIdRanges.ComplianceGDPR)],
            ["Encina.Compliance.LawfulBasis"] = [nameof(EventIdRanges.ComplianceLawfulBasis)],
            ["Encina.Compliance.NIS2"] = [nameof(EventIdRanges.ComplianceNIS2)],
            ["Encina.Compliance.PrivacyByDesign"] = [nameof(EventIdRanges.CompliancePrivacyByDesign)],
            ["Encina.Compliance.ProcessorAgreements"] = [nameof(EventIdRanges.ComplianceProcessorAgreements)],
            ["Encina.Compliance.Retention"] = [nameof(EventIdRanges.ComplianceRetention)],
            ["Encina.Dapper.MySQL"] = [nameof(EventIdRanges.DapperMySQL)],
            ["Encina.Dapper.PostgreSQL"] = [nameof(EventIdRanges.DapperPostgreSQL)],
            ["Encina.Dapper.SqlServer"] = [nameof(EventIdRanges.DapperSqlServer)],
            ["Encina.DistributedLock.InMemory"] = [nameof(EventIdRanges.DistributedLockInMemory)],
            ["Encina.DistributedLock.Redis"] = [nameof(EventIdRanges.DistributedLockRedis)],
            ["Encina.DistributedLock.SqlServer"] = [nameof(EventIdRanges.DistributedLockSqlServer)],
            ["Encina.DomainModeling"] = [nameof(EventIdRanges.DomainModelingRepository), nameof(EventIdRanges.DomainModelingUnitOfWork), nameof(EventIdRanges.DomainModelingBulkOperations), nameof(EventIdRanges.DomainModelingSpecification), nameof(EventIdRanges.DomainModelingAudit), nameof(EventIdRanges.DomainModelingDomainEvents)],
            ["Encina.EntityFrameworkCore"] = [nameof(EventIdRanges.EntityFrameworkCore), nameof(EventIdRanges.EntityFrameworkCoreSoftDelete), nameof(EventIdRanges.EntityFrameworkCoreQueryCache)],
            ["Encina.Extensions.Resilience"] = [nameof(EventIdRanges.ExtensionsResilience)],
            ["Encina.GraphQL"] = [nameof(EventIdRanges.GraphQL)],
            ["Encina.gRPC"] = [nameof(EventIdRanges.GRpc)],
            ["Encina.Hangfire"] = [nameof(EventIdRanges.Hangfire)],
            ["Encina.IdGeneration"] = [nameof(EventIdRanges.IdGeneration)],
            ["Encina.InMemory"] = [nameof(EventIdRanges.InMemory)],
            ["Encina.Kafka"] = [nameof(EventIdRanges.Kafka)],
            ["Encina.Marten"] = [nameof(EventIdRanges.Marten)],
            ["Encina.Messaging"] = [nameof(EventIdRanges.MessagingOutbox), nameof(EventIdRanges.MessagingInbox), nameof(EventIdRanges.MessagingSaga), nameof(EventIdRanges.MessagingScheduling), nameof(EventIdRanges.Messaging)],
            ["Encina.Messaging.Encryption"] = [nameof(EventIdRanges.MessagingEncryption)],
            ["Encina.Messaging.Encryption.AwsKms"] = [nameof(EventIdRanges.MessagingEncryption)],
            ["Encina.Messaging.Encryption.AzureKeyVault"] = [nameof(EventIdRanges.MessagingEncryption)],
            ["Encina.Messaging.Encryption.DataProtection"] = [nameof(EventIdRanges.MessagingEncryption)],
            ["Encina.MongoDB"] = [nameof(EventIdRanges.MongoDB)],
            ["Encina.MQTT"] = [nameof(EventIdRanges.MQTT)],
            ["Encina.NATS"] = [nameof(EventIdRanges.NATS)],
            ["Encina.Polly"] = [nameof(EventIdRanges.Polly)],
            ["Encina.Quartz"] = [nameof(EventIdRanges.Quartz)],
            ["Encina.RabbitMQ"] = [nameof(EventIdRanges.RabbitMQ)],
            ["Encina.Redis.PubSub"] = [nameof(EventIdRanges.RedisPubSub)],
            ["Encina.Refit"] = [nameof(EventIdRanges.Refit)],
            ["Encina.Security.ABAC"] = [nameof(EventIdRanges.SecurityABAC)],
            ["Encina.Security.AntiTampering"] = [nameof(EventIdRanges.SecurityAntiTampering)],
            ["Encina.Security.Audit"] = [nameof(EventIdRanges.SecurityAuditRead), nameof(EventIdRanges.SecurityAudit)],
            ["Encina.Security.PII"] = [nameof(EventIdRanges.SecurityPII)],
            ["Encina.Security.Sanitization"] = [nameof(EventIdRanges.Sanitization)],
            ["Encina.Security.Secrets"] = [nameof(EventIdRanges.SecuritySecrets)],
            ["Encina.Security.Secrets.AwsSecretsManager"] = [nameof(EventIdRanges.SecuritySecretsAwsSecretsManager)],
            ["Encina.Security.Secrets.AzureKeyVault"] = [nameof(EventIdRanges.SecuritySecretsAzureKeyVault)],
            ["Encina.Security.Secrets.GoogleCloudSecretManager"] = [nameof(EventIdRanges.SecuritySecretsGoogleCloudSecretManager)],
            ["Encina.Security.Secrets.HashiCorpVault"] = [nameof(EventIdRanges.SecuritySecretsHashiCorpVault)],
            ["Encina.SignalR"] = [nameof(EventIdRanges.SignalR)],
            ["Encina.Tenancy"] = [nameof(EventIdRanges.Tenancy)],
        };

    private static readonly Lazy<IReadOnlyList<Assembly>> EncinaAssemblies = new(LoadEncinaAssemblies);

    [Fact]
    public void EventIds_AreUniqueAcrossAndWithinAssemblies()
    {
        var violations = EventIdUniquenessRule.AssertEventIdsAreGloballyUnique(EncinaAssemblies.Value);

        violations.ShouldBeEmpty(string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void EventIds_LieInsideTheRangesMappedToTheirAssembly()
    {
        var violations = EventIdUniquenessRule.AssertEventIdsWithinRegisteredRanges(
            EncinaAssemblies.Value, AssemblyRanges);

        violations.ShouldBeEmpty(string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void RegisteredRanges_DoNotOverlap()
    {
        var violations = EventIdUniquenessRule.AssertNoRangeOverlaps();

        violations.ShouldBeEmpty(string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void AssemblyRanges_HasNoStaleEntries()
    {
        var withEventIds = EventIdUniquenessRule.ExtractEventIds(EncinaAssemblies.Value)
            .Select(e => e.AssemblyName)
            .ToHashSet(StringComparer.Ordinal);

        var stale = AssemblyRanges.Keys.Where(k => !withEventIds.Contains(k)).Order(StringComparer.Ordinal).ToList();

        stale.ShouldBeEmpty(
            "These assemblies are mapped but declare no [LoggerMessage] EventIds in the test output: " +
            string.Join(", ", stale));
    }

    [Fact]
    public void EveryPackageWithLoggerMessagesIsScanned()
    {
        // Source of truth: the src/ packages whose code contains [LoggerMessage(. Each must be loaded,
        // otherwise a missing project reference would silently exempt it from the checks above.
        var srcRoot = Path.Combine(FindRepositoryRoot(), "src");
        var packagesWithLoggerMessages = Directory.EnumerateDirectories(srcRoot)
            .Where(dir => Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
                .Where(file => !IsBuildOutput(file))
                .Any(file => File.ReadAllText(file).Contains("[LoggerMessage(", StringComparison.Ordinal)))
            .Select(Path.GetFileName)
            .ToList();

        var loaded = EncinaAssemblies.Value
            .Select(a => a.GetName().Name!)
            .ToHashSet(StringComparer.Ordinal);

        var missing = packagesWithLoggerMessages.Where(p => !loaded.Contains(p!)).Order(StringComparer.Ordinal).ToList();

        packagesWithLoggerMessages.ShouldNotBeEmpty();
        missing.ShouldBeEmpty(
            "These src/ packages declare [LoggerMessage] methods but are not in the test output; " +
            "add a ProjectReference from Encina.UnitTests: " + string.Join(", ", missing));
    }

    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Encina.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException($"Encina.slnx not found above {AppContext.BaseDirectory}.");
    }

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static List<Assembly> LoadEncinaAssemblies()
    {
        var directory = AppContext.BaseDirectory;

        return Directory.EnumerateFiles(directory, "Encina*.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && IsShippedPackage(name))
            .Order(StringComparer.Ordinal)
            .Select(name => Assembly.Load(new AssemblyName(name!)))
            .ToList();
    }

    private static bool IsShippedPackage(string name) =>
        !name.EndsWith("Tests", StringComparison.Ordinal)
        && !name.Contains(".TestInfrastructure", StringComparison.Ordinal)
        && !name.Contains(".Testing.Examples", StringComparison.Ordinal);
}
