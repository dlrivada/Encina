using System.Data;
using Encina.ADO.MySQL.UnitOfWork;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.ADO.MySQL;

/// <summary>
/// Guard tests for <see cref="UnitOfWorkADO"/> to verify null parameter handling and disposed state.
/// </summary>
public class UnitOfWorkADOGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act & Assert
        var act = () => new UnitOfWorkADO(null!, serviceProvider);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        var act = () => new UnitOfWorkADO(connection, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act & Assert
        Should.NotThrow(() => new UnitOfWorkADO(connection, serviceProvider));
    }

    [Fact]
    public void HasActiveTransaction_InitiallyFalse()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var uow = new UnitOfWorkADO(connection, serviceProvider);

        // Act & Assert
        uow.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var uow = new UnitOfWorkADO(connection, serviceProvider);
        await uow.DisposeAsync();

        // Act & Assert
        var act = async () => await uow.SaveChangesAsync();
        await Should.ThrowAsync<ObjectDisposedException>(act);
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var uow = new UnitOfWorkADO(connection, serviceProvider);
        await uow.DisposeAsync();

        // Act & Assert
        var act = async () => await uow.BeginTransactionAsync();
        await Should.ThrowAsync<ObjectDisposedException>(act);
    }

    [Fact]
    public async Task CommitAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var uow = new UnitOfWorkADO(connection, serviceProvider);
        await uow.DisposeAsync();

        // Act & Assert
        var act = async () => await uow.CommitAsync();
        await Should.ThrowAsync<ObjectDisposedException>(act);
    }

    [Fact]
    public async Task CommitAsync_NoActiveTransaction_ReturnsLeft()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var uow = new UnitOfWorkADO(connection, serviceProvider);

        // Act
        var result = await uow.CommitAsync();

        // Assert
        result.IsLeft.ShouldBeTrue("Commit without active transaction should fail");
    }

    [Fact]
    public async Task RollbackAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var uow = new UnitOfWorkADO(connection, serviceProvider);
        await uow.DisposeAsync();

        // Act & Assert
        var act = async () => await uow.RollbackAsync();
        await Should.ThrowAsync<ObjectDisposedException>(act);
    }

    [Fact]
    public void Repository_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var uow = new UnitOfWorkADO(connection, serviceProvider);
        uow.DisposeAsync().AsTask().GetAwaiter().GetResult();

        // Act & Assert
        var act = () => uow.Repository<TestEntity, Guid>();
        Should.Throw<ObjectDisposedException>(act);
    }

    [Fact]
    public void UpdateImmutable_NullModified_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var uow = new UnitOfWorkADO(connection, serviceProvider);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => { uow.UpdateImmutable<TestEntity>(null!); });
    }

    [Fact]
    public void UpdateImmutable_ValidEntity_ReturnsLeftNotSupported()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var uow = new UnitOfWorkADO(connection, serviceProvider);

        // Act
        var result = uow.UpdateImmutable(new TestEntity());

        // Assert
        result.IsLeft.ShouldBeTrue("UpdateImmutable should return not supported for ADO.NET");
    }

    [Fact]
    public async Task UpdateImmutableAsync_NullModified_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var uow = new UnitOfWorkADO(connection, serviceProvider);

        // Act & Assert
        var act = async () => await uow.UpdateImmutableAsync<TestEntity>(null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task UpdateImmutableAsync_ValidEntity_ReturnsLeftNotSupported()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var uow = new UnitOfWorkADO(connection, serviceProvider);

        // Act
        var result = await uow.UpdateImmutableAsync(new TestEntity());

        // Assert
        result.IsLeft.ShouldBeTrue("UpdateImmutableAsync should return not supported for ADO.NET");
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var uow = new UnitOfWorkADO(connection, serviceProvider);

        // Act & Assert - Double dispose should be safe
        await uow.DisposeAsync();
        await Should.NotThrowAsync(async () => await uow.DisposeAsync());
    }

    public class TestEntity
    {
        public Guid Id { get; set; }
    }
}
