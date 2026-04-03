using Encina.OpenTelemetry.Enrichers;
using Shouldly;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests for <see cref="AuditActivityEnricher"/> to verify null activity handling.
/// </summary>
public sealed class AuditActivityEnricherGuardTests
{
    [Fact]
    public void EnrichWithAuditRecord_NullActivity_DoesNotThrow()
    {
        Should.NotThrow(() => AuditActivityEnricher.EnrichWithAuditRecord(null, "Order", "created"));
    }

    [Fact]
    public void EnrichWithAuditQuery_NullActivity_DoesNotThrow()
    {
        Should.NotThrow(() => AuditActivityEnricher.EnrichWithAuditQuery(null, "by_entity", "Order"));
    }
}
