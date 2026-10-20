namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// Unit tests for <see cref="RequestIdentityOptionsValidator"/>.
/// </summary>
public sealed class RequestIdentityOptionsValidatorTests
{
    private readonly RequestIdentityOptionsValidator _validator = new();

    [Fact]
    public void Validate_Defaults_Succeeds()
    {
        _validator.Validate(null, new RequestIdentityOptions()).Succeeded.ShouldBeTrue();
    }

    public static TheoryData<string> Lists =>
    [
        nameof(RequestIdentityOptions.UserIdClaimTypes),
        nameof(RequestIdentityOptions.RoleClaimTypes),
        nameof(RequestIdentityOptions.PermissionClaimTypes),
        nameof(RequestIdentityOptions.TenantIdClaimTypes)
    ];

    [Theory]
    [MemberData(nameof(Lists))]
    public void Validate_AnEmptyList_FailsNamingIt(string listName)
    {
        var options = new RequestIdentityOptions();
        ListOf(options, listName).Clear();

        var result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldHaveSingleItem().ShouldContain(listName);
        result.Failures.Single().ShouldContain("at least one");
    }

    [Theory]
    [MemberData(nameof(Lists))]
    public void Validate_ABlankEntry_FailsNamingTheList(string listName)
    {
        var options = new RequestIdentityOptions();
        ListOf(options, listName).Add(" ");

        var result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldHaveSingleItem().ShouldContain(listName);
        result.Failures.Single().ShouldContain("blank");
    }

    [Fact]
    public void Validate_ReportsEveryBadList()
    {
        var options = new RequestIdentityOptions();
        options.UserIdClaimTypes.Clear();
        options.TenantIdClaimTypes.Add(string.Empty);

        _validator.Validate(null, options).Failures!.Count().ShouldBe(2);
    }

    [Fact]
    public void Validate_RejectsNullOptions()
    {
        Should.Throw<ArgumentNullException>(() => _validator.Validate(null, null!));
    }

    private static IList<string> ListOf(RequestIdentityOptions options, string listName) => listName switch
    {
        nameof(RequestIdentityOptions.UserIdClaimTypes) => options.UserIdClaimTypes,
        nameof(RequestIdentityOptions.RoleClaimTypes) => options.RoleClaimTypes,
        nameof(RequestIdentityOptions.PermissionClaimTypes) => options.PermissionClaimTypes,
        _ => options.TenantIdClaimTypes
    };
}
