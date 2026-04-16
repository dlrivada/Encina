using System.Reflection;
using Encina.Security;
using Shouldly;

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

        usage.ShouldNotBeNull();
        usage!.ValidOn.HasFlag(AttributeTargets.Class).ShouldBeTrue();
    }

    [Fact]
    public void AllowAnonymous_ShouldNotAllowMultiple()
    {
        var usage = typeof(AllowAnonymousAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        usage!.AllowMultiple.ShouldBeFalse();
    }

    // -- DenyAnonymousAttribute --

    [Fact]
    public void DenyAnonymous_ShouldInheritFromSecurityAttribute()
    {
        var attr = new DenyAnonymousAttribute();
        attr.ShouldBeAssignableTo<SecurityAttribute>();
    }

    [Fact]
    public void DenyAnonymous_Order_ShouldDefaultToZero()
    {
        var attr = new DenyAnonymousAttribute();
        attr.Order.ShouldBe(0);
    }

    // -- RequireRoleAttribute --

    [Fact]
    public void RequireRole_ShouldStoreRoles()
    {
        var attr = new RequireRoleAttribute("Admin", "Manager");
        attr.Roles.ShouldBe(["Admin", "Manager"]);
    }

    [Fact]
    public void RequireRole_WithEmptyRoles_ShouldStoreEmpty()
    {
        var attr = new RequireRoleAttribute();
        attr.Roles.ShouldBeEmpty();
    }

    [Fact]
    public void RequireRole_ShouldInheritFromSecurityAttribute()
    {
        var attr = new RequireRoleAttribute("Admin");
        attr.ShouldBeAssignableTo<SecurityAttribute>();
    }

    // -- RequireAllRolesAttribute --

    [Fact]
    public void RequireAllRoles_ShouldStoreRoles()
    {
        var attr = new RequireAllRolesAttribute("Admin", "Manager");
        attr.Roles.ShouldBe(["Admin", "Manager"]);
    }

    [Fact]
    public void RequireAllRoles_ShouldInheritFromSecurityAttribute()
    {
        var attr = new RequireAllRolesAttribute("Admin");
        attr.ShouldBeAssignableTo<SecurityAttribute>();
    }

    // -- RequirePermissionAttribute --

    [Fact]
    public void RequirePermission_ShouldStorePermissions()
    {
        var attr = new RequirePermissionAttribute("orders:read", "orders:write");
        attr.Permissions.ShouldBe(["orders:read", "orders:write"]);
    }

    [Fact]
    public void RequirePermission_RequireAll_ShouldDefaultToFalse()
    {
        var attr = new RequirePermissionAttribute("orders:read");
        attr.RequireAll.ShouldBeFalse();
    }

    [Fact]
    public void RequirePermission_RequireAll_ShouldBeSettable()
    {
        var attr = new RequirePermissionAttribute("orders:read") { RequireAll = true };
        attr.RequireAll.ShouldBeTrue();
    }

    // -- RequireClaimAttribute --

    [Fact]
    public void RequireClaim_TypeOnly_ShouldStoreClaimType()
    {
        var attr = new RequireClaimAttribute("department");
        attr.ClaimType.ShouldBe("department");
        attr.ClaimValue.ShouldBeNull();
    }

    [Fact]
    public void RequireClaim_TypeAndValue_ShouldStoreBoth()
    {
        var attr = new RequireClaimAttribute("department", "engineering");
        attr.ClaimType.ShouldBe("department");
        attr.ClaimValue.ShouldBe("engineering");
    }

    // -- RequireOwnershipAttribute --

    [Fact]
    public void RequireOwnership_ShouldStorePropertyName()
    {
        var attr = new RequireOwnershipAttribute("OwnerId");
        attr.OwnerProperty.ShouldBe("OwnerId");
    }

    // -- SecurityAttribute.Order --

    [Fact]
    public void Order_ShouldBeSettable()
    {
        var attr = new DenyAnonymousAttribute { Order = 5 };
        attr.Order.ShouldBe(5);
    }

    // -- Multiple attributes on same type --

    [Fact]
    public void SecurityAttribute_ShouldAllowMultipleOnSameType()
    {
        var usage = typeof(SecurityAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        usage!.AllowMultiple.ShouldBeTrue();
    }

    [Fact]
    public void MultipleAttributes_ShouldBeDiscoverable()
    {
        var attributes = typeof(MultiAttributeRequest)
            .GetCustomAttributes(typeof(SecurityAttribute), inherit: true)
            .Cast<SecurityAttribute>()
            .ToList();

        attributes.Count.ShouldBe(2);
        attributes.ShouldContain(a => a is DenyAnonymousAttribute);
        attributes.ShouldContain(a => a is RequireRoleAttribute);
    }

    [Fact]
    public void Attributes_OrderedByOrder_ShouldSortCorrectly()
    {
        var attributes = typeof(OrderedAttributeRequest)
            .GetCustomAttributes(typeof(SecurityAttribute), inherit: true)
            .Cast<SecurityAttribute>()
            .OrderBy(a => a.Order)
            .ToList();

        attributes[0].ShouldBeOfType<RequireRoleAttribute>();
        attributes[0].Order.ShouldBe(1);
        attributes[1].ShouldBeOfType<DenyAnonymousAttribute>();
        attributes[1].Order.ShouldBe(2);
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
