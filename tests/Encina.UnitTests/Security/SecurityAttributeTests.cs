using System.Reflection;
using Encina.Security;
using FluentAssertions;

namespace Encina.UnitTests.Security;

/// <summary>
/// Unit tests for security attribute classes.
/// </summary>
public class SecurityAttributeTests
{
    // -- AllowAnonymousAttribute --

    [Fact]
    public void AllowAnonymous_ShouldBeApplicableToClass()
    {
        var usage = typeof(AllowAnonymousAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        usage.Should().NotBeNull();
        usage!.ValidOn.Should().HaveFlag(AttributeTargets.Class);
    }

    [Fact]
    public void AllowAnonymous_ShouldNotAllowMultiple()
    {
        var usage = typeof(AllowAnonymousAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        usage!.AllowMultiple.Should().BeFalse();
    }

    // -- DenyAnonymousAttribute --

    [Fact]
    public void DenyAnonymous_ShouldInheritFromSecurityAttribute()
    {
        var attr = new DenyAnonymousAttribute();
        attr.Should().BeAssignableTo<SecurityAttribute>();
    }

    [Fact]
    public void DenyAnonymous_Order_ShouldDefaultToZero()
    {
        var attr = new DenyAnonymousAttribute();
        attr.Order.Should().Be(0);
    }

    // -- RequireRoleAttribute --

    [Fact]
    public void RequireRole_ShouldStoreRoles()
    {
        var attr = new RequireRoleAttribute("Admin", "Manager");
        attr.Roles.Should().BeEquivalentTo(["Admin", "Manager"]);
    }

    [Fact]
    public void RequireRole_WithEmptyRoles_ShouldStoreEmpty()
    {
        var attr = new RequireRoleAttribute();
        attr.Roles.Should().BeEmpty();
    }

    [Fact]
    public void RequireRole_ShouldInheritFromSecurityAttribute()
    {
        var attr = new RequireRoleAttribute("Admin");
        attr.Should().BeAssignableTo<SecurityAttribute>();
    }

    // -- RequireAllRolesAttribute --

    [Fact]
    public void RequireAllRoles_ShouldStoreRoles()
    {
        var attr = new RequireAllRolesAttribute("Admin", "Manager");
        attr.Roles.Should().BeEquivalentTo(["Admin", "Manager"]);
    }

    [Fact]
    public void RequireAllRoles_ShouldInheritFromSecurityAttribute()
    {
        var attr = new RequireAllRolesAttribute("Admin");
        attr.Should().BeAssignableTo<SecurityAttribute>();
    }

    // -- RequirePermissionAttribute --

    [Fact]
    public void RequirePermission_ShouldStorePermissions()
    {
        var attr = new RequirePermissionAttribute("orders:read", "orders:write");
        attr.Permissions.Should().BeEquivalentTo(["orders:read", "orders:write"]);
    }

    [Fact]
    public void RequirePermission_RequireAll_ShouldDefaultToFalse()
    {
        var attr = new RequirePermissionAttribute("orders:read");
        attr.RequireAll.Should().BeFalse();
    }

    [Fact]
    public void RequirePermission_RequireAll_ShouldBeSettable()
    {
        var attr = new RequirePermissionAttribute("orders:read") { RequireAll = true };
        attr.RequireAll.Should().BeTrue();
    }

    // -- RequireClaimAttribute --

    [Fact]
    public void RequireClaim_TypeOnly_ShouldStoreClaimType()
    {
        var attr = new RequireClaimAttribute("department");
        attr.ClaimType.Should().Be("department");
        attr.ClaimValue.Should().BeNull();
    }

    [Fact]
    public void RequireClaim_TypeAndValue_ShouldStoreBoth()
    {
        var attr = new RequireClaimAttribute("department", "engineering");
        attr.ClaimType.Should().Be("department");
        attr.ClaimValue.Should().Be("engineering");
    }

    // -- RequireOwnershipAttribute --

    [Fact]
    public void RequireOwnership_ShouldStorePropertyName()
    {
        var attr = new RequireOwnershipAttribute("OwnerId");
        attr.OwnerProperty.Should().Be("OwnerId");
    }

    // -- SecurityAttribute.Order --

    [Fact]
    public void Order_ShouldBeSettable()
    {
        var attr = new DenyAnonymousAttribute { Order = 5 };
        attr.Order.Should().Be(5);
    }

    // -- Multiple attributes on same type --

    [Fact]
    public void SecurityAttribute_ShouldAllowMultipleOnSameType()
    {
        var usage = typeof(SecurityAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        usage!.AllowMultiple.Should().BeTrue();
    }

    [Fact]
    public void MultipleAttributes_ShouldBeDiscoverable()
    {
        var attributes = typeof(MultiAttributeRequest)
            .GetCustomAttributes(typeof(SecurityAttribute), inherit: true)
            .Cast<SecurityAttribute>()
            .ToList();

        attributes.Should().HaveCount(2);
        attributes.Should().ContainSingle(a => a is DenyAnonymousAttribute);
        attributes.Should().ContainSingle(a => a is RequireRoleAttribute);
    }

    [Fact]
    public void Attributes_OrderedByOrder_ShouldSortCorrectly()
    {
        var attributes = typeof(OrderedAttributeRequest)
            .GetCustomAttributes(typeof(SecurityAttribute), inherit: true)
            .Cast<SecurityAttribute>()
            .OrderBy(a => a.Order)
            .ToList();

        attributes[0].Should().BeOfType<RequireRoleAttribute>();
        attributes[0].Order.Should().Be(1);
        attributes[1].Should().BeOfType<DenyAnonymousAttribute>();
        attributes[1].Order.Should().Be(2);
    }

    #region Test Types

    [DenyAnonymous]
    [RequireRole("Admin")]
    private sealed class MultiAttributeRequest : ICommand<LanguageExt.Unit> { }

    [DenyAnonymous(Order = 2)]
    [RequireRole("Admin", Order = 1)]
    private sealed class OrderedAttributeRequest : ICommand<LanguageExt.Unit> { }

    #endregion
}
