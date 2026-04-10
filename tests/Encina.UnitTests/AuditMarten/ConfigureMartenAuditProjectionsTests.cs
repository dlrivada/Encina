using Encina.Audit.Marten.Projections;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="ConfigureMartenAuditProjections"/> Marten store option configurator.
/// </summary>
/// <remarks>
/// Only the null-guard path is validated in unit tests. The full Configure path requires a
/// real Marten store initialization (integration-level) because Marten's projection validation
/// requires a DocumentStore context that cannot be faked.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class ConfigureMartenAuditProjectionsTests
{
    [Fact]
    public void Configure_NullOptions_ThrowsArgumentNullException()
    {
        var sut = new ConfigureMartenAuditProjections();
        Should.Throw<ArgumentNullException>(() => sut.Configure(null!));
    }

    [Fact]
    public void Constructor_CreatesInstance()
    {
        var sut = new ConfigureMartenAuditProjections();
        sut.ShouldNotBeNull();
    }
}
