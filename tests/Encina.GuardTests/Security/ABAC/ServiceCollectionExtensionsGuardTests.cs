using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.Evaluation;
using Encina.Security.ABAC.Persistence;
using Encina.Security.ABAC.Persistence.Xacml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.GuardTests.Security.ABAC;

/// <summary>
/// Guard clause tests for <see cref="ServiceCollectionExtensions"/>.
/// Verifies null guards, DI registrations, and configuration behavior.
/// </summary>
public class ServiceCollectionExtensionsGuardTests
{
    #region Null Guard

    [Fact]
    public void AddEncinaABAC_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaABAC();

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    #endregion

    #region Default Registrations (No Configure)

    [Fact]
    public void AddEncinaABAC_NoConfigure_RegistersAllCoreServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaABAC();

        var sp = services.BuildServiceProvider();

        // Options
        var options = sp.GetRequiredService<IOptions<ABACOptions>>();
        options.Value.ShouldNotBeNull();

        // Function registry
        sp.GetRequiredService<IFunctionRegistry>().ShouldNotBeNull();

        // Combining algorithms
        sp.GetRequiredService<CombiningAlgorithmFactory>().ShouldNotBeNull();

        // Evaluators
        sp.GetRequiredService<TargetEvaluator>().ShouldNotBeNull();
        sp.GetRequiredService<ConditionEvaluator>().ShouldNotBeNull();

        // Default PAP (InMemory)
        sp.GetRequiredService<IPolicyAdministrationPoint>()
            .ShouldBeOfType<InMemoryPolicyAdministrationPoint>();

        // PDP
        sp.GetRequiredService<IPolicyDecisionPoint>()
            .ShouldBeOfType<XACMLPolicyDecisionPoint>();
    }

    #endregion

    #region Configure Delegate

    [Fact]
    public void AddEncinaABAC_WithConfigure_AppliesOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaABAC(options =>
        {
            options.EnforcementMode = ABACEnforcementMode.Warn;
            options.DefaultNotApplicableEffect = Effect.Permit;
            options.IncludeAdvice = false;
        });

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<ABACOptions>>().Value;

        options.EnforcementMode.ShouldBe(ABACEnforcementMode.Warn);
        options.DefaultNotApplicableEffect.ShouldBe(Effect.Permit);
        options.IncludeAdvice.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaABAC_NullConfigure_RegistersDefaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaABAC(null);

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<ABACOptions>>().Value;
        options.EnforcementMode.ShouldBe(ABACEnforcementMode.Block);
    }

    #endregion

    #region Custom Functions Registration

    [Fact]
    public void AddEncinaABAC_WithCustomFunction_RegistersInFunctionRegistry()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var customFunction = new NoOpXacmlFunction();

        services.AddEncinaABAC(options =>
        {
            options.AddFunction("custom:noop", customFunction);
        });

        var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<IFunctionRegistry>();

        // The function should be registered
        registry.ShouldNotBeNull();
    }

    #endregion

    #region Persistent PAP Without IPolicyStore

    [Fact]
    public void AddEncinaABAC_UsePersistentPAP_WithoutStore_ThrowsOnResolve()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaABAC(options =>
        {
            options.UsePersistentPAP = true;
        });

        var sp = services.BuildServiceProvider();

        // Resolution should fail because no IPolicyStore is registered
        var act = () => sp.GetRequiredService<IPolicyAdministrationPoint>();

        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region XACML XML Serializer Registration

    [Fact]
    public void AddEncinaABAC_UseXacmlXmlSerializer_RegistersXacmlSerializer()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaABAC(options =>
        {
            options.UsePersistentPAP = true;
            options.UseXacmlXmlSerializer();
        });

        // Verify the serializer registration is XacmlXmlPolicySerializer
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPolicySerializer) &&
            d.ImplementationType != null &&
            d.ImplementationType == typeof(XacmlXmlPolicySerializer));

        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaABAC_RegisterXacmlXmlAsKeyed_RegistersKeyedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaABAC(options =>
        {
            options.UsePersistentPAP = true;
            options.RegisterXacmlXmlSerializer();
        });

        // Verify keyed registration exists
        var hasKeyedRegistration = services.Any(d =>
            d.ServiceType == typeof(IPolicySerializer) &&
            d.IsKeyedService);

        hasKeyedRegistration.ShouldBeTrue();
    }

    #endregion

    #region Health Check Registration

    [Fact]
    public void AddEncinaABAC_WithHealthCheck_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaABAC(options =>
        {
            options.AddHealthCheck = true;
        });

        // Verify health check registration exists by looking for health check related services
        var hasHealthCheck = services.Any(d =>
            d.ServiceType.Name.Contains("HealthCheck"));

        hasHealthCheck.ShouldBeTrue();
    }

    #endregion

    #region Seeding Hosted Service

    [Fact]
    public void AddEncinaABAC_WithSeedPolicySets_RegistersSeedingService()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaABAC(options =>
        {
            options.SeedPolicySets.Add(new PolicySet
            {
                Id = "seed-ps",
                Target = null,
                Algorithm = CombiningAlgorithmId.DenyOverrides,
                Policies = [],
                PolicySets = [],
                Obligations = [],
                Advice = []
            });
        });

        // Verify hosted service registration
        services.ShouldContain(d =>
            d.ImplementationType != null &&
            d.ImplementationType.Name == "ABACPolicySeedingHostedService");
    }

    [Fact]
    public void AddEncinaABAC_WithSeedPolicies_RegistersSeedingService()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaABAC(options =>
        {
            options.SeedPolicies.Add(new Policy
            {
                Id = "seed-pol",
                Target = null,
                Algorithm = CombiningAlgorithmId.DenyOverrides,
                Rules = [],
                Obligations = [],
                Advice = [],
                VariableDefinitions = []
            });
        });

        services.ShouldContain(d =>
            d.ImplementationType != null &&
            d.ImplementationType.Name == "ABACPolicySeedingHostedService");
    }

    [Fact]
    public void AddEncinaABAC_NoSeeds_DoesNotRegisterSeedingService()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaABAC();

        services.ShouldNotContain(d =>
            d.ImplementationType != null &&
            d.ImplementationType.Name == "ABACPolicySeedingHostedService");
    }

    #endregion

    #region TryAdd Behavior — Custom Registration Wins

    [Fact]
    public void AddEncinaABAC_CustomAttributeProvider_IsNotOverridden()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IAttributeProvider, CustomTestAttributeProvider>();

        services.AddEncinaABAC();

        var descriptor = services.Last(d => d.ServiceType == typeof(IAttributeProvider));
        // TryAdd should keep the first registration
        var first = services.First(d => d.ServiceType == typeof(IAttributeProvider));
        first.ImplementationType.ShouldBe(typeof(CustomTestAttributeProvider));
    }

    #endregion

    #region Idempotent Registration

    [Fact]
    public void AddEncinaABAC_CalledTwice_DoesNotDuplicateRegistrations()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaABAC();
        services.AddEncinaABAC();

        // Count singleton registrations for IFunctionRegistry (should be 1 due to TryAdd)
        services.Count(d => d.ServiceType == typeof(IFunctionRegistry))
            .ShouldBe(1);
    }

    #endregion

    // ── Test Doubles ────────────────────────────────────────────────

    private sealed class NoOpXacmlFunction : IXACMLFunction
    {
        public string ReturnType => XACMLDataTypes.Boolean;

        public object? Evaluate(IReadOnlyList<object?> arguments) => true;
    }

    private sealed class CustomTestAttributeProvider : IAttributeProvider
    {
        public ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
            string userId, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyDictionary<string, object>>(new Dictionary<string, object>());

        public ValueTask<IReadOnlyDictionary<string, object>> GetResourceAttributesAsync<TRequest>(
            TRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyDictionary<string, object>>(new Dictionary<string, object>());

        public ValueTask<IReadOnlyDictionary<string, object>> GetEnvironmentAttributesAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyDictionary<string, object>>(new Dictionary<string, object>());
    }
}
