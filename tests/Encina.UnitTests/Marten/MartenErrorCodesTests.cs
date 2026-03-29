using Encina.Marten;
using Encina.Marten.Projections;
using Encina.Marten.Snapshots;
using Encina.Marten.Versioning;
using Shouldly;

namespace Encina.UnitTests.Marten;

public class MartenErrorCodesTests
{
    [Fact]
    public void MartenErrorCodes_AggregateNotFound_HasCorrectValue()
    {
        MartenErrorCodes.AggregateNotFound.ShouldBe("marten.aggregate_not_found");
    }

    [Fact]
    public void MartenErrorCodes_LoadFailed_HasCorrectValue()
    {
        MartenErrorCodes.LoadFailed.ShouldBe("marten.load_failed");
    }

    [Fact]
    public void MartenErrorCodes_SaveFailed_HasCorrectValue()
    {
        MartenErrorCodes.SaveFailed.ShouldBe("marten.save_failed");
    }

    [Fact]
    public void MartenErrorCodes_CreateFailed_HasCorrectValue()
    {
        MartenErrorCodes.CreateFailed.ShouldBe("marten.create_failed");
    }

    [Fact]
    public void MartenErrorCodes_ConcurrencyConflict_HasCorrectValue()
    {
        MartenErrorCodes.ConcurrencyConflict.ShouldBe("marten.concurrency_conflict");
    }

    [Fact]
    public void MartenErrorCodes_NoEventsToCreate_HasCorrectValue()
    {
        MartenErrorCodes.NoEventsToCreate.ShouldBe("marten.no_events_to_create");
    }

    [Fact]
    public void MartenErrorCodes_StreamAlreadyExists_HasCorrectValue()
    {
        MartenErrorCodes.StreamAlreadyExists.ShouldBe("marten.stream_already_exists");
    }

    [Fact]
    public void MartenErrorCodes_PublishEventsFailed_HasCorrectValue()
    {
        MartenErrorCodes.PublishEventsFailed.ShouldBe("marten.publish_events_failed");
    }

    [Fact]
    public void MartenErrorCodes_QueryFailed_HasCorrectValue()
    {
        MartenErrorCodes.QueryFailed.ShouldBe("marten.query_failed");
    }

    [Fact]
    public void MartenErrorCodes_InvalidQuery_HasCorrectValue()
    {
        MartenErrorCodes.InvalidQuery.ShouldBe("marten.invalid_query");
    }

    [Fact]
    public void MartenErrorCodes_EventNotFound_HasCorrectValue()
    {
        MartenErrorCodes.EventNotFound.ShouldBe("marten.event_not_found");
    }

    // ProjectionErrorCodes tests

    [Fact]
    public void ProjectionErrorCodes_Prefix_IsPROJECTION()
    {
        ProjectionErrorCodes.Prefix.ShouldBe("PROJECTION");
    }

    [Fact]
    public void ProjectionErrorCodes_ReadModelNotFound_HasCorrectValue()
    {
        ProjectionErrorCodes.ReadModelNotFound.ShouldBe("PROJECTION_READ_MODEL_NOT_FOUND");
    }

    [Fact]
    public void ProjectionErrorCodes_StoreFailed_HasCorrectValue()
    {
        ProjectionErrorCodes.StoreFailed.ShouldBe("PROJECTION_STORE_FAILED");
    }

    [Fact]
    public void ProjectionErrorCodes_DeleteFailed_HasCorrectValue()
    {
        ProjectionErrorCodes.DeleteFailed.ShouldBe("PROJECTION_DELETE_FAILED");
    }

    [Fact]
    public void ProjectionErrorCodes_QueryFailed_HasCorrectValue()
    {
        ProjectionErrorCodes.QueryFailed.ShouldBe("PROJECTION_QUERY_FAILED");
    }

    [Fact]
    public void ProjectionErrorCodes_ApplyFailed_HasCorrectValue()
    {
        ProjectionErrorCodes.ApplyFailed.ShouldBe("PROJECTION_APPLY_FAILED");
    }

    [Fact]
    public void ProjectionErrorCodes_RebuildFailed_HasCorrectValue()
    {
        ProjectionErrorCodes.RebuildFailed.ShouldBe("PROJECTION_REBUILD_FAILED");
    }

    [Fact]
    public void ProjectionErrorCodes_Cancelled_HasCorrectValue()
    {
        ProjectionErrorCodes.Cancelled.ShouldBe("PROJECTION_CANCELLED");
    }

    [Fact]
    public void ProjectionErrorCodes_NotRegistered_HasCorrectValue()
    {
        ProjectionErrorCodes.NotRegistered.ShouldBe("PROJECTION_NOT_REGISTERED");
    }

    // SnapshotErrorCodes tests

    [Fact]
    public void SnapshotErrorCodes_Prefix_IsSnapshot()
    {
        SnapshotErrorCodes.Prefix.ShouldBe("snapshot");
    }

    [Fact]
    public void SnapshotErrorCodes_LoadFailed_HasCorrectValue()
    {
        SnapshotErrorCodes.LoadFailed.ShouldBe("snapshot.load_failed");
    }

    [Fact]
    public void SnapshotErrorCodes_SaveFailed_HasCorrectValue()
    {
        SnapshotErrorCodes.SaveFailed.ShouldBe("snapshot.save_failed");
    }

    [Fact]
    public void SnapshotErrorCodes_DeleteFailed_HasCorrectValue()
    {
        SnapshotErrorCodes.DeleteFailed.ShouldBe("snapshot.delete_failed");
    }

    [Fact]
    public void SnapshotErrorCodes_PruneFailed_HasCorrectValue()
    {
        SnapshotErrorCodes.PruneFailed.ShouldBe("snapshot.prune_failed");
    }

    [Fact]
    public void SnapshotErrorCodes_RestoreFailed_HasCorrectValue()
    {
        SnapshotErrorCodes.RestoreFailed.ShouldBe("snapshot.restore_failed");
    }

    [Fact]
    public void SnapshotErrorCodes_InvalidVersion_HasCorrectValue()
    {
        SnapshotErrorCodes.InvalidVersion.ShouldBe("snapshot.invalid_version");
    }

    [Fact]
    public void SnapshotErrorCodes_NotSnapshotable_HasCorrectValue()
    {
        SnapshotErrorCodes.NotSnapshotable.ShouldBe("snapshot.not_snapshotable");
    }

    [Fact]
    public void SnapshotErrorCodes_CreationSkipped_HasCorrectValue()
    {
        SnapshotErrorCodes.CreationSkipped.ShouldBe("snapshot.creation_skipped");
    }

    // EventVersioningErrorCodes tests

    [Fact]
    public void EventVersioningErrorCodes_UpcastFailed_HasCorrectValue()
    {
        EventVersioningErrorCodes.UpcastFailed.ShouldBe("event.versioning.upcast_failed");
    }

    [Fact]
    public void EventVersioningErrorCodes_UpcasterNotFound_HasCorrectValue()
    {
        EventVersioningErrorCodes.UpcasterNotFound.ShouldBe("event.versioning.upcaster_not_found");
    }

    [Fact]
    public void EventVersioningErrorCodes_RegistrationFailed_HasCorrectValue()
    {
        EventVersioningErrorCodes.RegistrationFailed.ShouldBe("event.versioning.registration_failed");
    }

    [Fact]
    public void EventVersioningErrorCodes_DuplicateUpcaster_HasCorrectValue()
    {
        EventVersioningErrorCodes.DuplicateUpcaster.ShouldBe("event.versioning.duplicate_upcaster");
    }

    [Fact]
    public void EventVersioningErrorCodes_InvalidConfiguration_HasCorrectValue()
    {
        EventVersioningErrorCodes.InvalidConfiguration.ShouldBe("event.versioning.invalid_configuration");
    }
}
