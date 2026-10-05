using Encina.Testing.Identity;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// Unit tests for the identity carried by <see cref="RequestContext"/>: factories, copies, nested
/// dispatch, the <c>UserId</c> projection and redaction.
/// </summary>
public sealed class RequestContextIdentityTests
{
    private static readonly DateTimeOffset Stamp = new(2026, 10, 5, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateAnonymousAt_IsAnonymous_WithTheGivenValues()
    {
        var context = RequestContext.CreateAnonymousAt(Stamp, "corr-1", "tenant-1", "key-1");

        context.Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        context.UserId.ShouldBeNull();
        context.Timestamp.ShouldBe(Stamp);
        context.CorrelationId.ShouldBe("corr-1");
        context.TenantId.ShouldBe("tenant-1");
        context.IdempotencyKey.ShouldBe("key-1");
        context.CausationId.ShouldBeNull();
        ((RequestContext)context).Origin.ShouldBe(RequestOrigin.Unspecified);
    }

    [Fact]
    public void Create_IsAnonymous_WithAGeneratedCorrelationId_AndEmptyMetadata()
    {
        var context = RequestContext.Create();

        context.Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        context.CorrelationId.ShouldNotBeNullOrWhiteSpace();
        context.Metadata.ShouldBeEmpty();
        context.TenantId.ShouldBeNull();
        context.IdempotencyKey.ShouldBeNull();
        context.CausationId.ShouldBeNull();
    }

    [Fact]
    public void Create_WithoutAnActivity_GeneratesADistinctCorrelationIdPerCall()
    {
        using var noActivity = new NoCurrentActivity();

        RequestContext.Create().CorrelationId.ShouldNotBe(RequestContext.Create().CorrelationId);
    }

    [Fact]
    public void Create_UnderAnActivity_UsesItsIdAsTheCorrelationId()
    {
        using var activity = new System.Diagnostics.Activity("create-test").Start();

        RequestContext.Create().CorrelationId.ShouldBe(activity.Id);
    }

    private sealed class NoCurrentActivity : IDisposable
    {
        private readonly System.Diagnostics.Activity? _previous = System.Diagnostics.Activity.Current;

        public NoCurrentActivity() => System.Diagnostics.Activity.Current = null;

        public void Dispose() => System.Diagnostics.Activity.Current = _previous;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateAnonymousAt_RejectsABlankCorrelationId(string? correlationId)
    {
        Should.Throw<ArgumentException>(() => RequestContext.CreateAnonymousAt(Stamp, correlationId!));
    }

    [Fact]
    public void CreateAt_CarriesTheIdentity_AndUserIdProjectsIt()
    {
        var identity = TestIdentity.User("alice");

        var context = RequestContext.CreateAt(Stamp, "corr-1", identity);

        context.Identity.ShouldBeSameAs(identity);
        context.UserId.ShouldBe("alice");
        context.Timestamp.ShouldBe(Stamp);
    }

    [Fact]
    public void CreateAt_RejectsANullIdentity()
    {
        Should.Throw<ArgumentNullException>(() => RequestContext.CreateAt(Stamp, "corr-1", null!));
    }

    [Fact]
    public void Create_And_CreateForTest_AreAnonymous()
    {
        RequestContext.Create().Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        var test = RequestContext.CreateForTest(tenantId: "t", idempotencyKey: "k");
        test.Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        test.CorrelationId.ShouldStartWith("test-");
        test.TenantId.ShouldBe("t");
    }

    [Fact]
    public void WithMethods_KeepTheIdentityCausationAndOrigin()
    {
        var identity = TestIdentity.User("alice");
        var context = RequestContext.CreateAt(Stamp, "corr-1", identity).WithCausationId("cause-1").WithOrigin(RequestOrigin.Inbound);

        IRequestContext[] copies =
        [
            context.WithTenantId("t"),
            context.WithIdempotencyKey("k"),
            context.WithMetadata("m", 1)
        ];

        foreach (var copy in copies)
        {
            copy.Identity.ShouldBeSameAs(identity);
            copy.CausationId.ShouldBe("cause-1");
            ((RequestContext)copy).Origin.ShouldBe(RequestOrigin.Inbound);
        }
    }

    [Fact]
    public void WithIdentity_ReturnsACopy_AndLeavesTheOriginalUnchanged()
    {
        var original = (RequestContext)RequestContext.CreateAnonymousAt(Stamp, "corr-1");
        var identity = TestIdentity.User("alice");

        var copy = original.WithIdentity(identity);

        copy.Identity.ShouldBeSameAs(identity);
        copy.CorrelationId.ShouldBe("corr-1");
        original.Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        Should.Throw<ArgumentNullException>(() => original.WithIdentity(null!));
    }

    [Fact]
    public void ForNestedDispatch_KeepsIdentityCausationAndOrigin_AndDropsTheIdempotencyKey()
    {
        var identity = TestIdentity.User("alice");
        var parent = RequestContext.CreateAt(Stamp, "corr-1", identity, "tenant-1", "key-1")
            .WithCausationId("cause-1")
            .WithOrigin(RequestOrigin.Scope);
        var later = Stamp.AddSeconds(5);

        var nested = (RequestContext)RequestContext.ForNestedDispatch(parent, later);

        nested.Identity.ShouldBeSameAs(identity);
        nested.CausationId.ShouldBe("cause-1");
        nested.Origin.ShouldBe(RequestOrigin.Scope);
        nested.TenantId.ShouldBe("tenant-1");
        nested.IdempotencyKey.ShouldBeNull();
        nested.Timestamp.ShouldBe(later);
        nested.IsNestedDispatch().ShouldBeTrue();
    }

    [Fact]
    public void CopyOf_AForeignContextWithANullIdentity_ReadsAsAnonymous()
    {
        var foreign = Substitute.For<IRequestContext>();
        foreign.CorrelationId.Returns("corr-x");
        foreign.Metadata.Returns(new Dictionary<string, object?>());

        var copy = RequestContext.CopyOf(foreign);

        copy.Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        copy.CorrelationId.ShouldBe("corr-x");
        foreign.UserId.ShouldBeNull();
    }

    [Fact]
    public void CopyOf_ARequestContext_ReturnsItself()
    {
        var context = (RequestContext)RequestContext.CreateAnonymousAt(Stamp, "corr-1");

        RequestContext.CopyOf(context).ShouldBeSameAs(context);
        Should.Throw<ArgumentNullException>(() => RequestContext.CopyOf(null!));
    }

    [Fact]
    public void ToString_PrintsTheIdentityKind_NeverTheUserId()
    {
        var text = TestRequestContext.For(TestIdentity.User("sentinel-user-41d0"), tenantId: "t1").ToString()!;

        text.ShouldContain("IdentityKind = User");
        text.ShouldNotContain("sentinel-user-41d0");
    }

    [Fact]
    public void TestRequestContext_WithIdentity_CopiesTheContext()
    {
        var source = RequestContext.CreateForTest(tenantId: "t1", correlationId: "corr-1");

        var copy = TestRequestContext.WithIdentity(source, TestIdentity.User("alice"));

        copy.UserId.ShouldBe("alice");
        copy.TenantId.ShouldBe("t1");
        copy.CorrelationId.ShouldBe("corr-1");
        source.UserId.ShouldBeNull();
    }

    [Fact]
    public void TestIdentity_User_BuildsAnAuthenticatedPrincipal()
    {
        var identity = TestIdentity.User("alice", roles: ["admin"], permissions: ["orders:read"], claims: [new System.Security.Claims.Claim("dept", "sales")]);

        identity.Principal!.Identity!.IsAuthenticated.ShouldBeTrue();
        identity.HasClaim("sub", "alice").ShouldBeTrue();
        identity.HasClaim("dept", "sales").ShouldBeTrue();
        identity.Roles.ShouldContain("admin");
        identity.Permissions.ShouldContain("orders:read");
        TestIdentity.Anonymous.ShouldBeSameAs(RequestIdentity.Anonymous);
    }
}
