using Encina.Marten.Versioning;

namespace Encina.UnitTests.Marten.Versioning;

public sealed class EventVersioningOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new EventVersioningOptions();

        options.Enabled.ShouldBeFalse();
        options.ThrowOnUpcastFailure.ShouldBeTrue();
        options.AssembliesToScan.ShouldBeEmpty();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new EventVersioningOptions
        {
            Enabled = true,
            ThrowOnUpcastFailure = false
        };

        options.Enabled.ShouldBeTrue();
        options.ThrowOnUpcastFailure.ShouldBeFalse();
    }

    [Fact]
    public void ScanAssembly_AddsAssemblyToList()
    {
        var options = new EventVersioningOptions();
        var assembly = typeof(EventVersioningOptions).Assembly;

        options.ScanAssembly(assembly);

        options.AssembliesToScan.ShouldContain(assembly);
    }

    [Fact]
    public void ScanAssemblies_AddsMultipleAssemblies()
    {
        var options = new EventVersioningOptions();
        var assembly1 = typeof(EventVersioningOptions).Assembly;
        var assembly2 = typeof(string).Assembly;

        options.ScanAssemblies(assembly1, assembly2);

        options.AssembliesToScan.Count.ShouldBe(2);
        options.AssembliesToScan.ShouldContain(assembly1);
        options.AssembliesToScan.ShouldContain(assembly2);
    }

    [Fact]
    public void ScanAssembly_ReturnsOptionsForChaining()
    {
        var options = new EventVersioningOptions();
        var assembly = typeof(EventVersioningOptions).Assembly;

        var result = options.ScanAssembly(assembly);

        result.ShouldBe(options);
    }

    [Fact]
    public void ScanAssembly_ThrowsForNullAssembly()
    {
        var options = new EventVersioningOptions();

        Should.Throw<ArgumentNullException>(() => options.ScanAssembly(null!));
    }
}
