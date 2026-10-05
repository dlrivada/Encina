using System.Reflection;
using System.Text.Json.Serialization.Metadata;

using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Diagnostics;

using Marten;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Marten.GDPR;

/// <summary>
/// Validates crypto-shredding at startup and stops the host when anything would store personal data in plaintext,
/// make it unreadable, or keep it out of erasure.
/// </summary>
/// <remarks>
/// <para>
/// In order: the store serializer is a <see cref="CryptoShredderSerializer"/>; the contract modifier is still
/// installed (resolver identity and chain, nested canary); the infrastructure is safe (the async daemon does not
/// skip serialization errors, the erasure router and the Marten locator are not bypassed); then every type of the
/// scanned assemblies that declares or reaches a <c>[CryptoShredded]</c> property is classified through its
/// System.Text.Json contract, following the contract graph (property, element, key and derived types) so closed
/// generics and types of other assemblies are checked too.
/// </para>
/// <para>
/// Every issue is reported once, on its declaring type, with its reasons (event 8459), and one
/// <see cref="CryptoShreddingConfigurationException"/> lists them all (event 8470). Open generic types get the
/// reflection rules only; their subject-id type check is deferred to the closed types (event 8473).
/// </para>
/// </remarks>
internal sealed class CryptoShreddingStartupValidationHostedService : IHostedLifecycleService
{
    private readonly CryptoShreddingValidationDescriptor _descriptor;
    private readonly IServiceProvider _services;
    private readonly CryptoShreddingOptions _options;
    private readonly ILogger<CryptoShreddingStartupValidationHostedService> _logger;

    public CryptoShreddingStartupValidationHostedService(
        CryptoShreddingValidationDescriptor descriptor,
        IServiceProvider services,
        IOptions<CryptoShreddingOptions> options,
        ILogger<CryptoShreddingStartupValidationHostedService> logger)
    {
        _descriptor = descriptor;
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Validates before any hosted service starts (<see cref="IHostedLifecycleService.StartingAsync"/> runs before every
    /// <c>StartAsync</c>), so a misconfiguration stops the host before Marten's async daemon processes an event.
    /// </summary>
    public Task StartingAsync(CancellationToken cancellationToken)
    {
        if (!_options.ValidateOnStartup)
        {
            _logger.StartupValidationSkipped();
            return Task.CompletedTask;
        }

        var store = _services.GetRequiredService<IDocumentStore>();
        var serializer = RequireWrappedSerializer(store);
        serializer.VerifyContractModifierInstalled();
        CheckInfrastructure(store);

        var typeCount = ValidateTypes(serializer);
        _logger.StartupValidationCompleted(typeCount);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private CryptoShredderSerializer RequireWrappedSerializer(IDocumentStore store)
    {
        var serializer = store.Options.Serializer();
        return serializer as CryptoShredderSerializer
            ?? throw Infrastructure(CryptoShreddingConfigurationProblem.SerializerNotWrapped, serializer.GetType());
    }

    // -- Infrastructure --------------------------------------------------------------------------------------------

    private void CheckInfrastructure(IDocumentStore store)
    {
        if (store.Options is StoreOptions { Projections.Errors.SkipSerializationErrors: true })
        {
            throw Infrastructure(CryptoShreddingConfigurationProblem.ProjectionSkipsSerializationErrors, typeof(StoreOptions));
        }

        using var scope = _services.CreateScope();
        CheckErasureStrategy(scope.ServiceProvider);
        CheckLocator(scope.ServiceProvider);
        WarnInMemoryKeyStore(scope.ServiceProvider);
    }

    private void CheckErasureStrategy(IServiceProvider scoped)
    {
        var strategy = scoped.GetService<IDataErasureStrategy>();
        if (strategy is not CryptoShredRoutingErasureStrategy)
        {
            throw Infrastructure(CryptoShreddingConfigurationProblem.ErasureStrategyBypassed, strategy?.GetType() ?? typeof(IDataErasureStrategy));
        }
    }

    private void CheckLocator(IServiceProvider scoped)
    {
        var resolved = scoped.GetService<IPersonalDataLocator>();
        if (scoped.GetServices<IPersonalDataLocator>().Count() > 1 && !IncludesMartenLocator(resolved))
        {
            throw Infrastructure(CryptoShreddingConfigurationProblem.PersonalDataLocatorBypassed, resolved!.GetType());
        }
    }

    internal static bool IncludesMartenLocator(IPersonalDataLocator? locator) =>
        locator is MartenEventPersonalDataLocator
        || (locator is CompositePersonalDataLocator composite && composite.Locators.Any(l => l is MartenEventPersonalDataLocator));

    private void WarnInMemoryKeyStore(IServiceProvider scoped)
    {
        if (scoped.GetService<ISubjectKeyProvider>() is InMemorySubjectKeyProvider)
        {
            _logger.InMemoryKeyStoreInUse();
        }
    }

    private CryptoShreddingConfigurationException Infrastructure(CryptoShreddingConfigurationProblem problem, Type component)
    {
        var componentName = component.FullName ?? component.Name;
        _logger.CryptoShreddingInfrastructureInvalid(problem.ToString(), componentName);
        return new CryptoShreddingConfigurationException(problem, [], componentName);
    }

    // -- Types -----------------------------------------------------------------------------------------------------

    private int ValidateTypes(CryptoShredderSerializer serializer)
    {
        var scan = new ScanState();
        foreach (var type in _descriptor.Assemblies.SelectMany(LoadTypes).Where(IsCandidate))
        {
            ValidateCandidate(serializer, type, scan);
        }

        if (scan.Unseen.Count > 0)
        {
            throw Infrastructure(CryptoShreddingConfigurationProblem.ContractModifierMissing, scan.Unseen[0]);
        }

        ThrowIfIssues(scan);
        return scan.OwnerTypes.Count;
    }

    private void ValidateCandidate(CryptoShredderSerializer serializer, Type type, ScanState scan)
    {
        var shape = CryptoShreddedPropertyClassifier.GetShape(type);
        if (shape.IsOwner)
        {
            scan.OwnerTypes.Add(type);
            WarnPersonalDataSubjectIds(shape, scan);
        }

        if (type.ContainsGenericParameters)
        {
            scan.AddIssues(shape.Issues);
            foreach (var property in shape.DeferredChecks)
            {
                _logger.OpenGenericCheckDeferred(CryptoShreddedPropertyClassifier.TypeName(type), property);
            }

            return;
        }

        WalkContracts(serializer, type, scan);
    }

    /// <summary>Resolves the contract of a closed type and of every crypto-relevant type its contract reaches.</summary>
    private static void WalkContracts(CryptoShredderSerializer serializer, Type root, ScanState scan)
    {
        var pending = new Queue<Type>([root]);
        while (pending.Count > 0)
        {
            var type = pending.Dequeue();
            if (!scan.Visited.Add(type) || ResolveOrCollect(serializer, type, scan) is not { } typeInfo)
            {
                continue;
            }

            foreach (var next in ContractNeighbours(typeInfo).Where(IsCryptoRelevant))
            {
                pending.Enqueue(next);
            }
        }
    }

    private static JsonTypeInfo? ResolveOrCollect(CryptoShredderSerializer serializer, Type type, ScanState scan)
    {
        try
        {
            var typeInfo = serializer.ResolveContract(type, out var modifierSawType);
            if (!modifierSawType)
            {
                scan.Unseen.Add(type);
            }

            return typeInfo;
        }
        catch (CryptoShreddingConfigurationException ex)
        {
            scan.AddIssues(ex.Issues);
            return null;
        }
        catch (Exception ex) when (ex is NotSupportedException or ArgumentException)
        {
            // A type System.Text.Json cannot handle at all is never written, so it cannot leak.
            return null;
        }
    }

    private static IEnumerable<Type> ContractNeighbours(JsonTypeInfo typeInfo) =>
        typeInfo.Properties.Select(property => property.PropertyType)
            .Concat(new[] { typeInfo.ElementType, typeInfo.KeyType }.OfType<Type>())
            .Concat(DerivedTypes(typeInfo));

    private static IEnumerable<Type> DerivedTypes(JsonTypeInfo typeInfo) =>
        typeInfo.PolymorphismOptions is { } polymorphism ? polymorphism.DerivedTypes.Select(d => d.DerivedType) : [];

    private static bool IsCryptoRelevant(Type type) =>
        CryptoShreddedPropertyClassifier.GetShape(type).IsRelevant || CryptoShreddedPropertyClassifier.ReachesCryptoOwner(type);

    private static bool IsCandidate(Type type) =>
        !CryptoShreddedPropertyClassifier.IsTerminal(type)
        && !(type.IsAbstract && type.IsSealed)
        && !typeof(Delegate).IsAssignableFrom(type)
        && IsCryptoRelevant(type);

    private void WarnPersonalDataSubjectIds(CryptoShreddedTypeShape shape, ScanState scan)
    {
        foreach (var subjectId in shape.Members.Select(m => m.SubjectIdProperty).OfType<PropertyInfo>())
        {
            if (CryptoShreddedPropertyClassifier.PersonalDataOf(subjectId) is not null
                && scan.WarnedSubjectIds.Add((subjectId.DeclaringType!, subjectId.Name)))
            {
                _logger.SubjectIdPropertyIsPersonalData(CryptoShreddedPropertyClassifier.TypeName(subjectId.DeclaringType!), subjectId.Name);
            }
        }
    }

    private void ThrowIfIssues(ScanState scan)
    {
        if (scan.Issues.Count == 0)
        {
            return;
        }

        var issues = scan.Issues
            .Select(entry => new CryptoShreddedPropertyIssue(entry.Key.Type, entry.Key.Property, entry.Value))
            .ToList();
        foreach (var issue in issues)
        {
            _logger.AttributeMisconfigured(issue.DeclaringTypeName, issue.PropertyName, issue.Problems.ToString());
        }

        _logger.StartupValidationFailed(issues.Count, issues.Select(i => i.DeclaringTypeName).Distinct(StringComparer.Ordinal).Count());
        throw new CryptoShreddingConfigurationException(CryptoShreddingConfigurationProblem.MisconfiguredProperties, issues);
    }

    private Type[] LoadTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.StartupTypeLoadPartial(assembly.GetName().Name ?? "unknown", ex.LoaderExceptions.Length);
            return [.. ex.Types.OfType<Type>()];
        }
    }

    /// <summary>The state of one startup scan.</summary>
    private sealed class ScanState
    {
        internal HashSet<Type> Visited { get; } = [];

        internal HashSet<Type> OwnerTypes { get; } = [];

        internal List<Type> Unseen { get; } = [];

        internal HashSet<(Type, string)> WarnedSubjectIds { get; } = [];

        internal Dictionary<(string Type, string Property), CryptoShreddedPropertyProblems> Issues { get; } = [];

        /// <summary>Adds issues, merging the flags of the same (declaring type, property).</summary>
        internal void AddIssues(IEnumerable<CryptoShreddedPropertyIssue> issues)
        {
            foreach (var issue in issues)
            {
                var key = (issue.DeclaringTypeName, issue.PropertyName);
                Issues[key] = Issues.TryGetValue(key, out var existing) ? existing | issue.Problems : issue.Problems;
            }
        }
    }
}
