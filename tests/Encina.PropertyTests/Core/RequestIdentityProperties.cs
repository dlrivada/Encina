using System.Security.Claims;
using FsCheck;
using FsCheck.Fluent;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.PropertyTests.Core;

/// <summary>
/// Property tests of the request identity model: whatever principal comes in, the mapped identity
/// keeps the invariant <c>UserId != null</c> if and only if <c>IsAuthenticated</c>, and reserved or
/// malformed subjects never become users. Also covers <see cref="RequestIdentityOptionsValidator"/>.
/// </summary>
public sealed class RequestIdentityProperties
{
    private static readonly ClaimsRequestIdentityFactory Factory =
        new(Options.Create(new RequestIdentityOptions()), NullLogger<ClaimsRequestIdentityFactory>.Instance);

    private static readonly string[] Subjects =
    [
        "alice", "bob", "service:billing", "SERVICE:billing", " service:billing", "alice ", "", "  ", "a\u0001b", "00000000-0000-0000-0000-000000000001"
    ];

    private static readonly string?[] AuthenticationTypes = [null, "bearer", "cookie"];

    private static readonly string[] SubjectClaimTypes = ["sub", ClaimTypes.NameIdentifier, "email"];

    private static Gen<ClaimsIdentity> IdentityGen() =>
        from subject in Gen.Elements(Subjects)
        from claimType in Gen.Elements(SubjectClaimTypes)
        from authenticationType in Gen.Elements(AuthenticationTypes)
        select new ClaimsIdentity([new Claim(claimType, subject)], authenticationType);

    private static Arbitrary<ClaimsPrincipal?> PrincipalArb() => Arb.From(
        Gen.Frequency(
            (1, Gen.Constant<ClaimsPrincipal?>(null)),
            (6, Gen.Select(Gen.ListOf(IdentityGen()), identities => (ClaimsPrincipal?)new ClaimsPrincipal(identities)))));

    [Fact]
    public void MappedIdentity_UserIdIsSetIfAndOnlyIfAuthenticated()
    {
        Prop.ForAll(PrincipalArb(), principal =>
        {
            var identity = Factory.Create(principal);
            return (identity.UserId is not null) == identity.IsAuthenticated;
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void MappedIdentity_IsNeverAServiceAndNeverCarriesAnInvalidUserId()
    {
        Prop.ForAll(PrincipalArb(), principal =>
        {
            var identity = Factory.Create(principal);
            return identity.Kind != IdentityKind.Service
                && (identity.Kind == IdentityKind.Anonymous || RequestIdentity.IsValidUserId(identity.UserId));
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void MappedIdentity_WithoutAnAuthenticatedIdentity_IsAnonymous()
    {
        Prop.ForAll(PrincipalArb(), principal =>
        {
            var hasAuthenticated = principal?.Identities.Any(identity => identity.IsAuthenticated) == true;
            return hasAuthenticated || ReferenceEquals(Factory.Create(principal), RequestIdentity.Anonymous);
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void Validator_FailsExactlyWhenAListIsEmptyOrHasABlankEntry()
    {
        var entries = Gen.Elements("sub", "role", "", " ", "tid");
        var lists = Gen.ListOf(entries);

        Prop.ForAll(Arb.From(Gen.Zip(lists, lists)), pair =>
        {
            var options = new RequestIdentityOptions();
            options.UserIdClaimTypes.Clear();
            options.RoleClaimTypes.Clear();
            foreach (var entry in pair.Item1)
            {
                options.UserIdClaimTypes.Add(entry);
            }

            foreach (var entry in pair.Item2)
            {
                options.RoleClaimTypes.Add(entry);
            }

            static bool Bad(IReadOnlyCollection<string> list) => list.Count == 0 || list.Any(string.IsNullOrWhiteSpace);

            var result = new RequestIdentityOptionsValidator().Validate(null, options);
            return result.Failed == (Bad(pair.Item1) || Bad(pair.Item2));
        }).QuickCheckThrowOnFailure();
    }
}
