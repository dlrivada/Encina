using Encina.EntityFrameworkCore.ReadWriteSeparation;
using Encina.Messaging.ReadWriteSeparation;
using Microsoft.EntityFrameworkCore;

namespace Encina.GuardTests.EntityFrameworkCore.ReadWrite;

/// <summary>
/// Guard clause tests for <see cref="ReadWriteDbContextFactory{TContext}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class ReadWriteDbContextFactoryGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceProvider serviceProvider = null!;
        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        var baseOptions = new DbContextOptionsBuilder<TestReadWriteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ReadWriteDbContextFactory<TestReadWriteDbContext>(
                serviceProvider, connectionSelector, baseOptions));
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullConnectionSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        IReadWriteConnectionSelector connectionSelector = null!;
        var baseOptions = new DbContextOptionsBuilder<TestReadWriteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ReadWriteDbContextFactory<TestReadWriteDbContext>(
                serviceProvider, connectionSelector, baseOptions));
        ex.ParamName.ShouldBe("connectionSelector");
    }

    [Fact]
    public void Constructor_NullBaseOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        DbContextOptions<TestReadWriteDbContext> baseOptions = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ReadWriteDbContextFactory<TestReadWriteDbContext>(
                serviceProvider, connectionSelector, baseOptions));
        ex.ParamName.ShouldBe("baseOptions");
    }

    #endregion

    #region Test Infrastructure

    private sealed class TestReadWriteDbContext : DbContext
    {
        public TestReadWriteDbContext(DbContextOptions<TestReadWriteDbContext> options) : base(options)
        {
        }
    }

    #endregion
}
