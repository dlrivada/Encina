using Encina.DomainModeling.Auditing;
using Encina.EntityFrameworkCore.Auditing;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.Auditing;

/// <summary>
/// Guard clause tests for <see cref="AuditInterceptor"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class AuditInterceptorGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceProvider serviceProvider = null!;
        var options = new AuditInterceptorOptions();
        var timeProvider = TimeProvider.System;
        var logger = Substitute.For<ILogger<AuditInterceptor>>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new AuditInterceptor(serviceProvider, options, timeProvider, logger));
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        AuditInterceptorOptions options = null!;
        var timeProvider = TimeProvider.System;
        var logger = Substitute.For<ILogger<AuditInterceptor>>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new AuditInterceptor(serviceProvider, options, timeProvider, logger));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new AuditInterceptorOptions();
        TimeProvider timeProvider = null!;
        var logger = Substitute.For<ILogger<AuditInterceptor>>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new AuditInterceptor(serviceProvider, options, timeProvider, logger));
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new AuditInterceptorOptions();
        var timeProvider = TimeProvider.System;
        ILogger<AuditInterceptor> logger = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new AuditInterceptor(serviceProvider, options, timeProvider, logger));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ValidParameters_WithNullAuditLogStore_DoesNotThrow()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new AuditInterceptorOptions();
        var timeProvider = TimeProvider.System;
        var logger = Substitute.For<ILogger<AuditInterceptor>>();

        // Act & Assert - auditLogStore is optional (nullable)
        Should.NotThrow(() =>
            new AuditInterceptor(serviceProvider, options, timeProvider, logger, auditLogStore: null));
    }

    #endregion
}
