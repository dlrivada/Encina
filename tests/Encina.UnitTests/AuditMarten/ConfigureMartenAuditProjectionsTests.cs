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
    public void Configure_ValidOptions_PassesMartenProjectionValidation()
    {
        // Arrange
        var sut = CreateSut();
        var storeOptions = new StoreOptions();

        // Act + Assert: this is the exact call that previously threw InvalidProjectionException
        // at Marten's projection validation layer because Create() took IServiceProvider.
        // The regression signal for #949 is simply that StoreOptions.Projections.Add(...) —
        // invoked from inside Configure — runs to completion for BOTH audit projections.
        //
        // Marten does not expose a public, stable way to enumerate registered projections/mappings
        // on a bare StoreOptions instance (the real state materializes at DocumentStore.For()
        // time), so the projection graph itself is exercised end-to-end by the integration test
        // suite tracked in #951. For unit-level regression locking, not-throwing is the precise
        // inverse of the bug.
        Should.NotThrow(() => sut.Configure(storeOptions));
    }

    [Fact]
    public void Configure_CustomShreddedPlaceholder_PassesMartenProjectionValidation()
    {
        // Arrange
        var options = new MartenAuditOptions { ShreddedPlaceholder = "<REDACTED>" };
        var sut = CreateSut(options);
        var storeOptions = new StoreOptions();

        // Act + Assert: a non-default placeholder must not change the JasperFx validation result.
        // This guards against future refactors that might accidentally couple the placeholder to
        // a validator-rejected parameter type on the projection Create methods.
        Should.NotThrow(() => sut.Configure(storeOptions));
    }
}
