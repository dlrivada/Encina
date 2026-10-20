using Microsoft.Extensions.Hosting;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// Registration tests for <see cref="RequestIdentityServiceCollectionExtensions.AddEncinaRequestIdentity"/>
/// and its call from <c>AddEncina</c>.
/// </summary>
public sealed class IdentityRegistrationTests
{
    private static readonly ServiceProviderOptions Strict = new() { ValidateOnBuild = true, ValidateScopes = true };

    [Fact]
    public void AddEncinaRequestIdentity_Alone_ResolvesEveryServiceUnderStrictValidation()
    {
        var services = new ServiceCollection();

        services.AddEncinaRequestIdentity();

        using var provider = services.BuildServiceProvider(Strict);
        provider.GetRequiredService<IRequestContextAccessor>().ShouldBeOfType<RequestContextAccessor>();
        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
        provider.GetRequiredService<IRequestIdentityFactory>().ShouldBeOfType<ClaimsRequestIdentityFactory>();
        provider.GetRequiredService<IOptions<RequestIdentityOptions>>().Value.UserIdClaimTypes[0].ShouldBe("sub");
    }

    [Fact]
    public void AddEncina_RegistersTheRequestIdentityServices()
    {
        var services = new ServiceCollection();

        services.AddEncina();

        using var provider = services.BuildServiceProvider(Strict);
        provider.GetRequiredService<IRequestIdentityFactory>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaRequestIdentity_IsIdempotent_AndAppliesEveryConfigureAction()
    {
        var services = new ServiceCollection();

        services.AddEncinaRequestIdentity(options => options.PermissionClaimTypes.Add("scope"));
        services.AddEncinaRequestIdentity(options => options.PermissionClaimSeparator = ' ');

        services.Count(d => d.ServiceType == typeof(IRequestIdentityFactory)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IValidateOptions<RequestIdentityOptions>)).ShouldBe(1);
        using var provider = services.BuildServiceProvider(Strict);
        var options = provider.GetRequiredService<IOptions<RequestIdentityOptions>>().Value;
        options.PermissionClaimTypes.ShouldContain("scope");
        options.PermissionClaimSeparator.ShouldBe(' ');
    }

    [Fact]
    public void AddEncinaRequestIdentity_KeepsTheApplicationsOwnRegistrations_InAnyOrder()
    {
        var accessor = Substitute.For<IRequestContextAccessor>();
        var time = Substitute.For<TimeProvider>();
        var services = new ServiceCollection();
        services.AddSingleton(accessor);

        services.AddEncinaRequestIdentity();
        services.AddSingleton(time);

        using var provider = services.BuildServiceProvider(Strict);
        provider.GetRequiredService<IRequestContextAccessor>().ShouldBeSameAs(accessor);
        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(time);
    }

    [Fact]
    public void AddEncinaRequestIdentity_RejectsNullServices()
    {
        Should.Throw<ArgumentNullException>(() => RequestIdentityServiceCollectionExtensions.AddEncinaRequestIdentity(null!));
    }

    [Fact]
    public async Task InvalidOptions_FailHostStartup()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddEncinaRequestIdentity(options => options.UserIdClaimTypes.Clear());
        using var host = builder.Build();

        var exception = await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync());

        exception.Message.ShouldContain(nameof(RequestIdentityOptions.UserIdClaimTypes));
    }

    [Fact]
    public async Task ValidOptions_StartTheHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddEncinaRequestIdentity();
        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();
    }
}
