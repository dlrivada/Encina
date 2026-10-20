using Encina.AspNetCore;
using Microsoft.AspNetCore.Authorization;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Registration completeness of the ASP.NET Core identity integration (#1705, Phase 3 task 9, minor
/// 6): the method-injected parameter of the middleware is invisible to <c>ValidateOnBuild</c>, so the
/// tests resolve both scope-factory interfaces explicitly.
/// </summary>
public sealed class AspNetCoreIdentityRegistrationTests
{
    private static readonly ServiceProviderOptions Validated = new() { ValidateOnBuild = true, ValidateScopes = true };

    [Fact]
    public void AddEncinaAspNetCore_Alone_ResolvesBothScopeFactoryInterfaces_AsOneSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAspNetCore();

        using var provider = services.BuildServiceProvider(Validated);
        var publicFactory = provider.GetRequiredService<IRequestContextScopeFactory>();
        provider.GetRequiredService<IInternalRequestContextScopeFactory>().ShouldBeSameAs(publicFactory);
        provider.GetRequiredService<IRequestContextAccessor>().ShouldBeOfType<RequestContextAccessor>();
        provider.GetRequiredService<TimeProvider>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaAuthorization_Alone_RegistersAndResolvesTheBehavior()
    {
        // Routing is the web host's own registration (ASP.NET Core's policy cache needs it).
        var services = new ServiceCollection();
        services.AddRouting();

        services.AddEncinaAuthorization();

        using var provider = services.BuildServiceProvider(Validated);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetServices<IPipelineBehavior<ProtectedRequest, Unit>>()
            .ShouldHaveSingleItem()
            .ShouldBeOfType<AuthorizationPipelineBehavior<ProtectedRequest, Unit>>();
    }

    [Fact]
    public void AddEncinaAuthorization_AndCfgAddAuthorization_RegisterTheBehaviorOnce()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina(cfg => cfg.AddAuthorization());

        services.AddEncinaAuthorization();
        services.AddEncinaAuthorization();

        services.Count(static descriptor => descriptor.ImplementationType == typeof(AuthorizationPipelineBehavior<,>)).ShouldBe(1);
    }

    [Fact]
    public void AddEncinaAuthorization_IsRegistered_EvenWhenAnotherBehaviorExists()
    {
        var services = new ServiceCollection();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(OtherBehavior<,>));

        services.AddEncinaAuthorization();

        services.ShouldContain(static descriptor => descriptor.ImplementationType == typeof(AuthorizationPipelineBehavior<,>));
    }

    [Authorize]
    public sealed record ProtectedRequest : ICommand<Unit>;

    private sealed class OtherBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<EncinaError, TResponse>> Handle(TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken) =>
            nextStep();
    }
}
