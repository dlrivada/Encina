using Encina.Marten;
using Marten;

namespace Encina.GuardTests.Marten.Core;

public class EventMetadataEnrichmentServiceGuardTests
{
    [Fact]
    public void EnrichSession_NullSession_Throws()
    {
        var svc = new EventMetadataEnrichmentService(
            new EventMetadataOptions(), [], NullLogger<EventMetadataEnrichmentService>.Instance);
        Should.Throw<ArgumentNullException>(() =>
            svc.EnrichSession(null!, Substitute.For<IRequestContext>(), Array.Empty<object>()));
    }

    [Fact]
    public void EnrichSession_NullContext_Throws()
    {
        var svc = new EventMetadataEnrichmentService(
            new EventMetadataOptions(), [], NullLogger<EventMetadataEnrichmentService>.Instance);
        Should.Throw<ArgumentNullException>(() =>
            svc.EnrichSession(Substitute.For<IDocumentSession>(), null!, Array.Empty<object>()));
    }

    [Fact]
    public void EnrichSession_NullEvents_Throws()
    {
        var svc = new EventMetadataEnrichmentService(
            new EventMetadataOptions(), [], NullLogger<EventMetadataEnrichmentService>.Instance);
        Should.Throw<ArgumentNullException>(() =>
            svc.EnrichSession(Substitute.For<IDocumentSession>(), Substitute.For<IRequestContext>(), null!));
    }
}
