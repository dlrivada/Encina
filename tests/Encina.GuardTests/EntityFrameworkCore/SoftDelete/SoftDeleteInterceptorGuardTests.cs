using Encina.EntityFrameworkCore.SoftDelete;
using Microsoft.Extensions.Logging;

namespace Encina.GuardTests.EntityFrameworkCore.SoftDelete;

/// <summary>
/// Guard clause tests for <see cref="SoftDeleteInterceptor"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class SoftDeleteInterceptorGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceProvider serviceProvider = null!;
        var options = new SoftDeleteInterceptorOptions();
        var timeProvider = TimeProvider.System;
        var logger = Substitute.For<ILogger<SoftDeleteInterceptor>>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new SoftDeleteInterceptor(serviceProvider, options, timeProvider, logger));
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        SoftDeleteInterceptorOptions options = null!;
        var timeProvider = TimeProvider.System;
        var logger = Substitute.For<ILogger<SoftDeleteInterceptor>>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new SoftDeleteInterceptor(serviceProvider, options, timeProvider, logger));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new SoftDeleteInterceptorOptions();
        TimeProvider timeProvider = null!;
        var logger = Substitute.For<ILogger<SoftDeleteInterceptor>>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new SoftDeleteInterceptor(serviceProvider, options, timeProvider, logger));
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new SoftDeleteInterceptorOptions();
        var timeProvider = TimeProvider.System;
        ILogger<SoftDeleteInterceptor> logger = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new SoftDeleteInterceptor(serviceProvider, options, timeProvider, logger));
        ex.ParamName.ShouldBe("logger");
    }

    #endregion
}
