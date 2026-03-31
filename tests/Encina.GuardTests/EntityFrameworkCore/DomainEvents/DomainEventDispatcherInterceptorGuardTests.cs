using Encina.EntityFrameworkCore.DomainEvents;
using Microsoft.Extensions.Logging;

namespace Encina.GuardTests.EntityFrameworkCore.DomainEvents;

/// <summary>
/// Guard clause tests for <see cref="DomainEventDispatcherInterceptor"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class DomainEventDispatcherInterceptorGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceProvider serviceProvider = null!;
        var options = new DomainEventDispatcherOptions();
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new DomainEventDispatcherInterceptor(serviceProvider, options, logger));
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        DomainEventDispatcherOptions options = null!;
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new DomainEventDispatcherInterceptor(serviceProvider, options, logger));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new DomainEventDispatcherOptions();
        ILogger<DomainEventDispatcherInterceptor> logger = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new DomainEventDispatcherInterceptor(serviceProvider, options, logger));
        ex.ParamName.ShouldBe("logger");
    }

    #endregion
}
