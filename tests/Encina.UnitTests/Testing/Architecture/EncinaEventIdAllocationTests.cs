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
/// goes missing. EventIds created with <c>LoggerMessage.Define(..., new EventId(n, ...), ...)</c> are
/// constructor arguments, not attributes, so <see cref="EventIdUniquenessRule"/> cannot reflect on them; they
/// are instead found by scanning <c>src/&lt;Package&gt;/**/*.cs</c> for <c>new EventId(&lt;literal&gt;</c>
/// (see <see cref="DefinedEventIds"/>, <see cref="DefinedEventIds_LieInsideTheRangesMappedToTheirPackage"/>
/// and <see cref="DefinedEventIds_DoNotCollideWithAnyOtherEventId"/>, #1125).
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
            ["Encina.AspNetCore"] = [nameof(EventIdRanges.AspNetCore)],
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
            ["Encina.Compliance.AIAct"] = [nameof(EventIdRanges.ComplianceAIAct)],
            ["Encina.Compliance.Anonymization"] = [nameof(EventIdRanges.ComplianceAnonymization)],
            ["Encina.Compliance.Attestation"] = [nameof(EventIdRanges.ComplianceAttestation)],
            ["Encina.Compliance.BreachNotification"] = [nameof(EventIdRanges.ComplianceBreachNotification)],
            ["Encina.Compliance.Consent"] = [nameof(EventIdRanges.ComplianceConsent)],
            ["Encina.Compliance.CrossBorderTransfer"] = [nameof(EventIdRanges.ComplianceCrossBorderTransfer)],
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
            ["Encina.Marten.GDPR"] = [nameof(EventIdRanges.MartenGDPRCryptoShredding)],
            ["Encina.Messaging"] = [nameof(EventIdRanges.MessagingOutbox), nameof(EventIdRanges.MessagingInbox), nameof(EventIdRanges.MessagingSaga), nameof(EventIdRanges.MessagingScheduling), nameof(EventIdRanges.Messaging)],
            ["Encina.Messaging.Encryption"] = [nameof(EventIdRanges.MessagingEncryption)],
            ["Encina.Messaging.Encryption.AwsKms"] = [nameof(EventIdRanges.MessagingEncryption)],
            ["Encina.Messaging.Encryption.AzureKeyVault"] = [nameof(EventIdRanges.MessagingEncryption)],
            ["Encina.Messaging.Encryption.DataProtection"] = [nameof(EventIdRanges.MessagingEncryption)],
            ["Encina.MongoDB"] = [nameof(EventIdRanges.MongoDB)],
            ["Encina.MQTT"] = [nameof(EventIdRanges.MQTT)],
            ["Encina.NATS"] = [nameof(EventIdRanges.NATS)],
            ["Encina.OpenTelemetry"] = [nameof(EventIdRanges.OpenTelemetry)],
            ["Encina.Polly"] = [nameof(EventIdRanges.Polly)],
            ["Encina.Quartz"] = [nameof(EventIdRanges.Quartz)],
            ["Encina.RabbitMQ"] = [nameof(EventIdRanges.RabbitMQ)],
            ["Encina.Redis.PubSub"] = [nameof(EventIdRanges.RedisPubSub)],
            ["Encina.Refit"] = [nameof(EventIdRanges.Refit)],
            ["Encina.Security"] = [nameof(EventIdRanges.Security)],
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
            ["Encina.Testing"] = [nameof(EventIdRanges.Testing)],
        };

    private static readonly Lazy<IReadOnlyList<Assembly>> EncinaAssemblies = new(LoadEncinaAssemblies);

    /// <summary>
    /// EventIds created with <c>LoggerMessage.Define(..., new EventId(n, ...), ...)</c>. They are constructor
    /// arguments, not attributes, so they are read from the source: every <c>new EventId(&lt;literal&gt;</c> in
    /// <c>src/&lt;Package&gt;/**/*.cs</c> outside comments.
    /// </summary>
    private static readonly Lazy<IReadOnlyList<(string Package, string Location, int EventId)>> DefinedEventIds = new(() =>
    {
        var srcRoot = Path.Combine(FindRepositoryRoot(), "src");
        var pattern = new System.Text.RegularExpressions.Regex(@"new\s+EventId\s*\(\s*(?<id>\d+)\s*[,)]");
        var result = new List<(string Package, string Location, int EventId)>();

        foreach (var dir in Directory.EnumerateDirectories(srcRoot))
        {
            var package = Path.GetFileName(dir);
            foreach (var file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories).Where(f => !IsBuildOutput(f)))
            {
                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                {
                    if (lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    foreach (System.Text.RegularExpressions.Match m in pattern.Matches(lines[i]))
                    {
                        var location = $"{Path.GetRelativePath(srcRoot, file).Replace('\\', '/')}:{i + 1}";
                        result.Add((package!, location, int.Parse(m.Groups["id"].Value, System.Globalization.CultureInfo.InvariantCulture)));
                    }
                }
            }
        }

        return result;
    });

    [Fact]
    public void EveryLoggerMessage_DeclaresAnEventId()
    {
        var violations = EventIdUniquenessRule.AssertEveryLoggerMessageHasEventId(EncinaAssemblies.Value);

        violations.ShouldBeEmpty(string.Join(Environment.NewLine, violations));
    }

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

        var withDefinedEventIds = DefinedEventIds.Value.Select(e => e.Package);
        withEventIds.UnionWith(withDefinedEventIds);

        var stale = AssemblyRanges.Keys.Where(k => !withEventIds.Contains(k)).Order(StringComparer.Ordinal).ToList();

        stale.ShouldBeEmpty(
            "These assemblies are mapped but declare no [LoggerMessage] or LoggerMessage.Define EventIds: " +
            string.Join(", ", stale));
    }

    [Fact]
    public void DefinedEventIds_LieInsideTheRangesMappedToTheirPackage()
    {
        var ranges = EventIdRanges.GetAllRanges().ToDictionary(r => r.Name, r => (r.Min, r.Max), StringComparer.Ordinal);
        var violations = new List<string>();

        foreach (var entry in DefinedEventIds.Value)
        {
            if (!AssemblyRanges.TryGetValue(entry.Package, out var names) || names.Count == 0)
            {
                violations.Add($"{entry.Location}: EventId {entry.EventId} in package '{entry.Package}', which has no entry in AssemblyRanges.");
                continue;
            }

            if (!names.Any(n => ranges.TryGetValue(n, out var r) && entry.EventId >= r.Min && entry.EventId <= r.Max))
            {
                violations.Add($"{entry.Location}: EventId {entry.EventId} is outside the ranges of '{entry.Package}' ({string.Join(", ", names)}).");
            }
        }

        violations.ShouldBeEmpty(string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void DefinedEventIds_DoNotCollideWithAnyOtherEventId()
    {
        var attributeIds = EventIdUniquenessRule.ExtractEventIds(EncinaAssemblies.Value)
            .Select(e => (e.EventId, Location: $"{e.AssemblyName}::{e.TypeName}.{e.MethodName}"));
        var definedIds = DefinedEventIds.Value.Select(e => (e.EventId, e.Location));

        var collisions = attributeIds.Concat(definedIds)
            .GroupBy(e => e.EventId)
            .Where(g => g.Count() > 1 && g.Any(e => DefinedEventIds.Value.Any(d => d.Location == e.Location)))
            .Select(g => $"EventId {g.Key}: {string.Join(", ", g.Select(e => e.Location))}")
            .ToList();

        collisions.ShouldBeEmpty(string.Join(Environment.NewLine, collisions));
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
