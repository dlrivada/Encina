using System.Reflection;
using Encina.DomainModeling;
using FluentAssertions;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying IDomainService marker interface contract.
/// </summary>
public sealed class DomainServiceContracts
{
    private readonly Type _interfaceType = typeof(IDomainService);

    [Fact]
    public void IDomainService_MustBeInterface()
    {
        _interfaceType.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IDomainService_MustBePublic()
    {
        _interfaceType.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void IDomainService_MustHaveNoMembers()
    {
        // Marker interface should have no methods or properties
        var members = _interfaceType.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        members.Should().BeEmpty("marker interface should have no members");
    }

    [Fact]
    public void IDomainService_MustNotExtendOtherInterfaces()
    {
        // Marker interface should not inherit from other interfaces
        _interfaceType.GetInterfaces().Should().BeEmpty();
    }

    [Fact]
    public void IDomainService_CanBeImplementedByClass()
    {
        // Verify the interface can be implemented
        var testService = new TestDomainService();
        testService.Should().BeAssignableTo<IDomainService>();
    }

    [Fact]
    public void IDomainService_IsUsableForDependencyInjection()
    {
        // Verify we can use it as a type parameter for generic constraints
        var method = typeof(DomainServiceContracts).GetMethod(nameof(GenericConstraintTest), BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();
    }

    private static void GenericConstraintTest<T>() where T : IDomainService
    {
        // This method exists to verify the interface can be used as a generic constraint
    }

    private sealed class TestDomainService : IDomainService
    {
        // Implementation of marker interface (no members needed)
    }
}
