using System.Data;
using Encina.Dapper.MySQL.ABAC;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Dapper.MySQL.ABAC;

/// <summary>
/// Guard tests for <see cref="PolicyStoreDapper"/> to verify null and invalid parameter handling.
/// </summary>
public class PolicyStoreDapperGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        var serializer = Substitute.For<IPolicySerializer>();

        // Act & Assert
        var act = () => new PolicyStoreDapper(null!, serializer);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullSerializer_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        var act = () => new PolicyStoreDapper(connection, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serializer");
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var serializer = Substitute.For<IPolicySerializer>();

        // Act & Assert
        Should.NotThrow(() => new PolicyStoreDapper(connection, serializer));
    }

    [Fact]
    public async Task GetPolicySetAsync_NullPolicySetId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var act = async () => await store.GetPolicySetAsync(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task GetPolicySetAsync_WhitespacePolicySetId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var act = async () => await store.GetPolicySetAsync("  ");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task SavePolicySetAsync_NullPolicySet_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var act = async () => await store.SavePolicySetAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("policySet");
    }

    [Fact]
    public async Task DeletePolicySetAsync_NullPolicySetId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var act = async () => await store.DeletePolicySetAsync(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task DeletePolicySetAsync_WhitespacePolicySetId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var act = async () => await store.DeletePolicySetAsync("  ");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task ExistsPolicySetAsync_NullPolicySetId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var act = async () => await store.ExistsPolicySetAsync(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ExistsPolicySetAsync_WhitespacePolicySetId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var act = async () => await store.ExistsPolicySetAsync("  ");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task GetPolicyAsync_NullPolicyId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var act = async () => await store.GetPolicyAsync(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task GetPolicyAsync_WhitespacePolicyId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var act = async () => await store.GetPolicyAsync("  ");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task SavePolicyAsync_NullPolicy_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var act = async () => await store.SavePolicyAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("policy");
    }

    [Fact]
    public async Task DeletePolicyAsync_NullPolicyId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var act = async () => await store.DeletePolicyAsync(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task DeletePolicyAsync_WhitespacePolicyId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var act = async () => await store.DeletePolicyAsync("  ");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task ExistsPolicyAsync_NullPolicyId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var act = async () => await store.ExistsPolicyAsync(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ExistsPolicyAsync_WhitespacePolicyId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var act = async () => await store.ExistsPolicyAsync("  ");
        Should.Throw<ArgumentException>(act);
    }

    private static PolicyStoreDapper CreateStore()
    {
        var connection = Substitute.For<IDbConnection>();
        var serializer = Substitute.For<IPolicySerializer>();
        return new PolicyStoreDapper(connection, serializer);
    }
}
