using Encina.AspNetCore;
using Encina.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.GuardTests.AspNetCore;

/// <summary>
/// Guard clauses of the HTTP request identity integration (#1705, Phase 3): the request middleware,
/// the <see cref="InboundRequestInfo"/> builder, the authorization behavior and the registrations.
/// </summary>
[Trait("Category", "Guard")]
public sealed class RequestIdentityHttpGuardTests
{
    private static readonly IOptions<EncinaAspNetCoreOptions> MiddlewareOptions = Options.Create(new EncinaAspNetCoreOptions());

    [Fact]
    public void Middleware_NullNext_Throws() =>
        Should.Throw<ArgumentNullException>(() => new EncinaContextMiddleware(null!, MiddlewareOptions, NullLogger<EncinaContextMiddleware>.Instance))
            .ParamName.ShouldBe("next");

    [Fact]
    public void Middleware_NullOptions_Throws() =>
        Should.Throw<ArgumentNullException>(() => new EncinaContextMiddleware(_ => Task.CompletedTask, null!, NullLogger<EncinaContextMiddleware>.Instance))
            .ParamName.ShouldBe("options");

    [Fact]
    public void Middleware_NullLogger_Throws() =>
        Should.Throw<ArgumentNullException>(() => new EncinaContextMiddleware(_ => Task.CompletedTask, MiddlewareOptions, null!))
            .ParamName.ShouldBe("logger");

    [Fact]
    public async Task Middleware_InvokeAsync_NullArguments_Throw()
    {
        var middleware = new EncinaContextMiddleware(_ => Task.CompletedTask, MiddlewareOptions, NullLogger<EncinaContextMiddleware>.Instance);

        (await Should.ThrowAsync<ArgumentNullException>(() => middleware.InvokeAsync(null!, Substitute.For<IInternalRequestContextScopeFactory>())))
            .ParamName.ShouldBe("context");
        (await Should.ThrowAsync<ArgumentNullException>(() => middleware.InvokeAsync(new DefaultHttpContext(), null!)))
            .ParamName.ShouldBe("scopes");
    }

    [Fact]
    public void CreateInboundRequestInfo_NullContext_Throws() =>
        Should.Throw<ArgumentNullException>(() => HttpContextInboundRequestExtensions.CreateInboundRequestInfo(null!))
            .ParamName.ShouldBe("context");

    [Fact]
    public void CreateInboundRequestInfo_ValidContext_CopiesThePrincipal()
    {
        var context = new DefaultHttpContext();

        context.CreateInboundRequestInfo().Principal.ShouldBeSameAs(context.User);
    }

    [Fact]
    public void AuthorizationBehavior_NullArguments_Throw()
    {
        var service = Substitute.For<IAuthorizationService>();
        var options = Options.Create(new AuthorizationConfiguration());
        var logger = NullLogger<AuthorizationPipelineBehavior<GuardRequest, string>>.Instance;

        Should.Throw<ArgumentNullException>(() => new AuthorizationPipelineBehavior<GuardRequest, string>(null!, options, logger)).ParamName.ShouldBe("authorizationService");
        Should.Throw<ArgumentNullException>(() => new AuthorizationPipelineBehavior<GuardRequest, string>(service, null!, logger)).ParamName.ShouldBe("options");
        Should.Throw<ArgumentNullException>(() => new AuthorizationPipelineBehavior<GuardRequest, string>(service, options, null!)).ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task AuthorizationBehavior_Handle_NullContextOrNext_Throw()
    {
        var behavior = new AuthorizationPipelineBehavior<GuardRequest, string>(
            Substitute.For<IAuthorizationService>(),
            Options.Create(new AuthorizationConfiguration()),
            NullLogger<AuthorizationPipelineBehavior<GuardRequest, string>>.Instance);

        (await Should.ThrowAsync<ArgumentNullException>(async () => await behavior.Handle(new GuardRequest(), null!, () => default, CancellationToken.None)))
            .ParamName.ShouldBe("context");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await behavior.Handle(new GuardRequest(), RequestContext.CreateForTest(), null!, CancellationToken.None)))
            .ParamName.ShouldBe("nextStep");
    }

    [Fact]
    public async Task ResourceAuthorizer_NullArguments_Throw()
    {
        var service = Substitute.For<IAuthorizationService>();
        var accessor = Substitute.For<IRequestContextAccessor>();
        Should.Throw<ArgumentNullException>(() => new ResourceAuthorizer(null!, accessor)).ParamName.ShouldBe("authorizationService");
        Should.Throw<ArgumentNullException>(() => new ResourceAuthorizer(service, null!)).ParamName.ShouldBe("requestContextAccessor");

        var authorizer = new ResourceAuthorizer(service, accessor);
        await Should.ThrowAsync<ArgumentNullException>(() => authorizer.AuthorizeAsync<object>(null!, "policy", CancellationToken.None));
        await Should.ThrowAsync<ArgumentException>(() => authorizer.AuthorizeAsync(new object(), " ", CancellationToken.None));
        accessor.RequestContext.Returns((IRequestContext?)null);
        (await authorizer.AuthorizeAsync((object)new GuardRequest(), "policy", CancellationToken.None)).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Registrations_NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => ((IServiceCollection)null!).AddEncinaAspNetCore(_ => { })).ParamName.ShouldBe("services");
        Should.Throw<ArgumentNullException>(() => new ServiceCollection().AddEncinaAspNetCore(null!)).ParamName.ShouldBe("configureOptions");
        Should.Throw<ArgumentNullException>(() => ((IServiceCollection)null!).AddEncinaAuthorization()).ParamName.ShouldBe("services");
    }

    public sealed record GuardRequest : IRequest<string>;
}
