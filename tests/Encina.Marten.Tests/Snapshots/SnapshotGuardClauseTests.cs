using Encina.Marten.Snapshots;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.Marten.Tests.Snapshots;

/// <summary>
/// Guard clause tests verifying null checks and parameter validation.
/// </summary>
public sealed class SnapshotGuardClauseTests
{
    #region Snapshot Guard Clauses

    [Fact]
    public void Snapshot_NullState_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new Snapshot<TestSnapshotableAggregate>(
            Guid.NewGuid(),
            10,
            null!,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("state");
    }

    #endregion

    #region SnapshotEnvelope Guard Clauses

    [Fact]
    public void SnapshotEnvelope_Create_NullSnapshot_ThrowsArgumentNullException()
    {
        // Act
        var act = () => SnapshotEnvelope.Create<TestSnapshotableAggregate>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("snapshot");
    }

    #endregion

    #region SnapshotOptions Guard Clauses

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void SnapshotOptions_ConfigureAggregate_InvalidSnapshotEvery_ThrowsArgumentOutOfRangeException(
        int invalidValue)
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act
        var act = () => options.ConfigureAggregate<TestSnapshotableAggregate>(
            snapshotEvery: invalidValue);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void SnapshotOptions_ConfigureAggregate_NegativeKeepSnapshots_ThrowsArgumentOutOfRangeException(
        int invalidValue)
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act
        var act = () => options.ConfigureAggregate<TestSnapshotableAggregate>(
            snapshotEvery: 50,
            keepSnapshots: invalidValue);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }


    #endregion

    #region MartenSnapshotStore Guard Clauses

    [Fact]
    public void MartenSnapshotStore_NullSession_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<MartenSnapshotStore<TestSnapshotableAggregate>>>();

        // Act
        var act = () => new MartenSnapshotStore<TestSnapshotableAggregate>(null!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("session");
    }

    [Fact]
    public void MartenSnapshotStore_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var session = Substitute.For<IDocumentSession>();

        // Act
        var act = () => new MartenSnapshotStore<TestSnapshotableAggregate>(session, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task MartenSnapshotStore_SaveAsync_NullSnapshot_ThrowsArgumentNullException()
    {
        // Arrange
        var session = Substitute.For<IDocumentSession>();
        var logger = Substitute.For<ILogger<MartenSnapshotStore<TestSnapshotableAggregate>>>();
        var store = new MartenSnapshotStore<TestSnapshotableAggregate>(session, logger);

        // Act
        var act = async () => await store.SaveAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("snapshot");
    }

    [Fact]
    public async Task MartenSnapshotStore_PruneAsync_NegativeKeepCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var session = Substitute.For<IDocumentSession>();
        var logger = Substitute.For<ILogger<MartenSnapshotStore<TestSnapshotableAggregate>>>();
        var store = new MartenSnapshotStore<TestSnapshotableAggregate>(session, logger);

        // Act
        var act = async () => await store.PruneAsync(Guid.NewGuid(), -1);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    #endregion

    #region SnapshotAwareAggregateRepository Guard Clauses

    [Fact]
    public void SnapshotAwareAggregateRepository_NullSession_ThrowsArgumentNullException()
    {
        // Arrange
        var snapshotStore = Substitute.For<ISnapshotStore<TestSnapshotableAggregate>>();
        var logger = Substitute.For<ILogger<SnapshotAwareAggregateRepository<TestSnapshotableAggregate>>>();
        var options = Options.Create(new EncinaMartenOptions());

        // Act
        var act = () => new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
            null!,
            snapshotStore,
            logger,
            options);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("session");
    }

    [Fact]
    public void SnapshotAwareAggregateRepository_NullSnapshotStore_ThrowsArgumentNullException()
    {
        // Arrange
        var session = Substitute.For<IDocumentSession>();
        var logger = Substitute.For<ILogger<SnapshotAwareAggregateRepository<TestSnapshotableAggregate>>>();
        var options = Options.Create(new EncinaMartenOptions());

        // Act
        var act = () => new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
            session,
            null!,
            logger,
            options);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("snapshotStore");
    }

    [Fact]
    public void SnapshotAwareAggregateRepository_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var session = Substitute.For<IDocumentSession>();
        var snapshotStore = Substitute.For<ISnapshotStore<TestSnapshotableAggregate>>();
        var options = Options.Create(new EncinaMartenOptions());

        // Act
        var act = () => new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
            session,
            snapshotStore,
            null!,
            options);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void SnapshotAwareAggregateRepository_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var session = Substitute.For<IDocumentSession>();
        var snapshotStore = Substitute.For<ISnapshotStore<TestSnapshotableAggregate>>();
        var logger = Substitute.For<ILogger<SnapshotAwareAggregateRepository<TestSnapshotableAggregate>>>();

        // Act
        var act = () => new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
            session,
            snapshotStore,
            logger,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public async Task SnapshotAwareAggregateRepository_SaveAsync_NullAggregate_ThrowsArgumentNullException()
    {
        // Arrange
        var session = Substitute.For<IDocumentSession>();
        var snapshotStore = Substitute.For<ISnapshotStore<TestSnapshotableAggregate>>();
        var logger = Substitute.For<ILogger<SnapshotAwareAggregateRepository<TestSnapshotableAggregate>>>();
        var options = Options.Create(new EncinaMartenOptions());
        var repository = new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
            session, snapshotStore, logger, options);

        // Act
        var act = async () => await repository.SaveAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("aggregate");
    }

    [Fact]
    public async Task SnapshotAwareAggregateRepository_CreateAsync_NullAggregate_ThrowsArgumentNullException()
    {
        // Arrange
        var session = Substitute.For<IDocumentSession>();
        var snapshotStore = Substitute.For<ISnapshotStore<TestSnapshotableAggregate>>();
        var logger = Substitute.For<ILogger<SnapshotAwareAggregateRepository<TestSnapshotableAggregate>>>();
        var options = Options.Create(new EncinaMartenOptions());
        var repository = new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
            session, snapshotStore, logger, options);

        // Act
        var act = async () => await repository.CreateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("aggregate");
    }

    #endregion

    #region ServiceCollection Guard Clauses

    [Fact]
    public void AddSnapshotableAggregate_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        Microsoft.Extensions.DependencyInjection.IServiceCollection services = null!;

        // Act
        var act = () => services.AddSnapshotableAggregate<TestSnapshotableAggregate>();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    #endregion
}
