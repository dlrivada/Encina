using System.Security.Claims;
using Encina.AspNetCore;
using Encina.AspNetCore.Authorization;
using Encina.Testing;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Authorization;

/// <summary>
/// Integration tests that verify the full authorization pipeline using real
/// ASP.NET Core authorization infrastructure (real policy evaluation, real
/// <see cref="IAuthorizationService"/>, real <see cref="AuthorizationHandler{TRequirement}"/>).
/// </summary>
[Trait("Category", "Integration")]
public class PolicyBasedAuthorizationTests : IAsyncLifetime
{
    private IServiceProvider _serviceProvider = null!;

    public ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddHttpContextAccessor();

        // Register Encina authorization with CQRS defaults
        services.AddEncinaAuthorization(
            auth =>
            {
                auth.AutoApplyPolicies = true;
                auth.DefaultCommandPolicy = "RequireAuthenticated";
                auth.DefaultQueryPolicy = "RequireAuthenticated";
            },
            policies =>
            {
                policies.AddRolePolicy("AdminOnly", "Admin");
                policies.AddClaimPolicy("SalesDepartment", "department", "sales");
                policies.AddAuthenticatedPolicy("MustBeLoggedIn");

                // Resource-based policy with custom handler
                policies.AddPolicy("CanEditOrder", policy =>
                    policy.Requirements.Add(new OrderOwnerRequirement()));
            });

        // Register our custom authorization handler
        services.AddSingleton<IAuthorizationHandler, OrderOwnerAuthorizationHandler>();

        _serviceProvider = services.BuildServiceProvider();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    // ── IResourceAuthorizer integration ─────────────────────────────

    [Fact]
    public async Task ResourceAuthorizer_PolicySucceeds_ReturnsRightTrue()
    {
        // Arrange
        var (authorizer, _) = CreateScopedAuthorizer("user-123", roles: ["Admin"]);
        var resource = new TestOrder("order-1", "user-123");

        // Act
        var result = await authorizer.AuthorizeAsync(resource, "CanEditOrder", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(value => value.ShouldBeTrue());
    }

    [Fact]
    public async Task ResourceAuthorizer_PolicyFails_ReturnsLeftResourceDenied()
    {
        // Arrange - user-456 is NOT the owner of the order
        var (authorizer, _) = CreateScopedAuthorizer("user-456");
        var resource = new TestOrder("order-1", "user-123");

        // Act
        var result = await authorizer.AuthorizeAsync(resource, "CanEditOrder", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationResourceDenied),
                None: () => Assert.Fail("Expected error code"));
        });
    }

    [Fact]
    public async Task ResourceAuthorizer_UnauthenticatedUser_ReturnsLeftUnauthorized()
    {
        // Arrange - unauthenticated
        var (authorizer, _) = CreateScopedAuthorizer(null);
        var resource = new TestOrder("order-1", "user-123");

        // Act
        var result = await authorizer.AuthorizeAsync(resource, "CanEditOrder", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationUnauthorized),
                None: () => Assert.Fail("Expected error code"));
        });
    }

    // ── Pipeline Behavior with real authorization infrastructure ─────

    [Fact]
    public async Task Pipeline_AdminOnlyCommand_UserIsAdmin_Succeeds()
    {
        // Arrange
        var behavior = CreateBehavior<AdminCommand, Unit>("user-1", roles: ["Admin"]);
        var request = new AdminCommand();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Pipeline_AdminOnlyCommand_UserIsNotAdmin_ReturnsForbidden()
    {
        // Arrange
        var behavior = CreateBehavior<AdminCommand, Unit>("user-1", roles: ["User"]);
        var request = new AdminCommand();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<Unit> nextStep = () =>
            ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationForbidden),
                None: () => Assert.Fail("Expected error code"));
        });
    }

    [Fact]
    public async Task Pipeline_ResourceAuthorize_OwnerCanEdit()
    {
        // Arrange - user-123 owns the order
        var behavior = CreateBehavior<EditOrderCommand, Unit>("user-123");
        var request = new EditOrderCommand("order-1", "user-123");
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Pipeline_ResourceAuthorize_NonOwnerDenied()
    {
        // Arrange - user-456 does NOT own the order
        var behavior = CreateBehavior<EditOrderCommand, Unit>("user-456");
        var request = new EditOrderCommand("order-1", "user-123");
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<Unit> nextStep = () =>
            ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationResourceDenied),
                None: () => Assert.Fail("Expected error code"));
        });
    }

    [Fact]
    public async Task Pipeline_AutoApplyPolicy_CommandWithoutAttributes_RequiresAuthentication()
    {
        // Arrange - unauthenticated user, auto-apply is enabled
        var behavior = CreateBehavior<PlainIntegrationCommand, Unit>(null);
        var request = new PlainIntegrationCommand();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<Unit> nextStep = () =>
            ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationUnauthorized),
                None: () => Assert.Fail("Expected error code"));
        });
    }

    [Fact]
    public async Task Pipeline_AutoApplyPolicy_AuthenticatedCommand_Succeeds()
    {
        // Arrange - authenticated user, auto-apply default policy
        var behavior = CreateBehavior<PlainIntegrationCommand, Unit>("user-1");
        var request = new PlainIntegrationCommand();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Pipeline_AutoApplyPolicy_QueryWithoutAttributes_RequiresAuthentication()
    {
        // Arrange - unauthenticated user, auto-apply is enabled
        var behavior = CreateBehavior<PlainIntegrationQuery, string>(null);
        var request = new PlainIntegrationQuery();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult(Right<EncinaError, string>("data"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Pipeline_AllowAnonymous_BypassesAutoApply()
    {
        // Arrange - no auth, but [AllowAnonymous] should bypass
        var behavior = CreateBehavior<PublicIntegrationQuery, string>(null);
        var request = new PublicIntegrationQuery();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<EncinaError, string>("public"));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Pipeline_ClaimPolicy_UserHasClaim_Succeeds()
    {
        // Arrange
        var behavior = CreateBehavior<SalesCommand, Unit>(
            "user-1",
            claims: [new Claim("department", "sales")]);
        var request = new SalesCommand();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Pipeline_ClaimPolicy_UserLacksClaim_ReturnsPolicyFailed()
    {
        // Arrange
        var behavior = CreateBehavior<SalesCommand, Unit>(
            "user-1",
            claims: [new Claim("department", "engineering")]);
        var request = new SalesCommand();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<Unit> nextStep = () =>
            ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationPolicyFailed),
                None: () => Assert.Fail("Expected error code"));
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private (IResourceAuthorizer authorizer, IServiceScope scope) CreateScopedAuthorizer(
        string? userId,
        string[]? roles = null)
    {
        var scope = _serviceProvider.CreateScope();
        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

        if (userId is not null)
        {
            httpContextAccessor.HttpContext = CreateHttpContext(userId, roles);
        }
        else
        {
            httpContextAccessor.HttpContext = new DefaultHttpContext
            {
                RequestServices = scope.ServiceProvider
            };
        }

        var authorizer = scope.ServiceProvider.GetRequiredService<IResourceAuthorizer>();
        return (authorizer, scope);
    }

    private AuthorizationPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        string? userId,
        string[]? roles = null,
        Claim[]? claims = null)
        where TRequest : IRequest<TResponse>
    {
        using var scope = _serviceProvider.CreateScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AuthorizationConfiguration>>();
        var logger = NullLogger<AuthorizationPipelineBehavior<TRequest, TResponse>>.Instance;

        HttpContext? httpContext = null;
        if (userId is not null)
        {
            httpContext = CreateHttpContext(userId, roles, claims);
            httpContext.RequestServices = scope.ServiceProvider;
        }

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        return new AuthorizationPipelineBehavior<TRequest, TResponse>(
            authorizationService,
            httpContextAccessor,
            options,
            logger);
    }

    private static DefaultHttpContext CreateHttpContext(
        string userId,
        string[]? roles = null,
        Claim[]? additionalClaims = null)
    {
        var allClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };

        if (roles is not null)
        {
            allClaims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        }

        if (additionalClaims is not null)
        {
            allClaims.AddRange(additionalClaims);
        }

        var identity = new ClaimsIdentity(allClaims, "Test");
        var principal = new ClaimsPrincipal(identity);

        return new DefaultHttpContext { User = principal };
    }

    // ── Test request types ──────────────────────────────────────────

    private sealed record PlainIntegrationCommand : ICommand<Unit>;
    private sealed record PlainIntegrationQuery : IQuery<string>;

    [AllowAnonymous]
    private sealed record PublicIntegrationQuery : IQuery<string>;

    [Authorize(Roles = "Admin")]
    private sealed record AdminCommand : ICommand<Unit>;

    [Authorize(Policy = "SalesDepartment")]
    private sealed record SalesCommand : ICommand<Unit>;

    [ResourceAuthorize("CanEditOrder")]
    private sealed record EditOrderCommand(string OrderId, string OwnerId) : ICommand<Unit>;

    // ── Custom authorization requirement + handler ──────────────────

    private sealed record TestOrder(string OrderId, string OwnerId);

    private sealed class OrderOwnerRequirement : IAuthorizationRequirement;

    private sealed class OrderOwnerAuthorizationHandler :
        AuthorizationHandler<OrderOwnerRequirement, EditOrderCommand>,
        IAuthorizationHandler
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OrderOwnerRequirement requirement,
            EditOrderCommand resource)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == resource.OwnerId)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        // Also handle TestOrder resources for IResourceAuthorizer tests
        public new async Task HandleAsync(AuthorizationHandlerContext context)
        {
            foreach (var requirement in context.Requirements.OfType<OrderOwnerRequirement>())
            {
                if (context.Resource is TestOrder order)
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (userId == order.OwnerId)
                    {
                        context.Succeed(requirement);
                    }
                }
                else if (context.Resource is EditOrderCommand command)
                {
                    await HandleRequirementAsync(context, requirement, command);
                }
            }
        }
    }
}

file static class IntegrationTestServiceCollectionExtensions
{
    public static IServiceCollection AddEncinaAuthorization(
        this IServiceCollection services,
        Action<AuthorizationConfiguration>? configureAuthorization = null,
        Action<AuthorizationOptions>? configurePolicies = null)
    {
        return global::Encina.AspNetCore.ServiceCollectionExtensions.AddEncinaAuthorization(
            services, configureAuthorization, configurePolicies);
    }
}
