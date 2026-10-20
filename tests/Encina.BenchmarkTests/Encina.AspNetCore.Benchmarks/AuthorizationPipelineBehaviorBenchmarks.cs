using BenchmarkDotNet.Attributes;
using Encina.AspNetCore.Authorization;
using Encina.Testing.Identity;
using Encina.UnitTests.AspNetCore;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.AspNetCore.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="AuthorizationPipelineBehavior{TRequest, TResponse}"/>.
/// Measures performance impact of authorization checks in the pipeline; the caller is the request
/// identity the context carries (#1705).
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
    private IRequestContext _anonymousContext = null!;
    private IRequestContext _userContext = null!;
    private IRequestContext _adminContext = null!;
    private RequestHandlerCallback<string> _nextStep = null!;

    [GlobalSetup]
    public void Setup()
    {
        _unauthorizedRequest = new UnauthorizedRequest();
        _authorizedRequest = new AuthorizedRequest();
        _roleBasedRequest = new RoleBasedRequest();
        _policyBasedRequest = new PolicyBasedRequest();

        _anonymousContext = RequestContext.CreateForTest();
        _userContext = TestRequestContext.For(TestIdentity.User("user-123"));
        _adminContext = TestRequestContext.For(TestIdentity.User("user-123", ["Admin"]));

        _nextStep = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        var authService = new TestAuthorizationService(shouldSucceed: true);
        var options = Options.Create(new AuthorizationConfiguration());

        _noAuthBehavior = new AuthorizationPipelineBehavior<UnauthorizedRequest, string>(
            authService, options, NullLogger<AuthorizationPipelineBehavior<UnauthorizedRequest, string>>.Instance);
        _authBehavior = new AuthorizationPipelineBehavior<AuthorizedRequest, string>(
            authService, options, NullLogger<AuthorizationPipelineBehavior<AuthorizedRequest, string>>.Instance);
        _roleBehavior = new AuthorizationPipelineBehavior<RoleBasedRequest, string>(
            authService, options, NullLogger<AuthorizationPipelineBehavior<RoleBasedRequest, string>>.Instance);
        _policyBehavior = new AuthorizationPipelineBehavior<PolicyBasedRequest, string>(
            authService, options, NullLogger<AuthorizationPipelineBehavior<PolicyBasedRequest, string>>.Instance);
    }

    [Benchmark(Baseline = true)]
    public async Task<Either<EncinaError, string>> NoAuthorization()
    {
        return await _noAuthBehavior.Handle(_unauthorizedRequest, _anonymousContext, _nextStep, CancellationToken.None);
    }

    [Benchmark]
    public async Task<Either<EncinaError, string>> SimpleAuthentication()
    {
        return await _authBehavior.Handle(_authorizedRequest, _userContext, _nextStep, CancellationToken.None);
    }

    [Benchmark]
    public async Task<Either<EncinaError, string>> RoleBasedAuthorization()
    {
        return await _roleBehavior.Handle(_roleBasedRequest, _adminContext, _nextStep, CancellationToken.None);
    }

    [Benchmark]
    public async Task<Either<EncinaError, string>> PolicyBasedAuthorization()
    {
        return await _policyBehavior.Handle(_policyBasedRequest, _userContext, _nextStep, CancellationToken.None);
    }

    // Test types
    private sealed record UnauthorizedRequest : IRequest<string>;

    [Authorize]
    private sealed record AuthorizedRequest : IRequest<string>;

    [Authorize(Roles = "Admin")]
    private sealed record RoleBasedRequest : IRequest<string>;

    [Authorize(Policy = "RequireElevation")]
    private sealed record PolicyBasedRequest : IRequest<string>;
}
