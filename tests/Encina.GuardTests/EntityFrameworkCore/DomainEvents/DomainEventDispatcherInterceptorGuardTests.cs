using Encina.DomainModeling;
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

    #region DomainEventDispatchException Constructor Guards

    [Fact]
    public void ExceptionConstructor_NullDomainEventWithError_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new DomainEventDispatchException(null!, EncinaErrors.Create("CODE", "message")));
        ex.ParamName.ShouldBe("domainEvent");
    }

    [Fact]
    public void ExceptionConstructor_NullDomainEventWithException_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new DomainEventDispatchException(null!, new InvalidOperationException("inner")));
        ex.ParamName.ShouldBe("domainEvent");
    }

    [Fact]
    public void ExceptionConstructor_NullInnerException_ThrowsArgumentNullException()
    {
        // Arrange
        var domainEvent = Substitute.For<IDomainEvent>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new DomainEventDispatchException(domainEvent, (Exception)null!));
        ex.ParamName.ShouldBe("innerException");
    }

    #endregion
}
