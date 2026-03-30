using Encina.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Encina.GuardTests.AspNetCore.Authorization;

public class AuthorizationConfigurationExtensionsGuardTests
{
    // --- AddRolePolicy ---

    [Fact]
    public void AddRolePolicy_NullOptions_Throws()
    {
        AuthorizationOptions options = null!;
        Should.Throw<ArgumentNullException>(() =>
            options.AddRolePolicy("policy", "Admin"));
    }

    [Fact]
    public void AddRolePolicy_NullPolicyName_Throws()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddRolePolicy(null!, "Admin"));
    }

    [Fact]
    public void AddRolePolicy_EmptyPolicyName_Throws()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddRolePolicy("", "Admin"));
    }

    [Fact]
    public void AddRolePolicy_WhitespacePolicyName_Throws()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddRolePolicy("  ", "Admin"));
    }

    [Fact]
    public void AddRolePolicy_NullRoles_Throws()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentNullException>(() =>
            options.AddRolePolicy("policy", null!));
    }

    [Fact]
    public void AddRolePolicy_EmptyRoles_Throws()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddRolePolicy("policy"));
    }

    // --- AddClaimPolicy ---

    [Fact]
    public void AddClaimPolicy_NullOptions_Throws()
    {
        AuthorizationOptions options = null!;
        Should.Throw<ArgumentNullException>(() =>
            options.AddClaimPolicy("policy", "claim"));
    }

    [Fact]
    public void AddClaimPolicy_NullPolicyName_Throws()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddClaimPolicy(null!, "claim"));
    }

    [Fact]
    public void AddClaimPolicy_EmptyPolicyName_Throws()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddClaimPolicy("", "claim"));
    }

    [Fact]
    public void AddClaimPolicy_NullClaimType_Throws()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddClaimPolicy("policy", null!));
    }

    [Fact]
    public void AddClaimPolicy_EmptyClaimType_Throws()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddClaimPolicy("policy", ""));
    }

    [Fact]
    public void AddClaimPolicy_WhitespaceClaimType_Throws()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddClaimPolicy("policy", "  "));
    }

    // --- AddAuthenticatedPolicy ---

    [Fact]
    public void AddAuthenticatedPolicy_NullOptions_Throws()
    {
        AuthorizationOptions options = null!;
        Should.Throw<ArgumentNullException>(() =>
            options.AddAuthenticatedPolicy("policy"));
    }

    [Fact]
    public void AddAuthenticatedPolicy_NullPolicyName_Throws()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddAuthenticatedPolicy(null!));
    }

    [Fact]
    public void AddAuthenticatedPolicy_EmptyPolicyName_Throws()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddAuthenticatedPolicy(""));
    }

    [Fact]
    public void AddAuthenticatedPolicy_WhitespacePolicyName_Throws()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddAuthenticatedPolicy("  "));
    }
}
