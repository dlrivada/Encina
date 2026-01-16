using Encina.Testing;
using Encina.EntityFrameworkCore;
using LanguageExt;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

using EncinaRequest = global::Encina.IRequest<string>;

namespace Encina.UnitTests.EntityFrameworkCore;

/// <summary>
/// Integration tests for <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/>
/// that test the behavior using SQLite in-memory database (supports transactions).
/// </summary>
[Trait("Category", "Integration")]
public sealed class TransactionPipelineBehaviorIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly TestDbContext _dbContext;
    private readonly TransactionPipelineBehavior<TestTransactionalCommand, string> _behavior;
    private readonly TransactionPipelineBehavior<TestNonTransactionalCommand, string> _nonTransactionalBehavior;
    private readonly TransactionPipelineBehavior<TestTransactionalAttributeCommand, string> _attributeBehavior;
    private readonly IRequestContext _context;

    public TransactionPipelineBehaviorIntegrationTests()
    {
        // SQLite in-memory connection - must stay open for the duration
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseSqlite(_connection));
        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<TestDbContext>();

        // Create tables
        _dbContext.Database.EnsureCreated();

        var logger = NullLogger<TransactionPipelineBehavior<TestTransactionalCommand, string>>.Instance;
        _behavior = new TransactionPipelineBehavior<TestTransactionalCommand, string>(_dbContext, logger);

        var nonTransactionalLogger = NullLogger<TransactionPipelineBehavior<TestNonTransactionalCommand, string>>.Instance;
        _nonTransactionalBehavior = new TransactionPipelineBehavior<TestNonTransactionalCommand, string>(_dbContext, nonTransactionalLogger);

        var attributeLogger = NullLogger<TransactionPipelineBehavior<TestTransactionalAttributeCommand, string>>.Instance;
        _attributeBehavior = new TransactionPipelineBehavior<TestTransactionalAttributeCommand, string>(_dbContext, attributeLogger);

        _context = Substitute.For<IRequestContext>();
        _context.CorrelationId.Returns(Guid.NewGuid().ToString());
    }

    #region RequiresTransaction Tests

    [Fact]
    public async Task Handle_NonTransactionalCommand_SkipsTransaction()
    {
        // Arrange
        var command = new TestNonTransactionalCommand();
        var called = false;

        // Act
        var result = await _nonTransactionalBehavior.Handle(
            command,
            _context,
            () =>
            {
                called = true;
                return ValueTask.FromResult(Either<EncinaError, string>.Right("Success"));
            },
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        called.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_TransactionalCommand_ExecutesPipeline()
    {
        // Arrange
        var command = new TestTransactionalCommand();
        var called = false;

        // Act
        var result = await _behavior.Handle(
            command,
            _context,
            () =>
            {
                called = true;
                return ValueTask.FromResult(Either<EncinaError, string>.Right("Success"));
            },
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        called.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_CommandWithTransactionAttribute_ExecutesPipeline()
    {
        // Arrange
        var command = new TestTransactionalAttributeCommand();
        var called = false;

        // Act
        var result = await _attributeBehavior.Handle(
            command,
            _context,
            () =>
            {
                called = true;
                return ValueTask.FromResult(Either<EncinaError, string>.Right("Success"));
            },
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        called.ShouldBeTrue();
    }

    #endregion

    #region Transaction Behavior Tests

    [Fact]
    public async Task Handle_SuccessResult_CommitsTransaction()
    {
        // Arrange
        var command = new TestTransactionalCommand();

        // Act
        var result = await _behavior.Handle(
            command,
            _context,
            () => ValueTask.FromResult(Either<EncinaError, string>.Right("Success")),
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        // No transaction should be active after commit
        _dbContext.Database.CurrentTransaction.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_FailureResult_RollsBackTransaction()
    {
        // Arrange
        var command = new TestTransactionalCommand();
        var error = EncinaError.New("Test error");

        // Act
        var result = await _behavior.Handle(
            command,
            _context,
            () => ValueTask.FromResult(Either<EncinaError, string>.Left(error)),
            CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        // No transaction should be active after rollback
        _dbContext.Database.CurrentTransaction.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_ExistingTransaction_ReusesTransaction()
    {
        // Arrange
        var command = new TestTransactionalCommand();
        await using var existingTransaction = await _dbContext.Database.BeginTransactionAsync();

        // Act
        var result = await _behavior.Handle(
            command,
            _context,
            () => ValueTask.FromResult(Either<EncinaError, string>.Right("Success")),
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        // Original transaction should still be active
        _dbContext.Database.CurrentTransaction.ShouldNotBeNull();
    }

    #endregion

    #region Argument Validation Tests

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _behavior.Handle(
                null!,
                _context,
                () => ValueTask.FromResult(Either<EncinaError, string>.Right("Success")),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var command = new TestTransactionalCommand();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _behavior.Handle(
                command,
                null!,
                () => ValueTask.FromResult(Either<EncinaError, string>.Right("Success")),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        // Arrange
        var command = new TestTransactionalCommand();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _behavior.Handle(
                command,
                _context,
                null!,
                CancellationToken.None).AsTask());
    }

    #endregion

    #region Result Handling Tests

    [Fact]
    public async Task Handle_SuccessfulResult_ReturnsRight()
    {
        // Arrange
        var command = new TestTransactionalCommand();
        const string expectedResult = "Operation successful";

        // Act
        var result = await _behavior.Handle(
            command,
            _context,
            () => ValueTask.FromResult(Either<EncinaError, string>.Right(expectedResult)),
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: value =>
            {
                value.ShouldBe(expectedResult);
                return Unit.Default;
            },
            Left: _ => throw new InvalidOperationException("Should be Right"));
    }

    [Fact]
    public async Task Handle_FailureResult_ReturnsLeft()
    {
        // Arrange
        var command = new TestTransactionalCommand();
        var expectedError = EncinaError.New("Validation failed");

        // Act
        var result = await _behavior.Handle(
            command,
            _context,
            () => ValueTask.FromResult(Either<EncinaError, string>.Left(expectedError)),
            CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task Handle_ExceptionInPipeline_RollsBackAndRethrows()
    {
        // Arrange
        var command = new TestTransactionalCommand();
        var expectedException = new InvalidOperationException("Test exception");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            _behavior.Handle(
                command,
                _context,
                () => throw expectedException,
                CancellationToken.None).AsTask());

        ex.Message.ShouldBe("Test exception");
        // No transaction should be active after rollback
        _dbContext.Database.CurrentTransaction.ShouldBeNull();
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
        _connection.Dispose();
    }

    #region Test Commands

    private sealed record TestTransactionalCommand : EncinaRequest, ITransactionalCommand;

    private sealed record TestNonTransactionalCommand : EncinaRequest;

    [Transaction]
    private sealed record TestTransactionalAttributeCommand : EncinaRequest;

    #endregion
}
