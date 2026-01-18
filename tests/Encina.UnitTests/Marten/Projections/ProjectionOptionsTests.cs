using Encina.Marten.Projections;

namespace Encina.UnitTests.Marten.Projections;

public sealed class ProjectionOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new ProjectionOptions();

        options.Enabled.ShouldBeFalse();
        options.UseInlineProjections.ShouldBeTrue();
        options.RebuildBatchSize.ShouldBe(1000);
        options.AutoRebuildOnStartup.ShouldBeFalse();
        options.ThrowOnProjectionError.ShouldBeFalse();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new ProjectionOptions
        {
            Enabled = true,
            UseInlineProjections = false,
            RebuildBatchSize = 500,
            AutoRebuildOnStartup = true,
            ThrowOnProjectionError = true
        };

        options.Enabled.ShouldBeTrue();
        options.UseInlineProjections.ShouldBeFalse();
        options.RebuildBatchSize.ShouldBe(500);
        options.AutoRebuildOnStartup.ShouldBeTrue();
        options.ThrowOnProjectionError.ShouldBeTrue();
    }
}
