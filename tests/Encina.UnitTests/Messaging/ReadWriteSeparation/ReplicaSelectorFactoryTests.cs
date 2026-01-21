using Encina.Messaging.ReadWriteSeparation;
using Shouldly;

namespace Encina.UnitTests.Messaging.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="ReplicaSelectorFactory"/>.
/// </summary>
public sealed class ReplicaSelectorFactoryTests
{
    private static readonly string[] TestReplicas =
    [
        "Server=replica1;",
        "Server=replica2;"
    ];

    [Fact]
    public void Create_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => ReplicaSelectorFactory.Create((ReadWriteSeparationOptions)null!));
    }

    [Fact]
    public void Create_WithEmptyReadConnectionStrings_ThrowsArgumentException()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            ReadConnectionStrings = []
        };

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => ReplicaSelectorFactory.Create(options));
        exception.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Create_WithNullReadConnectionStrings_ThrowsArgumentException()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            ReadConnectionStrings = null!
        };

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => ReplicaSelectorFactory.Create(options));
        exception.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Create_WithRoundRobinStrategy_ReturnsRoundRobinSelector()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            ReadConnectionStrings = TestReplicas.ToList(),
            ReplicaStrategy = ReplicaStrategy.RoundRobin
        };

        // Act
        var selector = ReplicaSelectorFactory.Create(options);

        // Assert
        selector.ShouldBeOfType<RoundRobinReplicaSelector>();
    }

    [Fact]
    public void Create_WithRandomStrategy_ReturnsRandomSelector()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            ReadConnectionStrings = TestReplicas.ToList(),
            ReplicaStrategy = ReplicaStrategy.Random
        };

        // Act
        var selector = ReplicaSelectorFactory.Create(options);

        // Assert
        selector.ShouldBeOfType<RandomReplicaSelector>();
    }

    [Fact]
    public void Create_WithLeastConnectionsStrategy_ReturnsLeastConnectionsSelector()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            ReadConnectionStrings = TestReplicas.ToList(),
            ReplicaStrategy = ReplicaStrategy.LeastConnections
        };

        // Act
        var selector = ReplicaSelectorFactory.Create(options);

        // Assert
        selector.ShouldBeOfType<LeastConnectionsReplicaSelector>();
    }

    [Fact]
    public void Create_WithUnknownStrategy_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            ReadConnectionStrings = TestReplicas.ToList(),
            ReplicaStrategy = (ReplicaStrategy)999
        };

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => ReplicaSelectorFactory.Create(options));
    }

    // Tests for the overload with explicit replicas and strategy

    [Fact]
    public void CreateWithReplicasAndStrategy_WithNullReplicas_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ReplicaSelectorFactory.Create(null!, ReplicaStrategy.RoundRobin));
    }

    [Fact]
    public void CreateWithReplicasAndStrategy_WithEmptyReplicas_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            ReplicaSelectorFactory.Create([], ReplicaStrategy.RoundRobin));
        exception.ParamName.ShouldBe("replicas");
    }

    [Theory]
    [InlineData(ReplicaStrategy.RoundRobin, typeof(RoundRobinReplicaSelector))]
    [InlineData(ReplicaStrategy.Random, typeof(RandomReplicaSelector))]
    [InlineData(ReplicaStrategy.LeastConnections, typeof(LeastConnectionsReplicaSelector))]
    public void CreateWithReplicasAndStrategy_ReturnsCorrectType(ReplicaStrategy strategy, Type expectedType)
    {
        // Act
        var selector = ReplicaSelectorFactory.Create(TestReplicas, strategy);

        // Assert
        selector.ShouldBeOfType(expectedType);
    }

    [Fact]
    public void CreateWithReplicasAndStrategy_WithUnknownStrategy_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            ReplicaSelectorFactory.Create(TestReplicas, (ReplicaStrategy)999));
    }

    [Fact]
    public void Create_CreatedSelectorWorks()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            ReadConnectionStrings = TestReplicas.ToList(),
            ReplicaStrategy = ReplicaStrategy.RoundRobin
        };

        // Act
        var selector = ReplicaSelectorFactory.Create(options);
        var selected = selector.SelectReplica();

        // Assert
        selected.ShouldBeOneOf(TestReplicas);
    }
}
