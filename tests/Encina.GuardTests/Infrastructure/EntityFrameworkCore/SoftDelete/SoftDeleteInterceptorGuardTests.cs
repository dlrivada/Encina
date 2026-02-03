using Encina.EntityFrameworkCore.SoftDelete;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Encina.GuardTests.Infrastructure.EntityFrameworkCore.SoftDelete;

/// <summary>
/// Guard tests for <see cref="SoftDeleteInterceptor"/> to verify null parameter handling.
/// </summary>
public sealed class SoftDeleteInterceptorGuardTests
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly SoftDeleteInterceptorOptions _options = new();
    private readonly IServiceProvider _serviceProvider = new TestServiceProvider();

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceProvider serviceProvider = null!;

        // Act & Assert
        var act = () => new SoftDeleteInterceptor(
            serviceProvider,
            _options,
            _timeProvider,
            NullLogger<SoftDeleteInterceptor>.Instance);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        SoftDeleteInterceptorOptions options = null!;

        // Act & Assert
        var act = () => new SoftDeleteInterceptor(
            _serviceProvider,
            options,
            _timeProvider,
            NullLogger<SoftDeleteInterceptor>.Instance);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        TimeProvider timeProvider = null!;

        // Act & Assert
        var act = () => new SoftDeleteInterceptor(
            _serviceProvider,
            _options,
            timeProvider,
            NullLogger<SoftDeleteInterceptor>.Instance);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        Microsoft.Extensions.Logging.ILogger<SoftDeleteInterceptor> logger = null!;

        // Act & Assert
        var act = () => new SoftDeleteInterceptor(
            _serviceProvider,
            _options,
            _timeProvider,
            logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
