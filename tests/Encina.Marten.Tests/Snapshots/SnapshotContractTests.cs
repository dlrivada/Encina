using Encina.Marten.Snapshots;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Encina.Marten.Tests.Snapshots;

/// <summary>
/// Contract tests verifying ISnapshotStore implementations follow expected behavior.
/// </summary>
public sealed class SnapshotContractTests
{
    /// <summary>
    /// Verifies that ISnapshotStore implementations return Either types correctly.
    /// </summary>
    [Fact]
    public void ISnapshotStore_GetLatestAsync_ReturnsEither()
    {
        // Arrange
        var session = Substitute.For<IDocumentSession>();
        var logger = Substitute.For<ILogger<MartenSnapshotStore<TestSnapshotableAggregate>>>();
        ISnapshotStore<TestSnapshotableAggregate> store =
            new MartenSnapshotStore<TestSnapshotableAggregate>(session, logger);

        // Assert - interface method returns Either
        var method = typeof(ISnapshotStore<TestSnapshotableAggregate>)
            .GetMethod(nameof(ISnapshotStore<TestSnapshotableAggregate>.GetLatestAsync));

        method.Should().NotBeNull();
        method!.ReturnType.Should().BeAssignableTo(
            typeof(Task<Either<EncinaError, Snapshot<TestSnapshotableAggregate>?>>));
    }

    /// <summary>
    /// Verifies that ISnapshotStore implementations return Either types correctly.
    /// </summary>
    [Fact]
    public void ISnapshotStore_SaveAsync_ReturnsEither()
    {
        // Assert
        var method = typeof(ISnapshotStore<TestSnapshotableAggregate>)
            .GetMethod(nameof(ISnapshotStore<TestSnapshotableAggregate>.SaveAsync));

        method.Should().NotBeNull();
        method!.ReturnType.Should().BeAssignableTo(
            typeof(Task<Either<EncinaError, Unit>>));
    }

    /// <summary>
    /// Verifies that ISnapshotStore implementations return Either types correctly.
    /// </summary>
    [Fact]
    public void ISnapshotStore_PruneAsync_ReturnsEither()
    {
        // Assert
        var method = typeof(ISnapshotStore<TestSnapshotableAggregate>)
            .GetMethod(nameof(ISnapshotStore<TestSnapshotableAggregate>.PruneAsync));

        method.Should().NotBeNull();
        method!.ReturnType.Should().BeAssignableTo(
            typeof(Task<Either<EncinaError, int>>));
    }

    /// <summary>
    /// Verifies that ISnapshot interface exposes required properties.
    /// </summary>
    [Fact]
    public void ISnapshot_HasRequiredProperties()
    {
        // Arrange
        var interfaceType = typeof(ISnapshot<TestSnapshotableAggregate>);

        // Assert
        interfaceType.GetProperty(nameof(ISnapshot<TestSnapshotableAggregate>.AggregateId))
            .Should().NotBeNull();
        interfaceType.GetProperty(nameof(ISnapshot<TestSnapshotableAggregate>.Version))
            .Should().NotBeNull();
        interfaceType.GetProperty(nameof(ISnapshot<TestSnapshotableAggregate>.CreatedAtUtc))
            .Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that Snapshot implements ISnapshot interface.
    /// </summary>
    [Fact]
    public void Snapshot_ImplementsISnapshot()
    {
        // Assert
        typeof(Snapshot<TestSnapshotableAggregate>)
            .Should().Implement<ISnapshot<TestSnapshotableAggregate>>();
    }

    /// <summary>
    /// Verifies that ISnapshotable is a marker interface with no methods.
    /// </summary>
    [Fact]
    public void ISnapshotable_IsMarkerInterface()
    {
        // Arrange
        var interfaceType = typeof(ISnapshotable<TestSnapshotableAggregate>);

        // Assert
        interfaceType.GetMethods()
            .Where(m => !m.IsSpecialName)
            .Should().BeEmpty("ISnapshotable should be a marker interface");

        interfaceType.GetProperties()
            .Should().BeEmpty("ISnapshotable should have no properties");
    }

    /// <summary>
    /// Verifies that TestSnapshotableAggregate implements required interfaces.
    /// </summary>
    [Fact]
    public void SnapshotableAggregate_ImplementsRequiredInterfaces()
    {
        // Assert
        typeof(TestSnapshotableAggregate)
            .Should().Implement<IAggregate>();
        typeof(TestSnapshotableAggregate)
            .Should().Implement<ISnapshotable<TestSnapshotableAggregate>>();
    }

    /// <summary>
    /// Verifies that SnapshotOptions exposes configuration fluently.
    /// </summary>
    [Fact]
    public void SnapshotOptions_ConfigureAggregate_ReturnsSelfForChaining()
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act
        var result = options.ConfigureAggregate<TestSnapshotableAggregate>(snapshotEvery: 50);

        // Assert
        result.Should().BeSameAs(options);
    }

    /// <summary>
    /// Verifies that MartenSnapshotStore implements ISnapshotStore.
    /// </summary>
    [Fact]
    public void MartenSnapshotStore_ImplementsISnapshotStore()
    {
        // Assert
        typeof(MartenSnapshotStore<TestSnapshotableAggregate>)
            .Should().Implement<ISnapshotStore<TestSnapshotableAggregate>>();
    }

    /// <summary>
    /// Verifies that SnapshotAwareAggregateRepository implements IAggregateRepository.
    /// </summary>
    [Fact]
    public void SnapshotAwareAggregateRepository_ImplementsIAggregateRepository()
    {
        // Assert
        typeof(SnapshotAwareAggregateRepository<TestSnapshotableAggregate>)
            .Should().Implement<IAggregateRepository<TestSnapshotableAggregate>>();
    }

    /// <summary>
    /// Verifies SnapshotEnvelope factory method exists on non-generic class.
    /// </summary>
    [Fact]
    public void SnapshotEnvelope_Factory_ExistsOnNonGenericClass()
    {
        // Arrange
        var factoryType = typeof(SnapshotEnvelope);

        // Assert
        factoryType.IsAbstract.Should().BeTrue("static class should be abstract");
        factoryType.IsSealed.Should().BeTrue("static class should be sealed");

        var createMethod = factoryType.GetMethod("Create");
        createMethod.Should().NotBeNull();
        createMethod!.IsGenericMethod.Should().BeTrue();
    }
}
