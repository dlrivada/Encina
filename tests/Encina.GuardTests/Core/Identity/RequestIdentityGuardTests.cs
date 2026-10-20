using Encina.Testing.Identity;
using Microsoft.Extensions.Options;

namespace Encina.GuardTests.Core.Identity;

/// <summary>
/// Guard clauses of the request identity model.
/// </summary>
public sealed class RequestIdentityGuardTests
{
    private static readonly DateTimeOffset Stamp = new(2026, 10, 5, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ForUser_NullUserId_Throws() =>
        Should.Throw<ArgumentNullException>(() => RequestIdentity.ForUser(null!));

    [Fact]
    public void HasClaim_NullType_Throws() =>
        Should.Throw<ArgumentNullException>(() => RequestIdentity.Anonymous.HasClaim(null!));

    [Fact]
    public void ToPersisted_NullCorrelationId_Throws() =>
        Should.Throw<ArgumentNullException>(() => RequestIdentity.Anonymous.ToPersisted(null, null!, null));

    [Fact]
    public void ClaimsRequestIdentityFactory_NullOptions_Throws() =>
        Should.Throw<ArgumentNullException>(() => new ClaimsRequestIdentityFactory(null!));

    [Fact]
    public void ClaimsRequestIdentityFactory_NullPrincipal_IsAnonymousNotAnException()
    {
        var factory = new ClaimsRequestIdentityFactory(Options.Create(new RequestIdentityOptions()));

        factory.Create(null).ShouldBeSameAs(RequestIdentity.Anonymous);
        factory.ResolveTenantId(null).ShouldBeNull();
    }

    [Fact]
    public void Validator_NullOptions_Throws() =>
        Should.Throw<ArgumentNullException>(() => new RequestIdentityOptionsValidator().Validate(null, null!));

    [Fact]
    public void AddEncinaRequestIdentity_NullServices_Throws() =>
        Should.Throw<ArgumentNullException>(() => RequestIdentityServiceCollectionExtensions.AddEncinaRequestIdentity(null!));

    [Fact]
    public void CreateAnonymousAt_NullCorrelationId_Throws() =>
        Should.Throw<ArgumentNullException>(() => RequestContext.CreateAnonymousAt(Stamp, null!));

    [Fact]
    public void CreateAt_NullIdentity_Throws() =>
        Should.Throw<ArgumentNullException>(() => RequestContext.CreateAt(Stamp, "corr", null!));

    [Fact]
    public void WithIdentity_NullIdentity_Throws() =>
        Should.Throw<ArgumentNullException>(() => ((RequestContext)RequestContext.CreateAnonymousAt(Stamp, "corr")).WithIdentity(null!));

    [Fact]
    public void CopyOf_NullSource_Throws() =>
        Should.Throw<ArgumentNullException>(() => RequestContext.CopyOf(null!));

    [Fact]
    public void AccessorPushAndEnd_Null_Throw()
    {
        Should.Throw<ArgumentNullException>(() => RequestContextAccessor.Push(null!));
        Should.Throw<ArgumentNullException>(() => RequestContextAccessor.End(null!));
    }

    [Fact]
    public void TestIdentity_User_NullUserId_Throws() =>
        Should.Throw<ArgumentNullException>(() => TestIdentity.User(null!));

    [Fact]
    public void TestRequestContext_For_NullIdentity_Throws() =>
        Should.Throw<ArgumentNullException>(() => TestRequestContext.For(null!));

    [Fact]
    public void TestRequestContext_WithIdentity_NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => TestRequestContext.WithIdentity(null!, RequestIdentity.Anonymous));
        Should.Throw<ArgumentNullException>(() => TestRequestContext.WithIdentity(RequestContext.CreateForTest(), null!));
    }
}
