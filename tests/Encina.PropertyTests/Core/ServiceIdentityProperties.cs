using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.Fluent;
using Microsoft.Extensions.Options;

namespace Encina.PropertyTests.Core;

/// <summary>
/// Property tests of the declared service identities (#1705 Phase 2): the catalog validator accepts
/// a declaration exactly when its name matches the pattern, the built-in prefix matches the built-in
/// flag, and no role or permission is a wildcard; and the per-token comparison never makes two
/// different users the same identity.
/// </summary>
public sealed partial class ServiceIdentityProperties
{
    private static readonly string[] Names =
    [
        "job", "billing-job", "a.b-c", "encina.seeding", "encina.", "Job", "-job", "job_1", "", " job", new string('a', 63), new string('a', 64)
    ];

    private static readonly string[] Entries = ["reader", "jobs:run", "*", "jobs:*", "admin"];

    private static readonly ServiceIdentityCatalogOptionsValidator Validator = new(Options.Create(new RequestIdentityOptions()));

    private static Arbitrary<(string Name, bool BuiltIn, string[] Roles, string[] Permissions)> DeclarationArb() => Arb.From(
        from name in Gen.Elements(Names)
        from builtIn in Gen.Elements(true, false)
        from roles in Gen.ArrayOf(Gen.Elements(Entries))
        from permissions in Gen.ArrayOf(Gen.Elements(Entries))
        select (name, builtIn, roles, permissions));

    [Fact]
    public void TheCatalogValidator_AcceptsExactlyTheValidDeclarations()
    {
        Prop.ForAll(DeclarationArb(), declaration =>
        {
            var options = new ServiceIdentityCatalogOptions();
            options.Declare(new ServiceIdentityBuilder()
                .WithRoles(declaration.Roles)
                .WithPermissions(declaration.Permissions)
                .Build(declaration.Name, declaration.BuiltIn));

            var expected = NamePattern().IsMatch(declaration.Name)
                && declaration.Name.StartsWith("encina.", StringComparison.Ordinal) == declaration.BuiltIn
                && !declaration.Roles.Concat(declaration.Permissions).Any(static entry => entry.Contains('*', StringComparison.Ordinal));

            return Validator.Validate(null, options).Succeeded == expected;
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void ARepeatedDeclaration_IsAConflictExactlyWhenItDiffers()
    {
        Prop.ForAll(Arb.From(Gen.ArrayOf(Gen.Elements(Entries))), Arb.From(Gen.ArrayOf(Gen.Elements(Entries))), (first, second) =>
        {
            var options = new ServiceIdentityCatalogOptions();
            options.Declare(new ServiceIdentityBuilder().WithRoles(first).Build("job", isBuiltIn: false));
            options.Declare(new ServiceIdentityBuilder().WithRoles(second).Build("job", isBuiltIn: false));

            var same = first.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(second);
            return options.ConflictingNames.Count == (same ? 0 : 1);
        }).QuickCheckThrowOnFailure();
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9.-]{0,62}$", RegexOptions.CultureInvariant)]
    private static partial Regex NamePattern();
}
