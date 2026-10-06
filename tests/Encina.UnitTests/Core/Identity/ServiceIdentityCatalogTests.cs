using Encina.Testing.Identity;
using Microsoft.Extensions.Hosting;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// Declared service identities: the builder, the catalog, the catalog validator, the registration
/// methods (DI under <c>ValidateOnBuild</c> and <c>ValidateScopes</c>, startup validation) and the
/// accessor-type startup check.
/// </summary>
public sealed class ServiceIdentityCatalogTests
{
    private static readonly ServiceProviderOptions Strict = new() { ValidateOnBuild = true, ValidateScopes = true };

    private static ServiceIdentityCatalogOptionsValidator Validator(RequestIdentityOptions? options = null) =>
        new(Microsoft.Extensions.Options.Options.Create(options ?? new RequestIdentityOptions()));

    private static ServiceIdentityCatalogOptions Declared(params ServiceIdentityDefinition[] definitions)
    {
        var options = new ServiceIdentityCatalogOptions();
        foreach (var definition in definitions)
        {
            options.Declare(definition);
        }

        return options;
    }

    // ── Builder and definition ───────────────────────────────────────────

    [Fact]
    public void TheBuilder_CollectsRolesPermissionsAndClaims_CaseInsensitively()
    {
        var definition = new ServiceIdentityBuilder()
            .WithRoles("Job", "job")
            .WithPermissions("jobs:run")
            .WithClaim("department", "finance")
            .Build("billing", isBuiltIn: false);

        definition.Name.ShouldBe("billing");
        definition.Roles.Count.ShouldBe(1);
        definition.Roles.ShouldContain("JOB");
        definition.Permissions.ShouldBe(["jobs:run"]);
        definition.Claims.ShouldBe([new KeyValuePair<string, string>("department", "finance")]);
        definition.ToString().ShouldBe("ServiceIdentityDefinition { Name = billing }");
    }

    [Fact]
    public void TheBuilder_RejectsBlankEntries()
    {
        var builder = new ServiceIdentityBuilder();

        Should.Throw<ArgumentException>(() => builder.WithRoles(" "));
        Should.Throw<ArgumentException>(() => builder.WithPermissions(""));
        Should.Throw<ArgumentNullException>(() => builder.WithRoles(null!));
        Should.Throw<ArgumentException>(() => builder.WithClaim(" ", "v"));
        Should.Throw<ArgumentException>(() => builder.WithClaim("t", " "));
    }

    [Fact]
    public void IsEquivalentTo_ComparesEveryDeclaredValue()
    {
        var a = new ServiceIdentityBuilder().WithRoles("r").WithClaim("c", "1").Build("job", false);

        a.IsEquivalentTo(new ServiceIdentityBuilder().WithRoles("r").WithClaim("c", "1").Build("job", false)).ShouldBeTrue();
        a.IsEquivalentTo(new ServiceIdentityBuilder().WithRoles("r", "s").WithClaim("c", "1").Build("job", false)).ShouldBeFalse();
        a.IsEquivalentTo(new ServiceIdentityBuilder().WithRoles("r").WithClaim("c", "2").Build("job", false)).ShouldBeFalse();
        a.IsEquivalentTo(new ServiceIdentityBuilder().WithRoles("r").WithPermissions("p").WithClaim("c", "1").Build("job", false)).ShouldBeFalse();
    }

    // ── Catalog ──────────────────────────────────────────────────────────

    [Fact]
    public void TheCatalog_FindsDeclaredNamesOrdinally()
    {
        var catalog = new ServiceIdentityCatalog(Microsoft.Extensions.Options.Options.Create(
            Declared(new ServiceIdentityBuilder().Build("job", false))));

        catalog.TryGet("job", out var found).ShouldBeTrue();
        found!.Name.ShouldBe("job");
        catalog.TryGet("JOB", out _).ShouldBeFalse();
        Should.Throw<ArgumentNullException>(() => catalog.TryGet(null!, out _));
    }

    [Fact]
    public void Declare_IgnoresAnIdenticalRepeat_AndRecordsAConflict()
    {
        var options = Declared(
            new ServiceIdentityBuilder().WithRoles("r").Build("job", false),
            new ServiceIdentityBuilder().WithRoles("r").Build("job", false));
        options.ConflictingNames.ShouldBeEmpty();

        options.Declare(new ServiceIdentityBuilder().WithRoles("admin").Build("job", false));

        options.ConflictingNames.ShouldBe(["job"]);
        options.Identities["job"].Roles.ShouldBe(["r"]);
    }

    // ── Validator ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Job")]
    [InlineData("-job")]
    [InlineData("job_1")]
    [InlineData("")]
    public void TheValidator_RejectsNamesOutsideThePattern(string name)
    {
        Validator().Validate(null, Declared(new ServiceIdentityBuilder().Build(name, false))).Failed.ShouldBeTrue();
    }

    [Fact]
    public void TheValidator_RejectsAnOverLongName_AndAcceptsTheLongestValidOne()
    {
        Validator().Validate(null, Declared(new ServiceIdentityBuilder().Build(new string('a', 64), false))).Failed.ShouldBeTrue();
        Validator().Validate(null, Declared(new ServiceIdentityBuilder().Build(new string('a', 63), false))).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void TheValidator_ReservesTheEncinaPrefixForBuiltIns()
    {
        Validator().Validate(null, Declared(new ServiceIdentityBuilder().Build("encina.x", false))).Failed.ShouldBeTrue();
        Validator().Validate(null, Declared(new ServiceIdentityBuilder().Build("job", true))).Failed.ShouldBeTrue();
        Validator().Validate(null, Declared(new ServiceIdentityBuilder().Build("encina.x", true))).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void TheValidator_RejectsWildcards_ConflictsAndUserIdClaims()
    {
        Validator().Validate(null, Declared(new ServiceIdentityBuilder().WithRoles("*").Build("job", false))).Failed.ShouldBeTrue();
        Validator().Validate(null, Declared(new ServiceIdentityBuilder().WithPermissions("jobs:*").Build("job", false))).Failed.ShouldBeTrue();
        Validator().Validate(null, Declared(new ServiceIdentityBuilder().WithClaim("sub", "x").Build("job", false))).Failed.ShouldBeTrue();
        Validator().Validate(null, Declared(new ServiceIdentityBuilder().WithClaim("role", "admin").Build("job", false))).Failed.ShouldBeTrue();
        Validator().Validate(null, Declared(new ServiceIdentityBuilder().WithClaim("permission", "*").Build("job", false))).Failed.ShouldBeTrue();
        Validator().Validate(null, Declared(new ServiceIdentityBuilder().WithClaim("tid", "t1").Build("job", false))).Failed.ShouldBeTrue();
        Validator().Validate(null, Declared(new ServiceIdentityBuilder().WithClaim("department", "finance").Build("job", false))).Succeeded.ShouldBeTrue();
        Validator().Validate(null, Declared(new ServiceIdentityBuilder().WithClaim(RequestIdentity.IdentityKindClaimType, "user").Build("job", false))).Failed.ShouldBeTrue();

        var conflicting = Declared(new ServiceIdentityBuilder().Build("job", false));
        conflicting.Declare(new ServiceIdentityBuilder().WithRoles("r").Build("job", false));
        Validator().Validate(null, conflicting).Failures!.ShouldContain(f => f.Contains("declared more than once"));
    }

    // ── Registration ─────────────────────────────────────────────────────

    [Fact]
    public void AddEncinaServiceIdentity_ResolvesUnderStrictValidation_WithOneSingletonBehindBothFactoryInterfaces()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaServiceIdentity("billing", id => id.WithRoles("billing-job"));

        using var provider = services.BuildServiceProvider(Strict);
        var publicFactory = provider.GetRequiredService<IRequestContextScopeFactory>();
        var internalFactory = provider.GetRequiredService<IInternalRequestContextScopeFactory>();
        publicFactory.ShouldBeOfType<RequestContextScopeFactory>();
        internalFactory.ShouldBeSameAs(publicFactory);
        provider.GetRequiredService<IServiceIdentityCatalog>().TryGet("billing", out var definition).ShouldBeTrue();
        definition!.Roles.ShouldBe(["billing-job"]);
    }

    [Fact]
    public void AddEncina_RegistersTheScopeFactoryAndCatalog()
    {
        var services = new ServiceCollection();
        services.AddEncina();

        using var provider = services.BuildServiceProvider(Strict);
        provider.GetRequiredService<IRequestContextScopeFactory>().ShouldNotBeNull();
        provider.GetRequiredService<IServiceIdentityCatalog>().ShouldNotBeNull();
    }

    [Theory]
    [InlineData("Bad Name")]
    [InlineData("encina.reserved")]
    [InlineData("")]
    public void AddEncinaServiceIdentity_RejectsInvalidAndReservedNamesEagerly(string name)
    {
        Should.Throw<ArgumentException>(() => new ServiceCollection().AddEncinaServiceIdentity(name));
    }

    [Fact]
    public void AddBuiltInServiceIdentity_RequiresTheEncinaPrefix_AndMarksTheDeclarationBuiltIn()
    {
        Should.Throw<ArgumentException>(() => new ServiceCollection().AddBuiltInServiceIdentity("seeding"));
        var services = new ServiceCollection();

        services.AddBuiltInServiceIdentity("encina.seeding", id => id.WithPermissions("policies:seed"));

        using var provider = services.BuildServiceProvider(Strict);
        provider.GetRequiredService<IServiceIdentityCatalog>().TryGet("encina.seeding", out var definition).ShouldBeTrue();
        definition!.IsBuiltIn.ShouldBeTrue();
    }

    [Fact]
    public async Task AnIdenticalRedeclaration_StartsTheHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddEncinaServiceIdentity("billing", id => id.WithRoles("r"));
        builder.Services.AddEncinaServiceIdentity("billing", id => id.WithRoles("r"));
        using var host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AConflictingRedeclaration_FailsStartup()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddEncinaServiceIdentity("billing", id => id.WithRoles("r"));
        builder.Services.AddEncinaServiceIdentity("billing", id => id.WithRoles("admin"));
        using var host = builder.Build();

        await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ACustomAccessor_FailsStartup_WithTheUnsupportedAccessorMessage()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(Substitute.For<IRequestContextAccessor>());
        builder.Services.AddEncinaRequestIdentity();
        using var host = builder.Build();

        var failure = await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));

        failure.Message.ShouldContain("IRequestContextAccessor");
    }

    [Fact]
    public void ThePerTokenValidator_RejectsAuthorityClaims_AndBlankEntries()
    {
        var options = new RequestIdentityOptions();
        options.PerTokenClaimTypes.Add("amr");
        options.PerTokenClaimTypes.Add("SUB");
        options.PerTokenClaimTypes.Add(" ");

        var result = new RequestIdentityOptionsValidator().Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.Failures!.ShouldContain(f => f.Contains("amr") && f.Contains("SUB"));
        result.Failures!.ShouldContain(f => f.Contains("blank"));
    }

    [Fact]
    public void ThePerTokenDefaults_ExcludeAuthTimeAmrAndAcr()
    {
        RequestIdentityOptions.DefaultPerTokenClaimTypes.ShouldContain("nonce");
        RequestIdentityOptions.DefaultPerTokenClaimTypes.ShouldContain("C_HASH");
        RequestIdentityOptions.DefaultPerTokenClaimTypes.ShouldNotContain("auth_time");
        RequestIdentityOptions.DefaultPerTokenClaimTypes.ShouldNotContain("amr");
        new RequestIdentityOptions().PerTokenClaimTypes.Count.ShouldBe(10);
        new RequestIdentityOptionsValidator().Validate(null, new RequestIdentityOptions()).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void TestIdentityService_BuildsAServiceIdentityWithoutAnIssuer()
    {
        var identity = TestIdentity.Service("nightly", roles: ["r"], permissions: ["p"]);

        identity.Kind.ShouldBe(IdentityKind.Service);
        identity.UserId.ShouldBe("service:nightly");
        identity.Issuer.ShouldBeNull();
        identity.Roles.ShouldBe(["r"]);
        Should.Throw<ArgumentException>(() => TestIdentity.Service("Bad Name"));
    }
}
