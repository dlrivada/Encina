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

    [Fact]
    public void ScanAssemblies_ThrowsForNull()
    {
        var options = new EventVersioningOptions();
        Should.Throw<ArgumentNullException>(() => options.ScanAssemblies(null!));
    }

    [Fact]
    public void ScanAssemblies_ReturnsOptionsForChaining()
    {
        var options = new EventVersioningOptions();
        var result = options.ScanAssemblies(typeof(string).Assembly);
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void AddUpcaster_Instance_NullThrows()
    {
        var options = new EventVersioningOptions();
        Should.Throw<ArgumentNullException>(() => options.AddUpcaster((IEventUpcaster)null!));
    }

    [Fact]
    public void AddUpcaster_Instance_ReturnsOptionsForChaining()
    {
        var options = new EventVersioningOptions();
        var upcaster = new LambdaEventUpcaster<OldEvt, NewEvt>(e => new NewEvt());
        var result = options.AddUpcaster(upcaster);
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void AddUpcaster_Type_NullThrows()
    {
        var options = new EventVersioningOptions();
        Should.Throw<ArgumentNullException>(() => options.AddUpcaster((Type)null!));
    }

    [Fact]
    public void AddUpcaster_Type_InvalidTypeThrows()
    {
        var options = new EventVersioningOptions();
        Should.Throw<ArgumentException>(() => options.AddUpcaster(typeof(string)));
    }

    [Fact]
    public void AddUpcaster_Type_ReturnsOptionsForChaining()
    {
        var options = new EventVersioningOptions();
        var result = options.AddUpcaster(typeof(TestUpcaster));
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void AddUpcaster_Lambda_NullFuncThrows()
    {
        var options = new EventVersioningOptions();
        Should.Throw<ArgumentNullException>(() =>
            options.AddUpcaster<OldEvt, NewEvt>(null!));
    }

    [Fact]
    public void AddUpcaster_Lambda_ReturnsOptionsForChaining()
    {
        var options = new EventVersioningOptions();
        var result = options.AddUpcaster<OldEvt, NewEvt>(e => new NewEvt());
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void AddUpcaster_Generic_ReturnsOptionsForChaining()
    {
        var options = new EventVersioningOptions();
        var result = options.AddUpcaster<TestUpcaster>();
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void ApplyTo_NullRegistryThrows()
    {
        var options = new EventVersioningOptions();
        Should.Throw<ArgumentNullException>(() => options.ApplyTo(null!));
    }

    [Fact]
    public void ApplyTo_AppliesRegistrations()
    {
        var options = new EventVersioningOptions();
        var upcaster = new LambdaEventUpcaster<OldEvt, NewEvt>(e => new NewEvt());
        options.AddUpcaster(upcaster);

        var registry = new EventUpcasterRegistry();
        options.ApplyTo(registry);

        registry.HasUpcasterFor(typeof(OldEvt).Name).ShouldBeTrue();
    }

    [Fact]
    public void ApplyTo_ScansAssemblies()
    {
        var options = new EventVersioningOptions();
        options.ScanAssembly(typeof(EventVersioningOptionsTests).Assembly);

        var registry = new EventUpcasterRegistry();
        options.ApplyTo(registry);

        // TestUpcaster is in this assembly, should be found
        registry.HasUpcasterFor(typeof(OldEvt).Name).ShouldBeTrue();
    }

    [Fact]
    public void ApplyTo_MultipleRegistrations_AllApplied()
    {
        var options = new EventVersioningOptions();
        options.AddUpcaster<OldEvt, NewEvt>(e => new NewEvt());
        options.AddUpcaster<OldEvt2, NewEvt2>(e => new NewEvt2());

        var registry = new EventUpcasterRegistry();
        options.ApplyTo(registry);

        registry.HasUpcasterFor(typeof(OldEvt).Name).ShouldBeTrue();
        registry.HasUpcasterFor(typeof(OldEvt2).Name).ShouldBeTrue();
    }

    public record OldEvt;
    public record NewEvt;
    public record OldEvt2;
    public record NewEvt2;

    public class TestUpcaster : IEventUpcaster
    {
        public string SourceEventTypeName => typeof(OldEvt).Name;
        public Type SourceEventType => typeof(OldEvt);
        public Type TargetEventType => typeof(NewEvt);
#pragma warning disable CA1822
        public object Upcast(object oldEvent) => new NewEvt();
#pragma warning restore CA1822
    }
}
