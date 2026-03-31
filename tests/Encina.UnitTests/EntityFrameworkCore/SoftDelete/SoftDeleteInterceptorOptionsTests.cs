using Encina.EntityFrameworkCore.SoftDelete;

namespace Encina.UnitTests.EntityFrameworkCore.SoftDelete;

/// <summary>
/// Unit tests for <see cref="SoftDeleteInterceptorOptions"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SoftDeleteInterceptorOptionsTests
{
    #region Default Values

    [Fact]
    public void Enabled_DefaultIsTrue()
    {
        // Arrange
        var options = new SoftDeleteInterceptorOptions();

        // Assert
        options.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void TrackDeletedAt_DefaultIsTrue()
    {
        // Arrange
        var options = new SoftDeleteInterceptorOptions();

        // Assert
        options.TrackDeletedAt.ShouldBeTrue();
    }

    [Fact]
    public void TrackDeletedBy_DefaultIsTrue()
    {
        // Arrange
        var options = new SoftDeleteInterceptorOptions();

        // Assert
        options.TrackDeletedBy.ShouldBeTrue();
    }

    [Fact]
    public void LogSoftDeletes_DefaultIsFalse()
    {
        // Arrange
        var options = new SoftDeleteInterceptorOptions();

        // Assert
        options.LogSoftDeletes.ShouldBeFalse();
    }

    #endregion

    #region Property Setting

    [Fact]
    public void Enabled_CanBeSetToFalse()
    {
        // Arrange & Act
        var options = new SoftDeleteInterceptorOptions { Enabled = false };

        // Assert
        options.Enabled.ShouldBeFalse();
    }

    [Fact]
    public void TrackDeletedAt_CanBeSetToFalse()
    {
        // Arrange & Act
        var options = new SoftDeleteInterceptorOptions { TrackDeletedAt = false };

        // Assert
        options.TrackDeletedAt.ShouldBeFalse();
    }

    [Fact]
    public void TrackDeletedBy_CanBeSetToFalse()
    {
        // Arrange & Act
        var options = new SoftDeleteInterceptorOptions { TrackDeletedBy = false };

        // Assert
        options.TrackDeletedBy.ShouldBeFalse();
    }

    [Fact]
    public void LogSoftDeletes_CanBeSetToTrue()
    {
        // Arrange & Act
        var options = new SoftDeleteInterceptorOptions { LogSoftDeletes = true };

        // Assert
        options.LogSoftDeletes.ShouldBeTrue();
    }

    #endregion

    #region Multiple Properties

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange & Act
        var options = new SoftDeleteInterceptorOptions
        {
            Enabled = false,
            TrackDeletedAt = false,
            TrackDeletedBy = false,
            LogSoftDeletes = true
        };

        // Assert
        options.Enabled.ShouldBeFalse();
        options.TrackDeletedAt.ShouldBeFalse();
        options.TrackDeletedBy.ShouldBeFalse();
        options.LogSoftDeletes.ShouldBeTrue();
    }

    [Fact]
    public void Properties_CanBeToggledMultipleTimes()
    {
        // Arrange
        var options = new SoftDeleteInterceptorOptions();

        // Act
        options.Enabled = false;
        options.Enabled = true;
        options.LogSoftDeletes = true;
        options.LogSoftDeletes = false;

        // Assert
        options.Enabled.ShouldBeTrue();
        options.LogSoftDeletes.ShouldBeFalse();
    }

    #endregion
}
