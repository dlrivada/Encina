using System.Data;
using Encina.ADO.PostgreSQL.Sagas;
using Encina.Messaging.Sagas;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.ADO.PostgreSQL;

/// <summary>
/// Guard tests for <see cref="SagaStoreADO"/> to verify null and invalid parameter handling.
/// </summary>
public class SagaStoreADOGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SagaStoreADO(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullTableName_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        var act = () => new SagaStoreADO(connection, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("tableName");
    }

    [Fact]
    public void Constructor_WhitespaceTableName_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        var act = () => new SagaStoreADO(connection, "  ");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.NotThrow(() => new SagaStoreADO(connection));
    }

    [Fact]
    public void Constructor_CustomTableName_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.NotThrow(() => new SagaStoreADO(connection, "CustomSagas"));
    }

    [Fact]
    public async Task GetAsync_EmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetAsync(Guid.Empty);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("sagaId");
    }

    [Fact]
    public async Task AddAsync_NullSagaState_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreADO(connection);

        // Act & Assert
        var act = async () => await store.AddAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("sagaState");
    }

    [Fact]
    public async Task UpdateAsync_NullSagaState_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreADO(connection);

        // Act & Assert
        var act = async () => await store.UpdateAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("sagaState");
    }

    [Fact]
    public async Task GetStuckSagasAsync_ZeroOlderThan_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetStuckSagasAsync(TimeSpan.Zero, 10);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("olderThan");
    }

    [Fact]
    public async Task GetStuckSagasAsync_NegativeOlderThan_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetStuckSagasAsync(TimeSpan.FromMinutes(-1), 10);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("olderThan");
    }

    [Fact]
    public async Task GetStuckSagasAsync_ZeroBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetStuckSagasAsync(TimeSpan.FromMinutes(5), 0);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("batchSize");
    }

    [Fact]
    public async Task GetStuckSagasAsync_NegativeBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetStuckSagasAsync(TimeSpan.FromMinutes(5), -1);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("batchSize");
    }

    [Fact]
    public async Task GetExpiredSagasAsync_ZeroBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetExpiredSagasAsync(0);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("batchSize");
    }

    [Fact]
    public async Task GetExpiredSagasAsync_NegativeBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetExpiredSagasAsync(-5);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("batchSize");
    }

    [Fact]
    public async Task SaveChangesAsync_ReturnsUnitDefault()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreADO(connection);

        // Act
        var result = await store.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue("SaveChanges should return Right(Unit)");
    }
}
