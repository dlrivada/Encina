using System.Data;
using Encina.ADO.SqlServer.Scheduling;
using Encina.Messaging.Scheduling;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.ADO.SqlServer;

/// <summary>
/// Guard tests for <see cref="ScheduledMessageStoreADO"/> to verify null and invalid parameter handling.
/// </summary>
public class ScheduledMessageStoreADOGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ScheduledMessageStoreADO(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullTableName_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        var act = () => new ScheduledMessageStoreADO(connection, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("tableName");
    }

    [Fact]
    public void Constructor_WhitespaceTableName_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        var act = () => new ScheduledMessageStoreADO(connection, "  ");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.NotThrow(() => new ScheduledMessageStoreADO(connection));
    }

    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreADO(connection);

        // Act & Assert
        var act = async () => await store.AddAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("message");
    }

    [Fact]
    public async Task GetDueMessagesAsync_ZeroBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetDueMessagesAsync(0, 3);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("batchSize");
    }

    [Fact]
    public async Task GetDueMessagesAsync_NegativeBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetDueMessagesAsync(-1, 3);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("batchSize");
    }

    [Fact]
    public async Task GetDueMessagesAsync_NegativeMaxRetries_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetDueMessagesAsync(10, -1);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("maxRetries");
    }

    [Fact]
    public async Task MarkAsProcessedAsync_EmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreADO(connection);

        // Act & Assert
        var act = async () => await store.MarkAsProcessedAsync(Guid.Empty);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("messageId");
    }

    [Fact]
    public async Task MarkAsFailedAsync_EmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreADO(connection);

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(Guid.Empty, "error", null);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("messageId");
    }

    [Fact]
    public async Task MarkAsFailedAsync_NullErrorMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreADO(connection);

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(Guid.NewGuid(), null!, null);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task MarkAsFailedAsync_WhitespaceErrorMessage_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreADO(connection);

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(Guid.NewGuid(), "  ", null);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task RescheduleRecurringMessageAsync_EmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreADO(connection);

        // Act & Assert
        var act = async () => await store.RescheduleRecurringMessageAsync(Guid.Empty, DateTime.UtcNow.AddHours(1));
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("messageId");
    }

    [Fact]
    public async Task CancelAsync_EmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreADO(connection);

        // Act & Assert
        var act = async () => await store.CancelAsync(Guid.Empty);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("messageId");
    }

    [Fact]
    public async Task SaveChangesAsync_ReturnsUnitDefault()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreADO(connection);

        // Act
        var result = await store.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue("SaveChanges should return Right(Unit)");
    }
}
