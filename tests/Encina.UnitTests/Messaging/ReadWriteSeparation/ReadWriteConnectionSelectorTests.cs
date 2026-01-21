using Encina.Messaging.ReadWriteSeparation;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Messaging.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="ReadWriteConnectionSelector"/>.
/// </summary>
public sealed class ReadWriteConnectionSelectorTests : IDisposable
{
    private const string WriteConnection = "Server=primary;Database=test;";
    private const string Replica1 = "Server=replica1;Database=test;";
    private const string Replica2 = "Server=replica2;Database=test;";

    public ReadWriteConnectionSelectorTests()
    {
        // Ensure clean state
        DatabaseRoutingContext.Clear();
    }

    public void Dispose()
    {
        DatabaseRoutingContext.Clear();
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReadWriteConnectionSelector((IOptions<ReadWriteSeparationOptions>)null!));
    }

    [Fact]
    public void Constructor_WithNullDirectOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReadWriteConnectionSelector((ReadWriteSeparationOptions)null!));
    }

    [Fact]
    public void Constructor_WithValidOptions_Succeeds()
    {
        // Arrange
        var options = CreateOptions(WriteConnection);

        // Act & Assert - Should not throw
        var selector = new ReadWriteConnectionSelector(options);
        selector.ShouldNotBeNull();
    }

    [Fact]
    public void HasReadReplicas_WhenNoReplicas_ReturnsFalse()
    {
        // Arrange
        var options = CreateOptions(WriteConnection);
        var selector = new ReadWriteConnectionSelector(options);

        // Assert
        selector.HasReadReplicas.ShouldBeFalse();
    }

    [Fact]
    public void HasReadReplicas_WhenReplicasConfigured_ReturnsTrue()
    {
        // Arrange
        var options = CreateOptions(WriteConnection, Replica1, Replica2);
        var replicaSelector = Substitute.For<IReplicaSelector>();
        var selector = new ReadWriteConnectionSelector(options, replicaSelector);

        // Assert
        selector.HasReadReplicas.ShouldBeTrue();
    }

    [Fact]
    public void GetWriteConnectionString_ReturnsWriteConnection()
    {
        // Arrange
        var options = CreateOptions(WriteConnection);
        var selector = new ReadWriteConnectionSelector(options);

        // Act
        var result = selector.GetWriteConnectionString();

        // Assert
        result.ShouldBe(WriteConnection);
    }

    [Fact]
    public void GetWriteConnectionString_WhenNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = CreateOptions(null);
        var selector = new ReadWriteConnectionSelector(options);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => selector.GetWriteConnectionString());
        ex.Message.ShouldContain("WriteConnectionString");
    }

    [Fact]
    public void GetWriteConnectionString_WhenEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = CreateOptions("");
        var selector = new ReadWriteConnectionSelector(options);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => selector.GetWriteConnectionString());
    }

    [Fact]
    public void GetWriteConnectionString_WhenWhitespace_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = CreateOptions("   ");
        var selector = new ReadWriteConnectionSelector(options);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => selector.GetWriteConnectionString());
    }

    [Fact]
    public void GetReadConnectionString_WhenNoReplicas_ReturnsWriteConnection()
    {
        // Arrange
        var options = CreateOptions(WriteConnection);
        var selector = new ReadWriteConnectionSelector(options);

        // Act
        var result = selector.GetReadConnectionString();

        // Assert
        result.ShouldBe(WriteConnection);
    }

    [Fact]
    public void GetReadConnectionString_WhenReplicasConfigured_UsesReplicaSelector()
    {
        // Arrange
        var options = CreateOptions(WriteConnection, Replica1);
        var replicaSelector = Substitute.For<IReplicaSelector>();
        replicaSelector.SelectReplica().Returns(Replica1);
        var selector = new ReadWriteConnectionSelector(options, replicaSelector);

        // Act
        var result = selector.GetReadConnectionString();

        // Assert
        result.ShouldBe(Replica1);
        replicaSelector.Received(1).SelectReplica();
    }

    [Fact]
    public void GetReadConnectionString_WhenNoReplicaSelector_ReturnsWriteConnection()
    {
        // Arrange
        var options = CreateOptions(WriteConnection, Replica1);
        var selector = new ReadWriteConnectionSelector(options); // No replica selector

        // Act
        var result = selector.GetReadConnectionString();

        // Assert
        result.ShouldBe(WriteConnection);
    }

    [Fact]
    public void GetConnectionString_WhenRoutingDisabled_ReturnsWriteConnection()
    {
        // Arrange
        var options = CreateOptions(WriteConnection, Replica1);
        var replicaSelector = Substitute.For<IReplicaSelector>();
        var selector = new ReadWriteConnectionSelector(options, replicaSelector);

        DatabaseRoutingContext.IsEnabled = false;
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;

        // Act
        var result = selector.GetConnectionString();

        // Assert
        result.ShouldBe(WriteConnection);
        replicaSelector.DidNotReceive().SelectReplica();
    }

    [Fact]
    public void GetConnectionString_WhenEnabledAndReadIntent_ReturnsReadConnection()
    {
        // Arrange
        var options = CreateOptions(WriteConnection, Replica1);
        var replicaSelector = Substitute.For<IReplicaSelector>();
        replicaSelector.SelectReplica().Returns(Replica1);
        var selector = new ReadWriteConnectionSelector(options, replicaSelector);

        DatabaseRoutingContext.IsEnabled = true;
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;

        // Act
        var result = selector.GetConnectionString();

        // Assert
        result.ShouldBe(Replica1);
    }

    [Fact]
    public void GetConnectionString_WhenEnabledAndWriteIntent_ReturnsWriteConnection()
    {
        // Arrange
        var options = CreateOptions(WriteConnection, Replica1);
        var replicaSelector = Substitute.For<IReplicaSelector>();
        var selector = new ReadWriteConnectionSelector(options, replicaSelector);

        DatabaseRoutingContext.IsEnabled = true;
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Write;

        // Act
        var result = selector.GetConnectionString();

        // Assert
        result.ShouldBe(WriteConnection);
        replicaSelector.DidNotReceive().SelectReplica();
    }

    [Fact]
    public void GetConnectionString_WhenEnabledAndForceWriteIntent_ReturnsWriteConnection()
    {
        // Arrange
        var options = CreateOptions(WriteConnection, Replica1);
        var replicaSelector = Substitute.For<IReplicaSelector>();
        var selector = new ReadWriteConnectionSelector(options, replicaSelector);

        DatabaseRoutingContext.IsEnabled = true;
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.ForceWrite;

        // Act
        var result = selector.GetConnectionString();

        // Assert
        result.ShouldBe(WriteConnection);
        replicaSelector.DidNotReceive().SelectReplica();
    }

    [Fact]
    public void GetConnectionString_WhenEnabledAndNullIntent_ReturnsWriteConnection()
    {
        // Arrange
        var options = CreateOptions(WriteConnection, Replica1);
        var replicaSelector = Substitute.For<IReplicaSelector>();
        var selector = new ReadWriteConnectionSelector(options, replicaSelector);

        DatabaseRoutingContext.IsEnabled = true;
        DatabaseRoutingContext.CurrentIntent = null;

        // Act
        var result = selector.GetConnectionString();

        // Assert
        result.ShouldBe(WriteConnection);
        replicaSelector.DidNotReceive().SelectReplica();
    }

    [Fact]
    public void Constructor_WithIOptions_ExtractsValueCorrectly()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = WriteConnection,
            ReadConnectionStrings = [Replica1]
        };
        var iOptions = Options.Create(options);
        var replicaSelector = Substitute.For<IReplicaSelector>();

        // Act
        var selector = new ReadWriteConnectionSelector(iOptions, replicaSelector);

        // Assert
        selector.HasReadReplicas.ShouldBeTrue();
        selector.GetWriteConnectionString().ShouldBe(WriteConnection);
    }

    [Fact]
    public void ImplementsInterface()
    {
        // Arrange
        var options = CreateOptions(WriteConnection);

        // Act
        IReadWriteConnectionSelector selector = new ReadWriteConnectionSelector(options);

        // Assert
        selector.ShouldNotBeNull();
    }

    private static ReadWriteSeparationOptions CreateOptions(
        string? writeConnection,
        params string[] readConnections)
    {
        return new ReadWriteSeparationOptions
        {
            WriteConnectionString = writeConnection,
            ReadConnectionStrings = readConnections.ToList()
        };
    }
}
