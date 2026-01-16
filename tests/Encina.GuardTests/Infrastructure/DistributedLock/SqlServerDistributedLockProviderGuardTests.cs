using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.GuardTests.Infrastructure.DistributedLock;

/// <summary>
/// Guard clause tests for SqlServerDistributedLockProvider.
/// </summary>
public class SqlServerDistributedLockProviderGuardTests
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        IOptions<SqlServerLockOptions>? options = null;
        var logger = NullLogger<SqlServerDistributedLockProvider>.Instance;

        // Act
        var act = () => new SqlServerDistributedLockProvider(options!, logger);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new SqlServerLockOptions
        {
            ConnectionString = "Server=.;Database=Test;"
        });
        ILogger<SqlServerDistributedLockProvider>? logger = null;

        // Act
        var act = () => new SqlServerDistributedLockProvider(options, logger!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_WithNullConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var options = Options.Create(new SqlServerLockOptions
        {
            ConnectionString = null
        });
        var logger = NullLogger<SqlServerDistributedLockProvider>.Instance;

        // Act
        var act = () => new SqlServerDistributedLockProvider(options, logger);

        // Assert - The parameter name is "options" because that's the invalid constructor argument
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("options");
    }
}
