using Encina.Marten.Projections;

namespace Encina.Marten.Tests.Projections;

public sealed class ProjectionOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var options = new ProjectionOptions();

        // Assert
        options.Enabled.ShouldBeFalse();
        options.UseInlineProjections.ShouldBeTrue();
        options.RebuildBatchSize.ShouldBe(1000);
        options.AutoRebuildOnStartup.ShouldBeFalse();
        options.ThrowOnProjectionError.ShouldBeFalse();
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        // Act
        var options = new ProjectionOptions
        {
            Enabled = true,
            UseInlineProjections = false,
            RebuildBatchSize = 500,
            AutoRebuildOnStartup = true,
            ThrowOnProjectionError = true,
        };

        // Assert
        options.Enabled.ShouldBeTrue();
        options.UseInlineProjections.ShouldBeFalse();
        options.RebuildBatchSize.ShouldBe(500);
        options.AutoRebuildOnStartup.ShouldBeTrue();
        options.ThrowOnProjectionError.ShouldBeTrue();
    }
}
