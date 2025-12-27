using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.DistributedLock.SqlServer.Tests;

public class SqlServerDistributedLockProviderTests
{
    private readonly ILogger<SqlServerDistributedLockProvider> _logger;

    public SqlServerDistributedLockProviderTests()
    {
        _logger = NullLogger<SqlServerDistributedLockProvider>.Instance;
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        IOptions<SqlServerLockOptions>? options = null;

        // Act
        var act = () => new SqlServerDistributedLockProvider(options!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new SqlServerLockOptions
        {
            ConnectionString = "Server=.;Database=Test;Trusted_Connection=True;"
        });
        ILogger<SqlServerDistributedLockProvider>? logger = null;

        // Act
        var act = () => new SqlServerDistributedLockProvider(options, logger!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_WithNullConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var options = Options.Create(new SqlServerLockOptions
        {
            ConnectionString = null
        });

        // Act
        var act = () => new SqlServerDistributedLockProvider(options, _logger);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task TryAcquireAsync_WithNullResource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new SqlServerLockOptions
        {
            ConnectionString = "Server=.;Database=Test;Trusted_Connection=True;"
        });
        var provider = new SqlServerDistributedLockProvider(options, _logger);

        // Act
        var act = () => provider.TryAcquireAsync(
            null!,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("resource");
    }

    [Fact]
    public async Task AcquireAsync_WithNullResource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new SqlServerLockOptions
        {
            ConnectionString = "Server=.;Database=Test;Trusted_Connection=True;"
        });
        var provider = new SqlServerDistributedLockProvider(options, _logger);

        // Act
        var act = () => provider.AcquireAsync(
            null!,
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("resource");
    }

    [Fact]
    public async Task IsLockedAsync_WithNullResource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new SqlServerLockOptions
        {
            ConnectionString = "Server=.;Database=Test;Trusted_Connection=True;"
        });
        var provider = new SqlServerDistributedLockProvider(options, _logger);

        // Act
        var act = () => provider.IsLockedAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("resource");
    }

    [Fact]
    public async Task ExtendAsync_WithNullResource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new SqlServerLockOptions
        {
            ConnectionString = "Server=.;Database=Test;Trusted_Connection=True;"
        });
        var provider = new SqlServerDistributedLockProvider(options, _logger);

        // Act
        var act = () => provider.ExtendAsync(
            null!,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("resource");
    }
}
