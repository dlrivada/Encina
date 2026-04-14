using Encina.AspNetCore;
using Encina.AspNetCore.Authorization;
using Encina.AspNetCore.Health;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Encina.GuardTests.AspNetCore;

/// <summary>
/// Guard tests covering null-guard clauses for public Encina.AspNetCore APIs.
/// Internal types are excluded (GuardTests lacks InternalsVisibleTo for this package).
/// </summary>
[Trait("Category", "Guard")]
public sealed class AspNetCoreGuardTests
{
    // ─── HealthCheckBuilderExtensions null guards ───

    [Fact]
    public void AddEncinaHealthChecks_NullBuilder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HealthCheckBuilderExtensions.AddEncinaHealthChecks(null!));
    }

    [Fact]
    public void AddEncinaHealthChecks_ValidBuilder_Succeeds()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();
        Should.NotThrow(() => builder.AddEncinaHealthChecks());
    }

    [Fact]
    public void AddEncinaOutbox_NullBuilder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HealthCheckBuilderExtensions.AddEncinaOutbox(null!));
    }

    [Fact]
    public void AddEncinaInbox_NullBuilder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HealthCheckBuilderExtensions.AddEncinaInbox(null!));
    }

    [Fact]
    public void AddEncinaSaga_NullBuilder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HealthCheckBuilderExtensions.AddEncinaSaga(null!));
    }

    [Fact]
    public void AddEncinaScheduling_NullBuilder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HealthCheckBuilderExtensions.AddEncinaScheduling(null!));
    }

    [Fact]
    public void AddEncinaDatabasePool_NullBuilder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HealthCheckBuilderExtensions.AddEncinaDatabasePool(null!));
    }

    [Fact]
    public void AddEncinaModuleHealthChecks_NullBuilder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HealthCheckBuilderExtensions.AddEncinaModuleHealthChecks(null!));
    }

    [Fact]
    public void AddEncinaIdGenerationHealthCheck_NullBuilder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HealthCheckBuilderExtensions.AddEncinaIdGenerationHealthCheck(null!));
    }

    [Fact]
    public void AddEncinaReferenceTableReplication_NullBuilder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HealthCheckBuilderExtensions.AddEncinaReferenceTableReplication(null!));
    }

    [Fact]
    public void AddEncinaTierTransition_NullBuilder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HealthCheckBuilderExtensions.AddEncinaTierTransition(null!));
    }

    [Fact]
    public void AddEncinaShardCreation_NullBuilder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HealthCheckBuilderExtensions.AddEncinaShardCreation(null!));
    }

    [Fact]
    public void AddEncinaAudit_NullBuilder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HealthCheckBuilderExtensions.AddEncinaAudit(null!));
    }

    [Fact]
    public void AddEncinaQueryCache_NullBuilder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HealthCheckBuilderExtensions.AddEncinaQueryCache(null!));
    }

    // ─── Valid happy paths (exercise registration logic) ───

    [Fact]
    public void AddEncinaOutbox_ValidBuilder_Succeeds()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();
        Should.NotThrow(() => builder.AddEncinaOutbox());
    }

    [Fact]
    public void AddEncinaInbox_ValidBuilder_Succeeds()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();
        Should.NotThrow(() => builder.AddEncinaInbox());
    }

    [Fact]
    public void AddEncinaSaga_ValidBuilder_Succeeds()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();
        Should.NotThrow(() => builder.AddEncinaSaga());
    }

    [Fact]
    public void AddEncinaScheduling_ValidBuilder_Succeeds()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();
        Should.NotThrow(() => builder.AddEncinaScheduling());
    }

    [Fact]
    public void AddEncinaDatabasePool_ValidBuilder_Succeeds()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();
        Should.NotThrow(() => builder.AddEncinaDatabasePool());
    }

    [Fact]
    public void AddEncinaModuleHealthChecks_ValidBuilder_Succeeds()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();
        Should.NotThrow(() => builder.AddEncinaModuleHealthChecks());
    }

    // ─── HttpAuditContextExtensions null guards (extends IRequestContext) ───

    [Fact]
    public void GetIpAddress_NullContext_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HttpAuditContextExtensions.GetIpAddress(null!));
    }

    [Fact]
    public void WithIpAddress_NullContext_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HttpAuditContextExtensions.WithIpAddress(null!, "1.2.3.4"));
    }

    [Fact]
    public void GetUserAgent_NullContext_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HttpAuditContextExtensions.GetUserAgent(null!));
    }

    [Fact]
    public void WithUserAgent_NullContext_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HttpAuditContextExtensions.WithUserAgent(null!, "Mozilla"));
    }

    // ─── HttpDataResidencyContextExtensions null guards ───

    [Fact]
    public void GetDataRegion_NullContext_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HttpDataResidencyContextExtensions.GetDataRegion(null!));
    }

    [Fact]
    public void WithDataRegion_NullContext_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            HttpDataResidencyContextExtensions.WithDataRegion(null!, "eu-west"));
    }

    // ─── DPIAEndpointExtensions null guard ───

    [Fact]
    public void MapDPIAEndpoints_NullEndpoints_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            DPIAEndpointExtensions.MapDPIAEndpoints(null!));
    }

    // ─── ServiceCollectionExtensions ───

    [Fact]
    public void AddEncinaAspNetCore_ValidServices_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var result = services.AddEncinaAspNetCore();

        result.ShouldNotBeNull();
        services.ShouldContain(sd => sd.ServiceType == typeof(IRequestContextAccessor));
        services.ShouldContain(sd => sd.ServiceType == typeof(IHttpContextAccessor));
    }

    // ─── EncinaAspNetCoreOptions ───

    [Fact]
    public void EncinaAspNetCoreOptions_DefaultValues()
    {
        var options = new EncinaAspNetCoreOptions();
        options.ShouldNotBeNull();
    }

    // ─── AuthorizationConfiguration ───

    [Fact]
    public void AuthorizationConfiguration_DefaultValues()
    {
        var config = new AuthorizationConfiguration();
        config.ShouldNotBeNull();
    }

    // ─── ResourceAuthorizeAttribute ───

    [Fact]
    public void ResourceAuthorizeAttribute_ValidPolicy_SetsPolicy()
    {
        var attr = new ResourceAuthorizeAttribute("TestPolicy");
        attr.Policy.ShouldBe("TestPolicy");
    }

    [Fact]
    public void ResourceAuthorizeAttribute_NullPolicy_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new ResourceAuthorizeAttribute(null!));
        ex.ParamName.ShouldBe("policy");
    }

    [Fact]
    public void ResourceAuthorizeAttribute_EmptyPolicy_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new ResourceAuthorizeAttribute(string.Empty));
        ex.ParamName.ShouldBe("policy");
    }

    [Fact]
    public void ResourceAuthorizeAttribute_WhitespacePolicy_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new ResourceAuthorizeAttribute("   "));
        ex.ParamName.ShouldBe("policy");
    }
}
