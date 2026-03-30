using Encina.Marten;
using Encina.Marten.Versioning;
using global::Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Marten.Versioning;

/// <summary>
/// Unit tests for <see cref="ConfigureMartenEventVersioning"/>.
/// </summary>
public sealed class ConfigureMartenEventVersioningTests
{
    [Fact]
    public void Configure_NullOptions_Throws()
    {
        var sut = CreateSut();
        Should.Throw<ArgumentNullException>(() => sut.Configure(null!));
    }

    [Fact]
    public void Configure_EmptyRegistry_DoesNotThrow()
    {
        var sut = CreateSut();
        var storeOpts = new StoreOptions();
        Should.NotThrow(() => sut.Configure(storeOpts));
    }

    [Fact]
    public void Configure_AppliesVersioningOptions()
    {
        var martenOpts = new EncinaMartenOptions();
        // Add an inline upcaster via options
        martenOpts.EventVersioning.AddUpcaster<OldEvent, NewEvent>(e => new NewEvent { Name = e.Name });

        var sut = CreateSut(martenOpts);
        var storeOpts = new StoreOptions();

        // Should not throw — exercises ApplyTo + iteration
        sut.Configure(storeOpts);
    }

    [Fact]
    public void Configure_AppliesRegistrars()
    {
        var registrar = new TestRegistrar();
        var sut = CreateSut(registrars: [registrar]);
        var storeOpts = new StoreOptions();

        sut.Configure(storeOpts);

        registrar.WasCalled.ShouldBeTrue();
    }

    [Fact]
    public void Configure_WithMultipleRegistrars_AppliesAll()
    {
        var r1 = new TestRegistrar();
        var r2 = new TestRegistrar();
        var sut = CreateSut(registrars: [r1, r2]);
        var storeOpts = new StoreOptions();

        sut.Configure(storeOpts);

        r1.WasCalled.ShouldBeTrue();
        r2.WasCalled.ShouldBeTrue();
    }

    private static ConfigureMartenEventVersioning CreateSut(
        EncinaMartenOptions? martenOpts = null,
        IEnumerable<IEventUpcasterRegistrar>? registrars = null)
    {
        return new ConfigureMartenEventVersioning(
            new EventUpcasterRegistry(),
            Options.Create(martenOpts ?? new EncinaMartenOptions()),
            registrars ?? [],
            NullLogger<ConfigureMartenEventVersioning>.Instance);
    }

    public record OldEvent { public string Name { get; init; } = ""; }
    public record NewEvent { public string Name { get; init; } = ""; }

    private sealed class TestRegistrar : IEventUpcasterRegistrar
    {
        public bool WasCalled { get; private set; }
        public void Register(EventUpcasterRegistry registry)
        {
            WasCalled = true;
        }
    }
}
