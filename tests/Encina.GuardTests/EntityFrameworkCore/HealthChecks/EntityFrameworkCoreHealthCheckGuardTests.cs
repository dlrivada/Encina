using Encina.EntityFrameworkCore.Health;
using Encina.Messaging.Health;

namespace Encina.GuardTests.EntityFrameworkCore.HealthChecks;

/// <summary>
/// Guard clause tests for <see cref="EntityFrameworkCoreHealthCheck"/>.
/// </summary>
/// <remarks>
/// <para>
/// Note: The <see cref="EntityFrameworkCoreHealthCheck"/> constructor does not currently
/// guard against null <c>serviceProvider</c>. The <c>options</c> parameter is nullable by design.
/// </para>
/// <para>
/// The base class <see cref="EncinaHealthCheck"/> guards against null/empty name,
/// but that is covered by the base class guard tests.
/// </para>
/// <para>
/// This file exists to document that the constructor was evaluated for guard clauses.
/// If null guards are added in the future, tests should be added here.
/// </para>
/// </remarks>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class EntityFrameworkCoreHealthCheckGuardTests
{
    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act & Assert - verifies the constructor works with valid params
        var healthCheck = new EntityFrameworkCoreHealthCheck(serviceProvider, null);
        healthCheck.ShouldNotBeNull();
    }
}
