using System.Collections.Immutable;
using System.Security.Claims;
using Encina.Security;
using Shouldly;

namespace Encina.UnitTests.Security;

/// <summary>
/// Unit tests for <see cref="SecurityContext"/>.
/// </summary>
public class SecurityContextTests
{
    [Fact]
    public void Constructor_WithValidClaims_ShouldExtractUserId()
    {
        // Arrange
        var principal = CreatePrincipal(("sub", "user-123"));

        // Act
        var context = new SecurityContext(principal);

        // Assert
        context.UserId.ShouldBe("user-123");
    }

    [Fact]
    public void Constructor_WithNameIdentifierFallback_ShouldExtractUserId()
    {
        // Arrange
        var principal = CreatePrincipal((ClaimTypes.NameIdentifier, "user-456"));

        // Act
        var context = new SecurityContext(principal);

        // Assert
        context.UserId.ShouldBe("user-456");
    }

    [Fact]
    public void Constructor_WithCustomUserIdClaimType_ShouldUseConfigured()
    {
        // Arrange
        var principal = CreatePrincipal(("custom_uid", "user-789"));
        var options = new SecurityOptions { UserIdClaimType = "custom_uid" };

        // Act
        var context = new SecurityContext(principal, options);

        // Assert
        context.UserId.ShouldBe("user-789");
    }

    [Fact]
    public void Constructor_WithRoles_ShouldExtractAllRoles()
    {
        // Arrange
        var principal = CreatePrincipal(
            ("role", "Admin"),
            ("role", "Manager"));

        // Act
        var context = new SecurityContext(principal);

        // Assert
        context.Roles.ShouldContain("Admin");
        context.Roles.ShouldContain("Manager");
        context.Roles.Count.ShouldBe(2);
    }

    [Fact]
    public void Constructor_WithClaimTypesRoleFallback_ShouldExtractRoles()
    {
        // Arrange
        var principal = CreatePrincipal((ClaimTypes.Role, "Editor"));

        // Act
        var context = new SecurityContext(principal);

        // Assert
        context.Roles.ShouldContain("Editor");
    }

    [Fact]
    public void Constructor_WithBothRoleClaimTypes_ShouldMergeDeduplicated()
    {
        // Arrange
        var principal = CreatePrincipal(
            ("role", "Admin"),
            (ClaimTypes.Role, "Admin"),
            (ClaimTypes.Role, "Editor"));

        // Act
        var context = new SecurityContext(principal);

        // Assert - ImmutableHashSet deduplicates (case-insensitive)
        context.Roles.Count.ShouldBe(2);
        context.Roles.ShouldContain("Admin");
        context.Roles.ShouldContain("Editor");
    }

    [Fact]
    public void Constructor_WithPermissions_ShouldExtractAllPermissions()
    {
        // Arrange
        var principal = CreatePrincipal(
            ("permission", "orders:read"),
            ("permission", "orders:write"));

        // Act
        var context = new SecurityContext(principal);

        // Assert
        context.Permissions.ShouldContain("orders:read");
        context.Permissions.ShouldContain("orders:write");
        context.Permissions.Count.ShouldBe(2);
    }

    [Fact]
    public void Constructor_WithTenantId_ShouldExtractTenantId()
    {
        // Arrange
        var principal = CreatePrincipal(("tenant_id", "tenant-abc"));

        // Act
        var context = new SecurityContext(principal);

        // Assert
        context.TenantId.ShouldBe("tenant-abc");
    }

    [Fact]
    public void Constructor_WithCustomClaimTypes_ShouldUseAllConfigured()
    {
        // Arrange
        var principal = CreatePrincipal(
            ("uid", "u1"),
            ("grp", "admin"),
            ("perm", "read"),
            ("tid", "t1"));

        var options = new SecurityOptions
        {
            UserIdClaimType = "uid",
            RoleClaimType = "grp",
            PermissionClaimType = "perm",
            TenantIdClaimType = "tid"
        };

        // Act
        var context = new SecurityContext(principal, options);

        // Assert
        context.UserId.ShouldBe("u1");
        context.Roles.ShouldContain("admin");
        context.Permissions.ShouldContain("read");
        context.TenantId.ShouldBe("t1");
    }

    [Fact]
    public void Constructor_WithEmptyClaims_ShouldHaveDefaults()
    {
        // Arrange
        var identity = new ClaimsIdentity("TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var context = new SecurityContext(principal);

        // Assert
        context.IsAuthenticated.ShouldBeTrue();
        context.UserId.ShouldBeNull();
        context.TenantId.ShouldBeNull();
        context.Roles.ShouldBeEmpty();
        context.Permissions.ShouldBeEmpty();
        context.User.ShouldBeSameAs(principal);
    }

    [Fact]
    public void Constructor_WithNullPrincipal_ShouldBeAnonymous()
    {
        // Act
        var context = new SecurityContext(null);

        // Assert
        context.IsAuthenticated.ShouldBeFalse();
        context.UserId.ShouldBeNull();
        context.TenantId.ShouldBeNull();
        context.Roles.ShouldBeEmpty();
        context.Permissions.ShouldBeEmpty();
        context.User.ShouldBeNull();
    }

    [Fact]
    public void Anonymous_ShouldReturnUnauthenticatedContext()
    {
        // Act
        var context = SecurityContext.Anonymous;

        // Assert
        context.IsAuthenticated.ShouldBeFalse();
        context.UserId.ShouldBeNull();
        context.TenantId.ShouldBeNull();
        context.Roles.ShouldBeEmpty();
        context.Permissions.ShouldBeEmpty();
        context.User.ShouldBeNull();
    }

    [Fact]
    public void Roles_ShouldBeCaseInsensitive()
    {
        // Arrange
        var principal = CreatePrincipal(("role", "Admin"));

        // Act
        var context = new SecurityContext(principal);

        // Assert
        context.Roles.Contains("admin").ShouldBeTrue();
        context.Roles.Contains("ADMIN").ShouldBeTrue();
    }

    [Fact]
    public void Permissions_ShouldBeCaseInsensitive()
    {
        // Arrange
        var principal = CreatePrincipal(("permission", "Orders:Read"));

        // Act
        var context = new SecurityContext(principal);

        // Assert
        context.Permissions.Contains("orders:read").ShouldBeTrue();
        context.Permissions.Contains("ORDERS:READ").ShouldBeTrue();
    }

    [Fact]
    public void Roles_ShouldBeImmutable()
    {
        // Arrange
        var principal = CreatePrincipal(("role", "Admin"));
        var context = new SecurityContext(principal);

        // Assert - IReadOnlySet doesn't expose Add/Remove
        context.Roles.ShouldBeAssignableTo<IReadOnlySet<string>>();
        context.Roles.ShouldBeAssignableTo<ImmutableHashSet<string>>();
    }

    [Fact]
    public void Permissions_ShouldBeImmutable()
    {
        // Arrange
        var principal = CreatePrincipal(("permission", "read"));
        var context = new SecurityContext(principal);

        // Assert
        context.Permissions.ShouldBeAssignableTo<IReadOnlySet<string>>();
        context.Permissions.ShouldBeAssignableTo<ImmutableHashSet<string>>();
    }

    [Fact]
    public void Constructor_WithEmptyRoleClaims_ShouldFilterOutEmptyValues()
    {
        // Arrange
        var principal = CreatePrincipal(
            ("role", "Admin"),
            ("role", ""));

        // Act
        var context = new SecurityContext(principal);

        // Assert - empty values filtered (implementation uses IsNullOrEmpty)
        context.Roles.Count.ShouldBe(1);
        context.Roles.ShouldContain("Admin");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldUseDefaults()
    {
        // Arrange
        var principal = CreatePrincipal(("sub", "user-1"), ("role", "Admin"));

        // Act
        var context = new SecurityContext(principal, options: null);

        // Assert
        context.UserId.ShouldBe("user-1");
        context.Roles.ShouldContain("Admin");
    }

    #region Helpers

    private static ClaimsPrincipal CreatePrincipal(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(
            claims.Select(c => new Claim(c.Type, c.Value)),
            authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    #endregion
}
