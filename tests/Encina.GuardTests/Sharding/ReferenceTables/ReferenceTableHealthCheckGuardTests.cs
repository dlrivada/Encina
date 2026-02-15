using Encina.Messaging.Health;
using Encina.Sharding.ReferenceTables;
using Encina.Sharding.ReferenceTables.Health;

namespace Encina.GuardTests.Sharding.ReferenceTables;

/// <summary>
/// Guard clause tests for <see cref="ReferenceTableHealthCheck"/>.
/// </summary>
public sealed class ReferenceTableHealthCheckGuardTests
{
    private readonly IReferenceTableRegistry _registry = Substitute.For<IReferenceTableRegistry>();
    private readonly IReferenceTableStateStore _stateStore = Substitute.For<IReferenceTableStateStore>();

    [Fact]
    public void Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ReferenceTableHealthCheck(null!, _stateStore);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("registry");
    }

    [Fact]
    public void Constructor_NullStateStore_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ReferenceTableHealthCheck(_registry, null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("stateStore");
    }

    [Fact]
    public void Constructor_NullOptions_DoesNotThrow()
    {
        // Act
        var act = () => new ReferenceTableHealthCheck(_registry, _stateStore, null);

        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public void DefaultName_IsNotNullOrWhiteSpace()
    {
        // Assert
        ReferenceTableHealthCheck.DefaultName.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void DefaultTags_IsNotEmpty()
    {
        // Assert
        ReferenceTableHealthCheck.DefaultTags.ShouldNotBeEmpty();
    }
}
