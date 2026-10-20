using Encina.Testing.Identity;

namespace Encina.ContractTests.Core;

/// <summary>
/// Contract of <see cref="IRequestContext.Identity"/> over every way a <see cref="RequestContext"/>
/// is produced or transformed: the identity is never null, <c>UserId</c> is exactly its projection,
/// the public factories are anonymous, and copies keep the identity.
/// </summary>
public sealed class RequestContextIdentityContractTests
{
    private static readonly DateTimeOffset Stamp = new(2026, 10, 5, 0, 0, 0, TimeSpan.Zero);

    public static TheoryData<string, IRequestContext> AllContexts()
    {
        var user = TestRequestContext.For(TestIdentity.User("contract-user"), tenantId: "t1", idempotencyKey: "k1");
        var anonymous = RequestContext.CreateAnonymousAt(Stamp, "corr-1", "t1", "k1");
        return new TheoryData<string, IRequestContext>
        {
            { "Create", RequestContext.Create() },
            { "CreateAnonymousAt", anonymous },
            { "CreateForTest", RequestContext.CreateForTest(tenantId: "t1") },
            { "TestRequestContext.For(user)", user },
            { "TestRequestContext.WithIdentity", TestRequestContext.WithIdentity(anonymous, TestIdentity.User("other-user")) },
            { "WithTenantId(user)", user.WithTenantId("t2") },
            { "WithIdempotencyKey(user)", user.WithIdempotencyKey("k2") },
            { "WithMetadata(user)", user.WithMetadata("m", 1) },
            { "ForNestedDispatch(user)", RequestContext.ForNestedDispatch(user, Stamp) },
            { "ForNestedDispatch(anonymous)", RequestContext.ForNestedDispatch(anonymous, Stamp) },
        };
    }

    [Theory]
    [MemberData(nameof(AllContexts))]
    public void Identity_IsNeverNull_AndUserIdIsItsProjection(string producedBy, IRequestContext context)
    {
        context.Identity.ShouldNotBeNull(producedBy);
        context.UserId.ShouldBe(context.Identity.UserId, producedBy);
        (context.UserId is not null).ShouldBe(context.Identity.IsAuthenticated, producedBy);
    }

    [Fact]
    public void PublicFactories_AreAnonymous()
    {
        RequestContext.Create().Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        RequestContext.CreateAnonymousAt(Stamp, "corr").Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        RequestContext.CreateForTest().Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
    }

    [Fact]
    public void Copies_KeepTheIdentityInstance()
    {
        var identity = TestIdentity.User("contract-user");
        var context = TestRequestContext.For(identity);

        context.WithTenantId("t").Identity.ShouldBeSameAs(identity);
        context.WithIdempotencyKey("k").Identity.ShouldBeSameAs(identity);
        context.WithMetadata("m", "v").Identity.ShouldBeSameAs(identity);
        RequestContext.ForNestedDispatch(context, Stamp).Identity.ShouldBeSameAs(identity);
    }

    [Fact]
    public void ToString_NeverPrintsTheUserId()
    {
        TestRequestContext.For(TestIdentity.User("contract-sentinel")).ToString()!.ShouldNotContain("contract-sentinel");
    }
}
