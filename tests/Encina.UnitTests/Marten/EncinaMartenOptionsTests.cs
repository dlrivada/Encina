using Encina.Marten;

namespace Encina.UnitTests.Marten;

public sealed class EncinaMartenOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new EncinaMartenOptions();

        options.AutoPublishDomainEvents.ShouldBeTrue();
        options.UseOptimisticConcurrency.ShouldBeTrue();
        options.ThrowOnConcurrencyConflict.ShouldBeFalse();
        options.StreamPrefix.ShouldBeEmpty();
        options.ProviderHealthCheck.ShouldNotBeNull();
        options.Projections.ShouldNotBeNull();
        options.Snapshots.ShouldNotBeNull();
        options.EventVersioning.ShouldNotBeNull();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new EncinaMartenOptions
        {
            AutoPublishDomainEvents = false,
            UseOptimisticConcurrency = false,
            ThrowOnConcurrencyConflict = true,
            StreamPrefix = "test_"
        };

        options.AutoPublishDomainEvents.ShouldBeFalse();
        options.UseOptimisticConcurrency.ShouldBeFalse();
        options.ThrowOnConcurrencyConflict.ShouldBeTrue();
        options.StreamPrefix.ShouldBe("test_");
    }
}
