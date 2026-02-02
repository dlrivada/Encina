using Encina.DomainModeling;
using Encina.Marten;
using Encina.Marten.Snapshots;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.GuardTests.Infrastructure.Marten;

/// <summary>
/// Guard tests for <see cref="SnapshotAwareAggregateRepository{TAggregate}"/> to verify null parameter handling.
/// </summary>
public class SnapshotAwareAggregateRepositoryGuardTests
{
    private readonly IDocumentSession _mockSession;
    private readonly ISnapshotStore<TestAggregate> _mockSnapshotStore;
    private readonly IRequestContext _mockRequestContext;
    private readonly ILogger<SnapshotAwareAggregateRepository<TestAggregate>> _mockLogger;
    private readonly IOptions<EncinaMartenOptions> _mockOptions;

    public SnapshotAwareAggregateRepositoryGuardTests()
    {
        _mockSession = Substitute.For<IDocumentSession>();
        _mockSnapshotStore = Substitute.For<ISnapshotStore<TestAggregate>>();
        _mockRequestContext = Substitute.For<IRequestContext>();
        _mockLogger = Substitute.For<ILogger<SnapshotAwareAggregateRepository<TestAggregate>>>();
        _mockOptions = Options.Create(new EncinaMartenOptions());
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when session is null.
    /// </summary>
    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SnapshotAwareAggregateRepository<TestAggregate>(
            null!,
            _mockSnapshotStore,
            _mockRequestContext,
            _mockLogger,
            _mockOptions);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("session");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when snapshotStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullSnapshotStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SnapshotAwareAggregateRepository<TestAggregate>(
            _mockSession,
            null!,
            _mockRequestContext,
            _mockLogger,
            _mockOptions);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("snapshotStore");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when requestContext is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRequestContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SnapshotAwareAggregateRepository<TestAggregate>(
            _mockSession,
            _mockSnapshotStore,
            null!,
            _mockLogger,
            _mockOptions);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("requestContext");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SnapshotAwareAggregateRepository<TestAggregate>(
            _mockSession,
            _mockSnapshotStore,
            _mockRequestContext,
            null!,
            _mockOptions);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SnapshotAwareAggregateRepository<TestAggregate>(
            _mockSession,
            _mockSnapshotStore,
            _mockRequestContext,
            _mockLogger,
            null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that SaveAsync throws ArgumentNullException when aggregate is null.
    /// </summary>
    [Fact]
    public async Task SaveAsync_NullAggregate_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        Func<Task> act = () => repository.SaveAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("aggregate");
    }

    /// <summary>
    /// Verifies that CreateAsync throws ArgumentNullException when aggregate is null.
    /// </summary>
    [Fact]
    public async Task CreateAsync_NullAggregate_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        Func<Task> act = () => repository.CreateAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("aggregate");
    }

    private SnapshotAwareAggregateRepository<TestAggregate> CreateRepository()
    {
        return new SnapshotAwareAggregateRepository<TestAggregate>(
            _mockSession,
            _mockSnapshotStore,
            _mockRequestContext,
            _mockLogger,
            _mockOptions);
    }

    /// <summary>
    /// Test aggregate for guard tests.
    /// </summary>
    public sealed class TestAggregate : AggregateBase, ISnapshotable<TestAggregate>
    {
        public string Name { get; set; } = string.Empty;

        protected override void Apply(object domainEvent)
        {
            // No-op for testing
        }

        public static TestAggregate RestoreFromSnapshot(TestAggregate snapshot)
        {
            return new TestAggregate
            {
                Name = snapshot.Name
            };
        }
    }
}
