using System.Data;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;
using Microsoft.Data.Sqlite;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.Tests;

/// <summary>
/// Integration tests for <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/>.
/// Tests transaction commit/rollback behavior with real SQLite database.
/// </summary>
[Trait("Category", "Integration")]
public sealed class TransactionPipelineBehaviorTests : IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _database;

    public TransactionPipelineBehaviorTests(SqliteFixture database)
    {
        _database = database;

        // Clear all data before each test
        _database.ClearAllDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Handle_SuccessfulRequest_CommitsTransaction()
    {
        // Arrange
        var connection = _database.CreateConnection();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);
        var request = new TestRequest();
        var context = RequestContext.Create();

        var wasHandlerCalled = false;
        RequestHandlerCallback<string> next = async () =>
        {
            wasHandlerCalled = true;

            // Insert data within transaction
            await using var command = ((SqliteConnection)connection).CreateCommand();
            command.CommandText = "INSERT INTO OutboxMessages (Id, NotificationType, Content, CreatedAtUtc, RetryCount) VALUES (@Id, @Type, @Content, @Created, 0)";
            command.Parameters.Add(new SqliteParameter("@Id", Guid.NewGuid().ToString()));
            command.Parameters.Add(new SqliteParameter("@Type", "TestNotification"));
            command.Parameters.Add(new SqliteParameter("@Content", "{}"));
            command.Parameters.Add(new SqliteParameter("@Created", DateTime.UtcNow.ToString("O")));
            await command.ExecuteNonQueryAsync();

            return Right<MediatorError, string>("Success");
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        Assert.True(wasHandlerCalled);
        Assert.True(result.IsRight);

        // Verify data was committed
        await using var verifyCommand = ((SqliteConnection)connection).CreateCommand();
        verifyCommand.CommandText = "SELECT COUNT(*) FROM OutboxMessages";
        var count = (long)(await verifyCommand.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Handle_FailedRequest_RollsBackTransaction()
    {
        // Arrange
        var connection = _database.CreateConnection();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);
        var request = new TestRequest();
        var context = RequestContext.Create();

        var wasHandlerCalled = false;
        RequestHandlerCallback<string> next = async () =>
        {
            wasHandlerCalled = true;

            // Insert data within transaction
            await using var command = ((SqliteConnection)connection).CreateCommand();
            command.CommandText = "INSERT INTO OutboxMessages (Id, NotificationType, Content, CreatedAtUtc, RetryCount) VALUES (@Id, @Type, @Content, @Created, 0)";
            command.Parameters.Add(new SqliteParameter("@Id", Guid.NewGuid().ToString()));
            command.Parameters.Add(new SqliteParameter("@Type", "TestNotification"));
            command.Parameters.Add(new SqliteParameter("@Content", "{}"));
            command.Parameters.Add(new SqliteParameter("@Created", DateTime.UtcNow.ToString("O")));
            await command.ExecuteNonQueryAsync();

            // Return error (Left)
            return MediatorErrors.Create("test.error", "Test error");
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        Assert.True(wasHandlerCalled);
        Assert.True(result.IsLeft);

        // Verify data was rolled back
        await using var verifyCommand = ((SqliteConnection)connection).CreateCommand();
        verifyCommand.CommandText = "SELECT COUNT(*) FROM OutboxMessages";
        var count = (long)(await verifyCommand.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Handle_ExceptionInHandler_RollsBackTransaction()
    {
        // Arrange
        var connection = _database.CreateConnection();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);
        var request = new TestRequest();
        var context = RequestContext.Create();

        RequestHandlerCallback<string> next = async () =>
        {
            // Insert data within transaction
            await using var command = ((SqliteConnection)connection).CreateCommand();
            command.CommandText = "INSERT INTO OutboxMessages (Id, NotificationType, Content, CreatedAtUtc, RetryCount) VALUES (@Id, @Type, @Content, @Created, 0)";
            command.Parameters.Add(new SqliteParameter("@Id", Guid.NewGuid().ToString()));
            command.Parameters.Add(new SqliteParameter("@Type", "TestNotification"));
            command.Parameters.Add(new SqliteParameter("@Content", "{}"));
            command.Parameters.Add(new SqliteParameter("@Created", DateTime.UtcNow.ToString("O")));
            await command.ExecuteNonQueryAsync();

            // Throw exception
            throw new InvalidOperationException("Test exception");
        };

        // Act & Assert - Exception should propagate
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.Handle(request, context, next, CancellationToken.None));

        // Verify data was rolled back
        await using var verifyCommand = ((SqliteConnection)connection).CreateCommand();
        verifyCommand.CommandText = "SELECT COUNT(*) FROM OutboxMessages";
        var count = (long)(await verifyCommand.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Handle_ClosedConnection_OpensConnection()
    {
        // Arrange
        var connection = _database.CreateConnection();
        connection.Close(); // Ensure closed
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);
        var request = new TestRequest();
        var context = RequestContext.Create();

        RequestHandlerCallback<string> next = async () =>
        {
            // Verify connection is open
            Assert.Equal(ConnectionState.Open, connection.State);
            await Task.CompletedTask;
            return Right<MediatorError, string>("Success");
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        Assert.True(result.IsRight);
    }

    [Fact]
    public async Task Handle_MultipleOperations_AllCommittedOrRolledBackTogether()
    {
        // Arrange
        var connection = _database.CreateConnection();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);
        var request = new TestRequest();
        var context = RequestContext.Create();

        RequestHandlerCallback<string> next = async () =>
        {
            // Insert into Outbox
            await using var cmd1 = ((SqliteConnection)connection).CreateCommand();
            cmd1.CommandText = "INSERT INTO OutboxMessages (Id, NotificationType, Content, CreatedAtUtc, RetryCount) VALUES (@Id, @Type, @Content, @Created, 0)";
            cmd1.Parameters.Add(new SqliteParameter("@Id", Guid.NewGuid().ToString()));
            cmd1.Parameters.Add(new SqliteParameter("@Type", "TestNotification"));
            cmd1.Parameters.Add(new SqliteParameter("@Content", "{}"));
            cmd1.Parameters.Add(new SqliteParameter("@Created", DateTime.UtcNow.ToString("O")));
            await cmd1.ExecuteNonQueryAsync();

            // Insert into Inbox
            await using var cmd2 = ((SqliteConnection)connection).CreateCommand();
            cmd2.CommandText = "INSERT INTO InboxMessages (MessageId, RequestType, ReceivedAtUtc, RetryCount, ExpiresAtUtc) VALUES (@Id, @Type, @Received, 0, @Expires)";
            cmd2.Parameters.Add(new SqliteParameter("@Id", "test-message"));
            cmd2.Parameters.Add(new SqliteParameter("@Type", "TestRequest"));
            cmd2.Parameters.Add(new SqliteParameter("@Received", DateTime.UtcNow.ToString("O")));
            cmd2.Parameters.Add(new SqliteParameter("@Expires", DateTime.UtcNow.AddDays(7).ToString("O")));
            await cmd2.ExecuteNonQueryAsync();

            return Right<MediatorError, string>("Success");
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        Assert.True(result.IsRight);

        // Verify both insertions committed
        await using var verifyCmd1 = ((SqliteConnection)connection).CreateCommand();
        verifyCmd1.CommandText = "SELECT COUNT(*) FROM OutboxMessages";
        var outboxCount = (long)(await verifyCmd1.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(1, outboxCount);

        await using var verifyCmd2 = ((SqliteConnection)connection).CreateCommand();
        verifyCmd2.CommandText = "SELECT COUNT(*) FROM InboxMessages";
        var inboxCount = (long)(await verifyCmd2.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(1, inboxCount);
    }

    [Fact]
    public async Task Handle_MultipleOperationsWithError_AllRolledBack()
    {
        // Arrange
        var connection = _database.CreateConnection();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);
        var request = new TestRequest();
        var context = RequestContext.Create();

        RequestHandlerCallback<string> next = async () =>
        {
            // Insert into Outbox
            await using var cmd1 = ((SqliteConnection)connection).CreateCommand();
            cmd1.CommandText = "INSERT INTO OutboxMessages (Id, NotificationType, Content, CreatedAtUtc, RetryCount) VALUES (@Id, @Type, @Content, @Created, 0)";
            cmd1.Parameters.Add(new SqliteParameter("@Id", Guid.NewGuid().ToString()));
            cmd1.Parameters.Add(new SqliteParameter("@Type", "TestNotification"));
            cmd1.Parameters.Add(new SqliteParameter("@Content", "{}"));
            cmd1.Parameters.Add(new SqliteParameter("@Created", DateTime.UtcNow.ToString("O")));
            await cmd1.ExecuteNonQueryAsync();

            // Insert into Inbox
            await using var cmd2 = ((SqliteConnection)connection).CreateCommand();
            cmd2.CommandText = "INSERT INTO InboxMessages (MessageId, RequestType, ReceivedAtUtc, RetryCount, ExpiresAtUtc) VALUES (@Id, @Type, @Received, 0, @Expires)";
            cmd2.Parameters.Add(new SqliteParameter("@Id", "test-message"));
            cmd2.Parameters.Add(new SqliteParameter("@Type", "TestRequest"));
            cmd2.Parameters.Add(new SqliteParameter("@Received", DateTime.UtcNow.ToString("O")));
            cmd2.Parameters.Add(new SqliteParameter("@Expires", DateTime.UtcNow.AddDays(7).ToString("O")));
            await cmd2.ExecuteNonQueryAsync();

            // Return error
            return MediatorErrors.Create("test.error", "Test error");
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        Assert.True(result.IsLeft);

        // Verify both insertions rolled back
        await using var verifyCmd1 = ((SqliteConnection)connection).CreateCommand();
        verifyCmd1.CommandText = "SELECT COUNT(*) FROM OutboxMessages";
        var outboxCount = (long)(await verifyCmd1.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(0, outboxCount);

        await using var verifyCmd2 = ((SqliteConnection)connection).CreateCommand();
        verifyCmd2.CommandText = "SELECT COUNT(*) FROM InboxMessages";
        var inboxCount = (long)(await verifyCmd2.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(0, inboxCount);
    }

    // Test helpers
    private sealed record TestRequest : IRequest<string>;
}
