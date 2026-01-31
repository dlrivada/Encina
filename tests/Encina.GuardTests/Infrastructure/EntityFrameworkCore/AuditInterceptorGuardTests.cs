using Encina.EntityFrameworkCore.Auditing;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Encina.GuardTests.Infrastructure.EntityFrameworkCore;

/// <summary>
/// Guard tests for AuditInterceptor to verify null parameter handling in constructor.
/// </summary>
public class AuditInterceptorGuardTests
{
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new AuditInterceptorOptions();
        var timeProvider = TimeProvider.System;
        var logger = Substitute.For<ILogger<AuditInterceptor>>();

        // Act
        var exception = Should.Throw<ArgumentNullException>(() =>
            new AuditInterceptor(null!, options, timeProvider, logger));

        // Assert
        exception.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var timeProvider = TimeProvider.System;
        var logger = Substitute.For<ILogger<AuditInterceptor>>();

        // Act
        var exception = Should.Throw<ArgumentNullException>(() =>
            new AuditInterceptor(serviceProvider, null!, timeProvider, logger));

        // Assert
        exception.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new AuditInterceptorOptions();
        var logger = Substitute.For<ILogger<AuditInterceptor>>();

        // Act
        var exception = Should.Throw<ArgumentNullException>(() =>
            new AuditInterceptor(serviceProvider, options, null!, logger));

        // Assert
        exception.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new AuditInterceptorOptions();
        var timeProvider = TimeProvider.System;

        // Act
        var exception = Should.Throw<ArgumentNullException>(() =>
            new AuditInterceptor(serviceProvider, options, timeProvider, null!));

        // Assert
        exception.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullAuditLogStore_DoesNotThrow()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new AuditInterceptorOptions();
        var timeProvider = TimeProvider.System;
        var logger = Substitute.For<ILogger<AuditInterceptor>>();

        // Act & Assert - null auditLogStore is valid (optional parameter)
        Should.NotThrow(() =>
            new AuditInterceptor(serviceProvider, options, timeProvider, logger, null));
    }

    [Fact]
    public void Constructor_AllValidParameters_CreatesInterceptor()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new AuditInterceptorOptions();
        var timeProvider = TimeProvider.System;
        var logger = Substitute.For<ILogger<AuditInterceptor>>();

        // Act
        var interceptor = new AuditInterceptor(serviceProvider, options, timeProvider, logger);

        // Assert
        interceptor.ShouldNotBeNull();
    }
}
