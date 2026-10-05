using Encina.Compliance.AIAct;
using Encina.Compliance.Anonymization;
using Encina.Compliance.Attestation;
using Encina.Compliance.BreachNotification;
using Encina.Compliance.Consent;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DPIA;
using Encina.Compliance.GDPR;
using Encina.Compliance.NIS2;
using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.Retention;
using Encina.DataAnnotations;
using Encina.EntityFrameworkCore;
using Encina.FluentValidation;
using Encina.MiniValidator;
using Encina.OpenTelemetry;
using Encina.Security;
using Encina.Security.ABAC;
using Encina.Security.AntiTampering;
using Encina.Security.Audit;
using Encina.Security.Encryption;
using Microsoft.EntityFrameworkCore;

namespace Encina.UnitTests.Core;

/// <summary>
/// Proves that every package registering an open-generic <see cref="IPipelineBehavior{TRequest, TResponse}"/>
/// leaves its behavior in the pipeline whichever package registered a behavior first (#1635).
/// <c>TryAdd</c> keys on the service type, so a second <c>TryAddTransient</c> of a different behavior
/// was silently skipped; <c>TryAddEnumerable</c> keys on the implementation type instead.
/// </summary>
public sealed class PipelineBehaviorRegistrationTests
{
    private static readonly ServiceProviderOptions StrictOptions = new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true,
    };

    private static readonly (string Name, Action<IServiceCollection> Register)[] Registrations =
    [
        ("Security", s => s.AddEncinaSecurity()),
        ("ABAC", s => s.AddEncinaABAC()),
        ("Audit", s => s.AddEncinaAudit()),
        ("Encryption", s => s.AddEncinaEncryption()),
        ("AntiTampering", s => s.AddEncinaAntiTampering()),
        ("DataAnnotations", s => s.AddDataAnnotationsValidation()),
        ("MiniValidator", s => s.AddMiniValidation()),
        ("FluentValidation", s => s.AddEncinaFluentValidation(typeof(PipelineBehaviorRegistrationTests).Assembly)),
        ("OpenTelemetry", s => s.AddEncinaOpenTelemetry()),
        ("EntityFrameworkCoreTransactions", s => s.AddEncinaEntityFrameworkCore<BehaviorDbContext>(c => c.UseTransactions = true)),
        ("AIAct", s => s.AddEncinaAIAct()),
        ("Anonymization", s => s.AddEncinaAnonymization()),
        ("Attestation", s => s.AddEncinaAttestation(o => o.UseInMemory())),
        ("BreachNotification", s => s.AddEncinaBreachNotification()),
        ("Consent", s => s.AddEncinaConsent()),
        ("DataResidency", s => s.AddEncinaDataResidency()),
        ("DataSubjectRights", s => s.AddEncinaDataSubjectRights()),
        ("DPIA", s => s.AddEncinaDPIA()),
        ("GDPR", s => s.AddEncinaGDPR()),
        ("NIS2", s => s.AddEncinaNIS2()),
        ("PrivacyByDesign", s => s.AddEncinaPrivacyByDesign()),
        ("ProcessorAgreements", s => s.AddEncinaProcessorAgreements()),
        ("Retention", s => s.AddEncinaRetention()),
    ];

    /// <summary>The pairs the issue names, plus every other ordered pair, in both orders.</summary>
    public static TheoryData<string, string> OrderedPairs
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var first in Registrations)
            {
                foreach (var second in Registrations)
                {
                    if (first.Name != second.Name)
                    {
                        data.Add(first.Name, second.Name);
                    }
                }
            }

            return data;
        }
    }

    public static TheoryData<string> RegistrationNames
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var registration in Registrations)
            {
                data.Add(registration.Name);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(RegistrationNames))]
    public void Registration_AddsAnOpenGenericPipelineBehavior(string name)
    {
        var behaviors = BehaviorTypesAddedBy(Find(name));

        behaviors.ShouldNotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(OrderedPairs))]
    public void Registration_InAnyOrder_KeepsBothBehaviors(string firstName, string secondName)
    {
        var firstBehaviors = BehaviorTypesAddedBy(Find(firstName));
        var secondBehaviors = BehaviorTypesAddedBy(Find(secondName));
        var services = NewServices();

        Find(firstName)(services);
        Find(secondName)(services);

        var registered = BehaviorTypes(services);
        foreach (var behavior in firstBehaviors.Concat(secondBehaviors))
        {
            registered.ShouldContain(behavior);
        }
    }

    [Fact]
    public void Registration_RepeatedCalls_AddEachBehaviorOnce()
    {
        var services = NewServices();

        // Audit and the EF Core transaction behavior use a plain scoped Add (always added, never
        // skipped), so they are outside the TryAddEnumerable idempotency this test proves.
        var enumerable = Registrations.Where(r => r.Name is not ("Audit" or "EntityFrameworkCoreTransactions")).ToList();
        foreach (var registration in enumerable.Concat(enumerable))
        {
            registration.Register(services);
        }

        var implementations = services
            .Where(d => d.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType)
            .Where(t => t is not null)
            .ToList();
        implementations.Distinct().Count().ShouldBe(implementations.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SecurityAndAbac_ResolveBothBehaviorsFromAScope(bool securityFirst)
    {
        var services = NewServices();
        AddEncinaCore(services);

        if (securityFirst)
        {
            services.AddEncinaSecurity();
            services.AddEncinaABAC();
        }
        else
        {
            services.AddEncinaABAC();
            services.AddEncinaSecurity();
        }

        AssertBothResolve(services, typeof(SecurityPipelineBehavior<,>), typeof(ABACPipelineBehavior<,>));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AuditAndSecurity_ResolveBothBehaviorsFromAScope(bool auditFirst)
    {
        var services = NewServices();
        AddEncinaCore(services);

        if (auditFirst)
        {
            services.AddEncinaAudit();
            services.AddEncinaSecurity();
        }
        else
        {
            services.AddEncinaSecurity();
            services.AddEncinaAudit();
        }

        AssertBothResolve(services, typeof(SecurityPipelineBehavior<,>), typeof(AuditPipelineBehavior<,>));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidationAndTransaction_ResolveBothBehaviorsFromAScope(bool validationFirst)
    {
        var services = NewServices();
        AddEncinaCore(services);
        services.AddDbContext<BehaviorDbContext>(o => o.UseInMemoryDatabase("pipeline-behavior-" + validationFirst));

        if (validationFirst)
        {
            services.AddDataAnnotationsValidation();
            services.AddEncinaEntityFrameworkCore<BehaviorDbContext>(c => c.UseTransactions = true);
        }
        else
        {
            services.AddEncinaEntityFrameworkCore<BehaviorDbContext>(c => c.UseTransactions = true);
            services.AddDataAnnotationsValidation();
        }

        AssertBothResolve(
            services,
            typeof(global::Encina.Validation.ValidationPipelineBehavior<,>),
            typeof(global::Encina.EntityFrameworkCore.TransactionPipelineBehavior<,>));
    }

    private static void AssertBothResolve(IServiceCollection services, Type first, Type second)
    {
        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var behaviors = scope.ServiceProvider
            .GetServices<IPipelineBehavior<BehaviorRequest, BehaviorResponse>>()
            .Select(b => b.GetType().GetGenericTypeDefinition())
            .ToList();

        behaviors.ShouldContain(first);
        behaviors.ShouldContain(second);
    }

    private static void AddEncinaCore(IServiceCollection services) =>
        services.AddEncina(typeof(IEncina).Assembly);

    private static ServiceCollection NewServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services;
    }

    private static Action<IServiceCollection> Find(string name) =>
        Registrations.Single(r => r.Name == name).Register;

    private static System.Collections.Generic.HashSet<Type> BehaviorTypesAddedBy(Action<IServiceCollection> register)
    {
        var services = NewServices();
        register(services);
        return BehaviorTypes(services);
    }

    private static System.Collections.Generic.HashSet<Type> BehaviorTypes(IServiceCollection services) =>
        services
            .Where(d => d.ServiceType == typeof(IPipelineBehavior<,>) && d.ImplementationType is not null)
            .Select(d => d.ImplementationType!)
            .ToHashSet();

    public sealed record BehaviorRequest : IRequest<BehaviorResponse>;

    public sealed record BehaviorResponse;

    public sealed class BehaviorDbContext(DbContextOptions<BehaviorDbContext> options) : DbContext(options);
}
