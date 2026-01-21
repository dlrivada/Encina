using Encina.Messaging.ReadWriteSeparation;
using Shouldly;

namespace Encina.UnitTests.Messaging.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="DatabaseRoutingContext"/>.
/// </summary>
public sealed class DatabaseRoutingContextTests : IDisposable
{
    public DatabaseRoutingContextTests()
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
    public void CurrentIntent_InitiallyIsNull()
    {
        DatabaseRoutingContext.CurrentIntent.ShouldBeNull();
    }

    [Fact]
    public void CurrentIntent_CanBeSet()
    {
        // Act
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;

        // Assert
        DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);
    }

    [Theory]
    [InlineData(DatabaseIntent.Write)]
    [InlineData(DatabaseIntent.Read)]
    [InlineData(DatabaseIntent.ForceWrite)]
    public void CurrentIntent_CanBeSetToAnyValue(DatabaseIntent intent)
    {
        // Act
        DatabaseRoutingContext.CurrentIntent = intent;

        // Assert
        DatabaseRoutingContext.CurrentIntent.ShouldBe(intent);
    }

    [Fact]
    public void CurrentIntent_CanBeSetToNull()
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;

        // Act
        DatabaseRoutingContext.CurrentIntent = null;

        // Assert
        DatabaseRoutingContext.CurrentIntent.ShouldBeNull();
    }

    [Fact]
    public void IsEnabled_InitiallyIsFalse()
    {
        DatabaseRoutingContext.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void IsEnabled_CanBeSet()
    {
        // Act
        DatabaseRoutingContext.IsEnabled = true;

        // Assert
        DatabaseRoutingContext.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void HasIntent_WhenIntentIsNull_ReturnsFalse()
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = null;

        // Assert
        DatabaseRoutingContext.HasIntent.ShouldBeFalse();
    }

    [Fact]
    public void HasIntent_WhenIntentIsSet_ReturnsTrue()
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;

        // Assert
        DatabaseRoutingContext.HasIntent.ShouldBeTrue();
    }

    [Fact]
    public void EffectiveIntent_WhenNull_ReturnsWrite()
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = null;

        // Assert
        DatabaseRoutingContext.EffectiveIntent.ShouldBe(DatabaseIntent.Write);
    }

    [Theory]
    [InlineData(DatabaseIntent.Write)]
    [InlineData(DatabaseIntent.Read)]
    [InlineData(DatabaseIntent.ForceWrite)]
    public void EffectiveIntent_WhenSet_ReturnsSetValue(DatabaseIntent intent)
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = intent;

        // Assert
        DatabaseRoutingContext.EffectiveIntent.ShouldBe(intent);
    }

    [Fact]
    public void IsReadIntent_WhenRead_ReturnsTrue()
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;

        // Assert
        DatabaseRoutingContext.IsReadIntent.ShouldBeTrue();
    }

    [Theory]
    [InlineData(DatabaseIntent.Write)]
    [InlineData(DatabaseIntent.ForceWrite)]
    public void IsReadIntent_WhenNotRead_ReturnsFalse(DatabaseIntent intent)
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = intent;

        // Assert
        DatabaseRoutingContext.IsReadIntent.ShouldBeFalse();
    }

    [Fact]
    public void IsReadIntent_WhenNull_ReturnsFalse()
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = null;

        // Assert
        DatabaseRoutingContext.IsReadIntent.ShouldBeFalse();
    }

    [Fact]
    public void IsWriteIntent_WhenWrite_ReturnsTrue()
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Write;

        // Assert
        DatabaseRoutingContext.IsWriteIntent.ShouldBeTrue();
    }

    [Fact]
    public void IsWriteIntent_WhenForceWrite_ReturnsTrue()
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.ForceWrite;

        // Assert
        DatabaseRoutingContext.IsWriteIntent.ShouldBeTrue();
    }

    [Fact]
    public void IsWriteIntent_WhenNull_ReturnsTrue()
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = null;

        // Assert - Null defaults to write for safety
        DatabaseRoutingContext.IsWriteIntent.ShouldBeTrue();
    }

    [Fact]
    public void IsWriteIntent_WhenRead_ReturnsFalse()
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;

        // Assert
        DatabaseRoutingContext.IsWriteIntent.ShouldBeFalse();
    }

    [Fact]
    public void Clear_ResetsIntent()
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;

        // Act
        DatabaseRoutingContext.Clear();

        // Assert
        DatabaseRoutingContext.CurrentIntent.ShouldBeNull();
    }

    [Fact]
    public void Clear_ResetsIsEnabled()
    {
        // Arrange
        DatabaseRoutingContext.IsEnabled = true;

        // Act
        DatabaseRoutingContext.Clear();

        // Assert
        DatabaseRoutingContext.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task CurrentIntent_FlowsAcrossAsyncBoundaries()
    {
        // Arrange
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;

        // Act & Assert - Value should flow across await
        await Task.Yield();
        DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);

        await Task.Delay(1);
        DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);
    }

    [Fact]
    public void CurrentIntent_IsIsolatedBetweenThreads()
    {
        // Arrange
        var barrier = new Barrier(2);
        DatabaseIntent? thread1Intent = null;
        DatabaseIntent? thread2Intent = null;

        // Act
        var t1 = Task.Run(() =>
        {
            DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;
            barrier.SignalAndWait();
            Thread.Sleep(10);
            thread1Intent = DatabaseRoutingContext.CurrentIntent;
        });

        var t2 = Task.Run(() =>
        {
            DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Write;
            barrier.SignalAndWait();
            Thread.Sleep(10);
            thread2Intent = DatabaseRoutingContext.CurrentIntent;
        });

        Task.WaitAll(t1, t2);

        // Assert - Each thread should maintain its own value
        thread1Intent.ShouldBe(DatabaseIntent.Read);
        thread2Intent.ShouldBe(DatabaseIntent.Write);
    }
}
