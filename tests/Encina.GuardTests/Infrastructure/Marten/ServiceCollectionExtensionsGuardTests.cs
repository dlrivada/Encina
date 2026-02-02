using Encina.DomainModeling;
using Encina.Marten;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.Infrastructure.Marten;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions"/> to verify null parameter handling.
/// </summary>
public class ServiceCollectionExtensionsGuardTests
{
    /// <summary>
    /// Verifies that AddEncinaMarten throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaMarten_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => services.AddEncinaMarten();
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaMarten with configure action throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaMarten_WithConfigure_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => services.AddEncinaMarten(_ => { });
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaMarten throws ArgumentNullException when configure action is null.
    /// </summary>
    [Fact]
    public void AddEncinaMarten_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<EncinaMartenOptions> configure = null!;

        // Act & Assert
        var act = () => services.AddEncinaMarten(configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    /// <summary>
    /// Verifies that AddAggregateRepository throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddAggregateRepository_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => services.AddAggregateRepository<TestAggregate>();
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Test aggregate for guard tests.
    /// </summary>
    private sealed class TestAggregate : AggregateBase
    {
        protected override void Apply(object domainEvent)
        {
            // No-op for testing
        }
    }
}
