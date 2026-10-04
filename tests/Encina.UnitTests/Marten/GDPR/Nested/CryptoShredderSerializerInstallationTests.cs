using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using Encina.Marten.GDPR;

using Marten;
using Marten.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.UnitTests.Marten.GDPR.Nested;

/// <summary>
/// Installation of the contract modifier: verified by resolver identity, resolver chain and a nested canary; the
/// factory only accepts Marten's System.Text.Json serializer; the configurator turns off SkipSerializationErrors.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CryptoShredderSerializerInstallationTests
{
    private const string Secret = "installation-secret@example.com";

    private static IServiceScopeFactory ScopeFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton<global::Encina.Marten.GDPR.Abstractions.ISubjectKeyProvider>(
            new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance));
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public void Installed_OnEveryOptionsObject_AndTheCanaryPasses()
    {
        using var harness = new CryptoHarness();

        harness.Serializer.InstalledOptionsCount.ShouldBe(4);
        Should.NotThrow(harness.Serializer.VerifyContractModifierInstalled);
    }

    [Fact]
    public void ResolverReplacedAfterInstallation_FailsTheCheckAndTheNextWrite()
    {
        using var harness = new CryptoHarness();
        harness.Inner.Configure(o => o.TypeInfoResolver = new DefaultJsonTypeInfoResolver());

        Should.Throw<CryptoShreddingConfigurationException>(harness.Serializer.VerifyContractModifierInstalled)
            .Problem.ShouldBe(CryptoShreddingConfigurationProblem.ContractModifierMissing);
        Should.Throw<CryptoShreddingConfigurationException>(() => harness.Serializer.ToJson(new NoPiiEvent()));
        Should.Throw<CryptoShreddingConfigurationException>(() => harness.Serializer.ToJson(new NoPiiEvent()));
        harness.Logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 8471);
    }

    [Fact]
    public void ContextPlacedAheadAfterInstallation_FailsTheIdentityCheck()
    {
        using var harness = new CryptoHarness();
        harness.Inner.UseTypeInfoResolver(DefaultModeContext.Default);

        Should.Throw<CryptoShreddingConfigurationException>(harness.Serializer.VerifyContractModifierInstalled);
    }

    [Fact]
    public void ResolverChainInsertedAhead_FailsTheChainCheck()
    {
        using var harness = new CryptoHarness();
        harness.Inner.Configure(o => o.TypeInfoResolverChain.Insert(0, DefaultModeContext.Default));

        Should.Throw<CryptoShreddingConfigurationException>(harness.Serializer.VerifyContractModifierInstalled);
    }

    [Fact]
    public void ContextInstalledBeforeTheModifier_IsWrappedAndEncrypted()
    {
        var inner = (SystemTextJsonSerializer)new StoreOptions().Serializer();
        inner.UseTypeInfoResolver(DefaultModeContext.Default);
        var serializer = new CryptoShredderSerializer(inner, ScopeFactory(), new FakeLogger<CryptoShredderSerializer>());

        var json = serializer.ToJson(new TopLevelOwner { PatientId = "p", Email = Secret });

        json.ShouldNotContain(Secret);
        json.ShouldContain(CryptoShreddingToken.Prefix);
    }

    [Fact]
    public void ReadOnlyOptions_CannotBeInstalled()
    {
        var inner = (SystemTextJsonSerializer)new StoreOptions().Serializer();
        inner.ToJson(new NoPiiEvent());

        Should.Throw<CryptoShreddingConfigurationException>(
                () => new CryptoShredderSerializer(inner, ScopeFactory(), new FakeLogger<CryptoShredderSerializer>()))
            .Problem.ShouldBe(CryptoShreddingConfigurationProblem.ContractModifierMissing);
    }

    [Fact]
    public void ResolveContract_ReportsWhetherTheModifierSawTheType()
    {
        using var harness = new CryptoHarness();

        harness.Serializer.ResolveContract(typeof(NestedEvent), out var seen).Type.ShouldBe(typeof(NestedEvent));
        seen.ShouldBeTrue();
        Should.Throw<CryptoShreddingConfigurationException>(() => harness.Serializer.ResolveContract(typeof(GetterOnlyOwner), out _));
    }

    [Fact]
    public void Factory_WrapsOnceAndLogs8460()
    {
        var options = new StoreOptions();
        var logger = new FakeLogger<CryptoShredderSerializer>();

        CryptoShredderSerializerFactory.Apply(options, ScopeFactory(), logger);
        var wrapped = options.Serializer();
        CryptoShredderSerializerFactory.Apply(options, ScopeFactory(), logger);

        wrapped.ShouldBeOfType<CryptoShredderSerializer>();
        options.Serializer().ShouldBeSameAs(wrapped);
        logger.Collector.GetSnapshot().Count(r => r.Id.Id == 8460).ShouldBe(1);
    }

    [Fact]
    public void Factory_NonSystemTextJsonSerializer_FailsClosed()
    {
        var options = new StoreOptions();
        options.Serializer(Substitute.For<ISerializer>());
        var logger = new FakeLogger<CryptoShredderSerializer>();

        Should.Throw<CryptoShreddingConfigurationException>(() => CryptoShredderSerializerFactory.Apply(options, ScopeFactory(), logger))
            .Problem.ShouldBe(CryptoShreddingConfigurationProblem.SerializerNotSupported);
        logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 8471);
    }

    [Fact]
    public void Configurator_WrapsAndTurnsOffSkipSerializationErrors()
    {
        var options = new StoreOptions();
        options.Projections.Errors.SkipSerializationErrors.ShouldBeTrue();
        var configurator = new ConfigureMartenCryptoShredding(
            ScopeFactory(), Options.Create(new CryptoShreddingOptions()), new FakeLogger<CryptoShredderSerializer>());

        configurator.Configure(options);

        options.Serializer().ShouldBeOfType<CryptoShredderSerializer>();
        options.Projections.Errors.SkipSerializationErrors.ShouldBeFalse();
    }

    [Fact]
    public void Staging_IsNeededOnlyWhenTheRootCanReachCryptoData()
    {
        CryptoShredderSerializer.NeedsStaging(typeof(NestedEvent)).ShouldBeTrue();
        CryptoShredderSerializer.NeedsStaging(typeof(ObjectMemberEvent)).ShouldBeTrue();
        CryptoShredderSerializer.NeedsStaging(typeof(PolymorphicEvent)).ShouldBeTrue();
        CryptoShredderSerializer.NeedsStaging(typeof(NoPiiEvent)).ShouldBeFalse();
    }
}

[JsonSerializable(typeof(TopLevelOwner))]
internal sealed partial class DefaultModeContext : JsonSerializerContext;
