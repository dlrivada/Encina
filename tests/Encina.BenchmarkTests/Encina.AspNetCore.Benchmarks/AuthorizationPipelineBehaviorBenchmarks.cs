using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Encina.AspNetCore.Authorization;
using Encina.UnitTests.AspNetCore;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.AspNetCore.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="AuthorizationPipelineBehavior{TRequest, TResponse}"/>.
/// Measures performance impact of authorization checks in the pipeline.
/// </summary>
[MemoryDiagnoser]
[MarkdownExporter]
public class AuthorizationPipelineBehaviorBenchmarks
{
    private AuthorizationPipelineBehavior<UnauthorizedRequest, string> _noAuthBehavior = null!;
    private AuthorizationPipelineBehavior<AuthorizedRequest, string> _authBehavior = null!;
    private AuthorizationPipelineBehavior<RoleBasedRequest, string> _roleBehavior = null!;
    private AuthorizationPipelineBehavior<PolicyBasedRequest, string> _policyBehavior = null!;
    private UnauthorizedRequest _unauthorizedRequest = null!;
    private AuthorizedRequest _authorizedRequest = null!;
    private RoleBasedRequest _roleBasedRequest = null!;
    private PolicyBasedRequest _policyBasedRequest = null!;
    private IRequestContext _context = null!;
    private RequestHandlerCallback<string> _nextStep = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup requests
        _unauthorizedRequest = new UnauthorizedRequest();
        _authorizedRequest = new AuthorizedRequest();
        _roleBasedRequest = new RoleBasedRequest();
        _policyBasedRequest = new PolicyBasedRequest();

        // Setup context
        _context = RequestContext.CreateForTest();

        // Setup next step
        _nextStep = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        // Setup behaviors
        var httpContextAccessor = new HttpContextAccessor();
        var authService = new TestAuthorizationService(shouldSucceed: true);
        var options = Options.Create(new AuthorizationConfiguration());

        // No authorization required
        httpContextAccessor.HttpContext = null;
        _noAuthBehavior = new AuthorizationPipelineBehavior<UnauthorizedRequest, string>(
            authService, httpContextAccessor, options,
            NullLogger<AuthorizationPipelineBehavior<UnauthorizedRequest, string>>.Instance);

        // Simple authentication
        httpContextAccessor.HttpContext = CreateAuthenticatedContext("user-123");
        _authBehavior = new AuthorizationPipelineBehavior<AuthorizedRequest, string>(
            authService, httpContextAccessor, options,
            NullLogger<AuthorizationPipelineBehavior<AuthorizedRequest, string>>.Instance);

        // Role-based authorization
        httpContextAccessor.HttpContext = CreateAuthenticatedContext("user-123", roles: ["Admin"]);
        _roleBehavior = new AuthorizationPipelineBehavior<RoleBasedRequest, string>(
            authService, httpContextAccessor, options,
            NullLogger<AuthorizationPipelineBehavior<RoleBasedRequest, string>>.Instance);

        // Policy-based authorization
        httpContextAccessor.HttpContext = CreateAuthenticatedContext("user-123");
        _policyBehavior = new AuthorizationPipelineBehavior<PolicyBasedRequest, string>(
            authService, httpContextAccessor, options,
            NullLogger<AuthorizationPipelineBehavior<PolicyBasedRequest, string>>.Instance);
    }

    [Benchmark(Baseline = true)]
    public async Task<Either<EncinaError, string>> NoAuthorization()
    {
        return await _noAuthBehavior.Handle(_unauthorizedRequest, _context, _nextStep, CancellationToken.None);
    }

    [Benchmark]
    public async Task<Either<EncinaError, string>> SimpleAuthentication()
    {
        return await _authBehavior.Handle(_authorizedRequest, _context, _nextStep, CancellationToken.None);
    }

    [Benchmark]
    public async Task<Either<EncinaError, string>> RoleBasedAuthorization()
    {
        return await _roleBehavior.Handle(_roleBasedRequest, _context, _nextStep, CancellationToken.None);
    }

    [Benchmark]
    public async Task<Either<EncinaError, string>> PolicyBasedAuthorization()
    {
        return await _policyBehavior.Handle(_policyBasedRequest, _context, _nextStep, CancellationToken.None);
    }

    // Test types
    private sealed record UnauthorizedRequest : IRequest<string>;

    [Authorize]
    private sealed record AuthorizedRequest : IRequest<string>;

    [Authorize(Roles = "Admin")]
    private sealed record RoleBasedRequest : IRequest<string>;

    [Authorize(Policy = "RequireElevation")]
    private sealed record PolicyBasedRequest : IRequest<string>;

    // Helper method
    private static DefaultHttpContext CreateAuthenticatedContext(string userId, string[]? roles = null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        if (roles != null)
        {
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        return new DefaultHttpContext
        {
            User = principal
        };
    }
}
