using Encina.Messaging.ReadWriteSeparation;
using Shouldly;

namespace Encina.UnitTests.Messaging.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="DatabaseRoutingScope"/>.
/// </summary>
public sealed class DatabaseRoutingScopeTests : IDisposable
{
    public DatabaseRoutingScopeTests()
    {
        // Ensure clean state before each test
        DatabaseRoutingContext.Clear();
    }

    public void Dispose()
    {
        // Clean up after each test
        DatabaseRoutingContext.Clear();
    }

    [Fact]
    public void Constructor_SetsCurrentIntent()
    {
        // Act
        using var scope = new DatabaseRoutingScope(DatabaseIntent.Read);

        // Assert
        DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);
    }

    [Fact]
    public void Constructor_SetsIsEnabled()
    {
        // Act
        using var scope = new DatabaseRoutingScope(DatabaseIntent.Read);

        // Assert
        DatabaseRoutingContext.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithEnabled_SetsIsEnabled()
    {
        // Act
        using var scope = new DatabaseRoutingScope(DatabaseIntent.Read, enabled: true);

        // Assert
        DatabaseRoutingContext.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithDisabled_DoesNotSetIsEnabled()
    {
        // Arrange - Ensure we start with disabled
        DatabaseRoutingContext.IsEnabled = false;

        // Act
        using var scope = new DatabaseRoutingScope(DatabaseIntent.Read, enabled: false);

        // Assert
        DatabaseRoutingContext.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Intent_ReturnsSetIntent()
    {
        // Act
        using var scope = new DatabaseRoutingScope(DatabaseIntent.ForceWrite);

        // Assert
        scope.Intent.ShouldBe(DatabaseIntent.ForceWrite);
    }

    [Fact]
    public void Dispose_RestoresPreviousIntent()
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Write;

        // Act
        using (var scope = new DatabaseRoutingScope(DatabaseIntent.Read))
        {
            DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);
        }

        // Assert
        DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Write);
    }

    [Fact]
    public void Dispose_RestoresNullIntent()
    {
        // Arrange - No previous intent
        DatabaseRoutingContext.CurrentIntent = null;

        // Act
        using (var scope = new DatabaseRoutingScope(DatabaseIntent.Read))
        {
            DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);
        }

        // Assert
        DatabaseRoutingContext.CurrentIntent.ShouldBeNull();
    }

    [Fact]
    public void Dispose_RestoresPreviousIsEnabled()
    {
        // Arrange
        DatabaseRoutingContext.IsEnabled = true;

        // Act
        using (var scope = new DatabaseRoutingScope(DatabaseIntent.Read, enabled: false))
        {
            DatabaseRoutingContext.IsEnabled.ShouldBeFalse();
        }

        // Assert
        DatabaseRoutingContext.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void NestedScopes_RestoreCorrectly()
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Write;

        // Act & Assert
        using (var outer = new DatabaseRoutingScope(DatabaseIntent.Read))
        {
            DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);

            using (var inner = new DatabaseRoutingScope(DatabaseIntent.ForceWrite))
            {
                DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.ForceWrite);
            }

            // After inner scope, should restore to outer scope's intent
            DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);
        }

        // After outer scope, should restore to original intent
        DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Write);
    }

    [Fact]
    public void ForRead_CreatesReadScope()
    {
        // Act
        using var scope = DatabaseRoutingScope.ForRead();

        // Assert
        scope.Intent.ShouldBe(DatabaseIntent.Read);
        DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);
    }

    [Fact]
    public void ForWrite_CreatesWriteScope()
    {
        // Act
        using var scope = DatabaseRoutingScope.ForWrite();

        // Assert
        scope.Intent.ShouldBe(DatabaseIntent.Write);
        DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Write);
    }

    [Fact]
    public void ForForceWrite_CreatesForceWriteScope()
    {
        // Act
        using var scope = DatabaseRoutingScope.ForForceWrite();

        // Assert
        scope.Intent.ShouldBe(DatabaseIntent.ForceWrite);
        DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.ForceWrite);
    }

    [Fact]
    public void StaticMethods_RestoreCorrectly()
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = null;

        // Act
        using (var scope = DatabaseRoutingScope.ForRead())
        {
            DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);
        }

        // Assert
        DatabaseRoutingContext.CurrentIntent.ShouldBeNull();
    }

    [Fact]
    public async Task Scope_FlowsAcrossAsyncBoundaries()
    {
        // Act & Assert
        using (var scope = new DatabaseRoutingScope(DatabaseIntent.Read))
        {
            DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);

            await Task.Yield();
            DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);

            await Task.Delay(1);
            DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);
        }

        DatabaseRoutingContext.CurrentIntent.ShouldBeNull();
    }

    [Fact]
    public void Struct_HasCorrectDefaultValue()
    {
        // Act - Default struct creation (not through constructor)
        var defaultScope = default(DatabaseRoutingScope);

        // Assert
        defaultScope.Intent.ShouldBe(DatabaseIntent.Write); // Default enum value
    }
}
