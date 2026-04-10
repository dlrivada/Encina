using Encina.Audit.Marten;
using Encina.Audit.Marten.Projections;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="ConfigureMartenAuditProjections"/> Marten store option configurator.
/// </summary>
/// <remarks>
/// <para>
/// These tests validate constructor guards and that <see cref="ConfigureMartenAuditProjections.Configure"/>
/// registers projections and indexes without throwing. Previously, this type could not be tested
/// end-to-end because projections took <see cref="IServiceProvider"/> as a parameter — Marten's
/// projection validator rejected that at store initialization time, which is why #949 was silently
/// broken in production.
/// </para>
/// <para>
/// With the fix, projections accept only parameter types supported by Marten
/// (<see cref="IDocumentOperations"/>, <see cref="CancellationToken"/>, and the event type), so
/// <see cref="ConfigureMartenAuditProjections.Configure"/> can be exercised here against a plain
/// <see cref="StoreOptions"/> instance without booting a real PostgreSQL store.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class ConfigureMartenAuditProjectionsTests
{
    private static ConfigureMartenAuditProjections CreateSut(MartenAuditOptions? options = null) =>
        new(
            Options.Create(options ?? new MartenAuditOptions()),
            NullLoggerFactory.Instance);

    [Fact]
    public void Constructor_NullAuditOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ConfigureMartenAuditProjections(null!, NullLoggerFactory.Instance));
    }

    [Fact]
    public void Constructor_NullLoggerFactory_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ConfigureMartenAuditProjections(
                Options.Create(new MartenAuditOptions()),
                null!));
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        var sut = CreateSut();
        sut.ShouldNotBeNull();
    }

    [Fact]
    public void Configure_NullOptions_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        Should.Throw<ArgumentNullException>(() => sut.Configure(null!));
    }

    [Fact]
    public void Configure_ValidOptions_RegistersAuditProjections()
    {
        // Arrange
        var sut = CreateSut();
        var storeOptions = new StoreOptions();

        // Act — this is the call that previously threw InvalidProjectionException
        // at Marten's projection validation layer because Create() took IServiceProvider.
        // With the fix it must complete without throwing.
        sut.Configure(storeOptions);

        // Assert: the configurator should run to completion. Marten does not expose a simple
        // public count of registered projections prior to store initialization, so we rely on
        // absence-of-exception plus the downstream integration test suite to verify the
        // projection graph itself.
    }

    [Fact]
    public void Configure_UsesCustomShreddedPlaceholder()
    {
        // Arrange
        var options = new MartenAuditOptions { ShreddedPlaceholder = "<REDACTED>" };
        var sut = CreateSut(options);
        var storeOptions = new StoreOptions();

        // Act — must not throw even when a non-default placeholder is configured.
        sut.Configure(storeOptions);
    }
}
