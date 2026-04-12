using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Health;
using Encina.Compliance.PrivacyByDesign.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.GuardTests.Compliance.PrivacyByDesign;

/// <summary>
/// Guard tests exercising happy paths of PrivacyByDesign classes to generate
/// line coverage on files marked with the guard flag.
/// </summary>
[Trait("Category", "Guard")]
public sealed class PrivacyByDesignHappyPathGuardTests : IDisposable
{
    private readonly InMemoryPurposeRegistry _registry;

    public PrivacyByDesignHappyPathGuardTests()
    {
        _registry = new InMemoryPurposeRegistry(NullLogger<InMemoryPurposeRegistry>.Instance);
    }

    public void Dispose() => _registry.Clear();

    private static PurposeDefinition MakePurpose(string name, string? moduleId = null) => new()
    {
        PurposeId = $"pid-{name}-{moduleId ?? "global"}",
        Name = name,
        Description = "Purpose description",
        LegalBasis = "Contract",
        AllowedFields = ["Field1"],
        ModuleId = moduleId,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    // ─── InMemoryPurposeRegistry happy paths ───

    [Fact]
    public async Task RegisterThenGet_ReturnsRegistered()
    {
        var purpose = MakePurpose("Marketing");
        var reg = await _registry.RegisterPurposeAsync(purpose);
        reg.IsRight.ShouldBeTrue();

        var get = await _registry.GetPurposeAsync("Marketing");
        get.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterModuleScoped_GetWithModule_Returns()
    {
        var purpose = MakePurpose("Analytics", "mod-1");
        await _registry.RegisterPurposeAsync(purpose);

        var get = await _registry.GetPurposeAsync("Analytics", "mod-1");
        get.IsRight.ShouldBeTrue();
        get.IfRight(opt => opt.IsSome.ShouldBeTrue());
    }

    [Fact]
    public async Task RegisterModuleScoped_GetGlobal_ReturnsNone()
    {
        var purpose = MakePurpose("ScopedOnly", "mod-2");
        await _registry.RegisterPurposeAsync(purpose);

        var get = await _registry.GetPurposeAsync("ScopedOnly");
        get.IsRight.ShouldBeTrue();
        get.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task RegisterThenRemove_GetReturnsNone()
    {
        var purpose = MakePurpose("ToDelete");
        await _registry.RegisterPurposeAsync(purpose);
        await _registry.RemovePurposeAsync(purpose.PurposeId);

        var get = await _registry.GetPurposeAsync("ToDelete");
        get.IsRight.ShouldBeTrue();
        get.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task RemoveUnknown_ReturnsError()
    {
        var result = await _registry.RemovePurposeAsync("nonexistent-id");
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAllGlobal_ReturnsOnlyGlobal()
    {
        await _registry.RegisterPurposeAsync(MakePurpose("Global1"));
        await _registry.RegisterPurposeAsync(MakePurpose("Scoped1", "mod-x"));

        var all = await _registry.GetAllPurposesAsync();
        all.IsRight.ShouldBeTrue();
        all.IfRight(list =>
        {
            list.ShouldContain(p => p.Name == "Global1");
            list.ShouldNotContain(p => p.Name == "Scoped1");
        });
    }

    [Fact]
    public async Task GetAllWithModule_MergesGlobalAndModule()
    {
        await _registry.RegisterPurposeAsync(MakePurpose("Base"));
        await _registry.RegisterPurposeAsync(MakePurpose("ModOnly", "mod-y"));

        var all = await _registry.GetAllPurposesAsync("mod-y");
        all.IsRight.ShouldBeTrue();
        all.IfRight(list => list.Count.ShouldBeGreaterThanOrEqualTo(2));
    }

    [Fact]
    public async Task DuplicatePurposeName_DifferentId_ReturnsError()
    {
        await _registry.RegisterPurposeAsync(MakePurpose("Dup"));

        var dup = new PurposeDefinition
        {
            PurposeId = "different-id",
            Name = "Dup",
            Description = "Other",
            LegalBasis = "Consent",
            AllowedFields = [],
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var result = await _registry.RegisterPurposeAsync(dup);
        result.IsLeft.ShouldBeTrue();
    }

    // ─── PrivacyByDesignHealthCheck ───

    [Fact]
    public async Task HealthCheck_NoPbDRegistered_ReturnsDegraded()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var sut = new PrivacyByDesignHealthCheck(sp, NullLogger<PrivacyByDesignHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", sut, null, null)
        };

        var result = await sut.CheckHealthAsync(context);

        // Without PrivacyByDesignOptions registered, health check reports unhealthy/degraded
        result.Status.ShouldNotBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task HealthCheck_WithOptionsRegistered_ReturnsHealthy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddEncinaPrivacyByDesign();
        var sp = services.BuildServiceProvider();

        var sut = new PrivacyByDesignHealthCheck(sp, NullLogger<PrivacyByDesignHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", sut, null, null)
        };

        var result = await sut.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    // ─── PurposeDefinition.IsCurrent ───

    [Fact]
    public void PurposeDefinition_NoExpiry_AlwaysCurrent()
    {
        var purpose = MakePurpose("NeverExpires");
        purpose.IsCurrent(DateTimeOffset.UtcNow).ShouldBeTrue();
        purpose.IsCurrent(DateTimeOffset.MaxValue).ShouldBeTrue();
    }

    [Fact]
    public void PurposeDefinition_ExpiredInPast_NotCurrent()
    {
        var purpose = new PurposeDefinition
        {
            PurposeId = "exp",
            Name = "Expired",
            Description = "d",
            LegalBasis = "l",
            AllowedFields = [],
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-30),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };

        purpose.IsCurrent(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    // ─── PrivacyByDesignOptions defaults ───

    [Fact]
    public void PrivacyByDesignOptions_Defaults()
    {
        var options = new PrivacyByDesignOptions();

        options.EnforcementMode.ShouldBe(PrivacyByDesignEnforcementMode.Warn);
        options.PrivacyLevel.ShouldBe(PrivacyLevel.Standard);
        options.MinimizationScoreThreshold.ShouldBeGreaterThanOrEqualTo(0.0);
        options.MinimizationScoreThreshold.ShouldBeLessThanOrEqualTo(1.0);
        options.PurposeBuilders.ShouldNotBeNull();
    }

    // ─── PrivacyByDesignOptionsValidator ───

    [Fact]
    public void Validator_ValidOptions_Succeeds()
    {
        var validator = new PrivacyByDesignOptionsValidator();
        validator.Validate(null, new PrivacyByDesignOptions()).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validator_InvalidEnforcementMode_Fails()
    {
        var validator = new PrivacyByDesignOptionsValidator();
        var options = new PrivacyByDesignOptions
        {
            EnforcementMode = (PrivacyByDesignEnforcementMode)999
        };
        validator.Validate(null, options).Failed.ShouldBeTrue();
    }
}
