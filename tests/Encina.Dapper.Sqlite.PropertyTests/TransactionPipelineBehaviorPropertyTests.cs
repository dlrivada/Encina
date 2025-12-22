using System.Data;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;
using Microsoft.Data.Sqlite;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.Tests;

/// <summary>
/// Property-based tests for <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/>.
/// Tests invariants of transaction behavior across different scenarios.
/// </summary>
[Trait("Category", "Property")]
public sealed class TransactionPipelineBehaviorPropertyTests : IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _database;

    public TransactionPipelineBehaviorPropertyTests(SqliteFixture database)
    {
        _database = database;
        _database.ClearAllDataAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property: Success (Right) always commits the transaction.
    /// Invariant: Result.IsRight => Data persisted.
    /// </summary>
    [Theory]
    [InlineData("Data1")]
    [InlineData("Data2")]
    [InlineData("Data3")]
    public async Task Success_AlwaysCommitsData(string testData)
    {
        // Arrange
        var connection = _database.CreateConnection();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);
        var messageId = Guid.NewGuid().ToString();

        RequestHandlerCallback<string> next = async () =>
        {
            await InsertTestDataAsync(connection, messageId, testData);
            return Right<EncinaError, string>("Success");
        };

        // Act
        var result = await behavior.Handle(
            new TestRequest(),
            RequestContext.Create(),
            next,
            CancellationToken.None);

        // Assert - Data should be committed
        Assert.True(result.IsRight);
        var exists = await DataExistsAsync(connection, messageId);
        Assert.True(exists);
    }

    /// <summary>
    /// Property: Failure (Left) always rolls back the transaction.
    /// Invariant: Result.IsLeft => No data persisted.
    /// </summary>
    [Theory]
    [InlineData("error1", "Error message 1")]
    [InlineData("error2", "Error message 2")]
    [InlineData("error3", "Error message 3")]
    public async Task Failure_AlwaysRollsBackData(string errorCode, string errorMessage)
    {
        // Arrange
        var connection = _database.CreateConnection();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);
        var messageId = Guid.NewGuid().ToString();

        RequestHandlerCallback<string> next = async () =>
        {
            await InsertTestDataAsync(connection, messageId, "test-data");
            return EncinaErrors.Create(errorCode, errorMessage);
        };

        // Act
        var result = await behavior.Handle(
            new TestRequest(),
            RequestContext.Create(),
            next,
            CancellationToken.None);

        // Assert - Data should be rolled back
        Assert.True(result.IsLeft);
        var exists = await DataExistsAsync(connection, messageId);
        Assert.False(exists);
    }

    /// <summary>
    /// Property: Exception always rolls back the transaction and propagates.
    /// Invariant: Exception thrown => No data persisted AND exception propagated.
    /// </summary>
    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(ArgumentException))]
    [InlineData(typeof(NotSupportedException))]
    public async Task Exception_AlwaysRollsBackAndPropagates(Type exceptionType)
    {
        // Arrange
        var connection = _database.CreateConnection();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);
        var messageId = Guid.NewGuid().ToString();

        RequestHandlerCallback<string> next = async () =>
        {
            await InsertTestDataAsync(connection, messageId, "test-data");
            throw (Exception)Activator.CreateInstance(exceptionType, "Test exception")!;
        };

        // Act & Assert - Exception should propagate
        var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await behavior.Handle(
                new TestRequest(),
                RequestContext.Create(),
                next,
                CancellationToken.None));

        Assert.IsType(exceptionType, exception);

        // Data should be rolled back
        var exists = await DataExistsAsync(connection, messageId);
        Assert.False(exists);
    }

    /// <summary>
    /// Property: Multiple operations are atomic (all-or-nothing).
    /// Invariant: (Op1 AND Op2 AND ... AND OpN AND Success) => All committed.
    ///            (Op1 AND Op2 AND ... AND Failure) => None committed.
    /// </summary>
    [Theory]
    [InlineData(true, 3)]  // Success with 3 operations
    [InlineData(false, 3)] // Failure with 3 operations
    [InlineData(true, 5)]  // Success with 5 operations
    [InlineData(false, 5)] // Failure with 5 operations
    public async Task MultipleOperations_AreAtomic(bool shouldSucceed, int operationCount)
    {
        // Arrange
        var connection = _database.CreateConnection();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);
        var messageIds = Enumerable.Range(0, operationCount)
            .Select(_ => Guid.NewGuid().ToString())
            .ToList();

        RequestHandlerCallback<string> next = async () =>
        {
            // Perform multiple insertions
            foreach (var messageId in messageIds)
            {
                await InsertTestDataAsync(connection, messageId, "test-data");
            }

            return shouldSucceed
                ? Right<EncinaError, string>("Success")
                : EncinaErrors.Create("test.error", "Test error");
        };

        // Act
        var result = await behavior.Handle(
            new TestRequest(),
            RequestContext.Create(),
            next,
            CancellationToken.None);

        // Assert - Either all committed or none
        Assert.Equal(shouldSucceed, result.IsRight);
        foreach (var messageId in messageIds)
        {
            var exists = await DataExistsAsync(connection, messageId);
            Assert.Equal(shouldSucceed, exists);
        }
    }

    /// <summary>
    /// Property: Behavior is idempotent (can be called multiple times safely).
    /// Invariant: Calling behavior N times with same handler produces N independent transactions.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Behavior_IsIdempotent(int invocationCount)
    {
        // Arrange
        var connection = _database.CreateConnection();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);

        // Act - Call behavior multiple times
        for (var i = 0; i < invocationCount; i++)
        {
            var messageId = Guid.NewGuid().ToString();
            RequestHandlerCallback<string> next = async () =>
            {
                await InsertTestDataAsync(connection, messageId, $"data-{i}");
                return Right<EncinaError, string>("Success");
            };

            var result = await behavior.Handle(
                new TestRequest(),
                RequestContext.Create(),
                next,
                CancellationToken.None);

            Assert.True(result.IsRight);
        }

        // Assert - Each invocation created independent transaction
        await using var cmd = ((SqliteConnection)connection).CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM OutboxMessages";
        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(invocationCount, count);
    }

    /// <summary>
    /// Property: Connection state does not affect transaction behavior.
    /// Invariant: Closed connection => Opened => Transaction works correctly.
    /// </summary>
    [Theory]
    [InlineData(ConnectionState.Closed)]
    [InlineData(ConnectionState.Open)]
    public async Task ConnectionState_DoesNotAffectBehavior(ConnectionState initialState)
    {
        // Arrange
        var connection = _database.CreateConnection();
        if (initialState == ConnectionState.Closed)
            connection.Close();

        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);
        var messageId = Guid.NewGuid().ToString();

        RequestHandlerCallback<string> next = async () =>
        {
            await InsertTestDataAsync(connection, messageId, "test-data");
            return Right<EncinaError, string>("Success");
        };

        // Act
        var result = await behavior.Handle(
            new TestRequest(),
            RequestContext.Create(),
            next,
            CancellationToken.None);

        // Assert - Should work regardless of initial state
        Assert.True(result.IsRight);
        var exists = await DataExistsAsync(connection, messageId);
        Assert.True(exists);
    }

    // Helper methods
    private static async Task InsertTestDataAsync(IDbConnection connection, string id, string content)
    {
        await using var command = ((SqliteConnection)connection).CreateCommand();
        command.CommandText = "INSERT INTO OutboxMessages (Id, NotificationType, Content, CreatedAtUtc, RetryCount) VALUES (@Id, @Type, @Content, @Created, 0)";
        command.Parameters.Add(new SqliteParameter("@Id", id));
        command.Parameters.Add(new SqliteParameter("@Type", "TestNotification"));
        command.Parameters.Add(new SqliteParameter("@Content", content));
        command.Parameters.Add(new SqliteParameter("@Created", DateTime.UtcNow.ToString("O")));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> DataExistsAsync(IDbConnection connection, string id)
    {
        await using var command = ((SqliteConnection)connection).CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM OutboxMessages WHERE Id = @Id";
        command.Parameters.Add(new SqliteParameter("@Id", id));
        var count = (long)(await command.ExecuteScalarAsync() ?? 0L);
        return count > 0;
    }

    // Test helpers
    private sealed record TestRequest : IRequest<string>;
}
